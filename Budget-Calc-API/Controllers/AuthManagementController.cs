using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using aspnetcore_auth.Configuration;
using aspnetcore_auth.Models.DTOs.Requests;
using aspnetcore_auth.Models.DTOs.Responses;
using Budget;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace aspnetcore_auth.Controllers;

[Route("api/[controller]")] // api/authManagement
[ApiController]
public class AuthManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly JwtConfig _jwtConfig;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly ApplicationDbContext _apiDbContext;

    public AuthManagementController(
        UserManager<ApplicationUser> userManager, 
        IOptionsMonitor<JwtConfig> optionsMonitor, 
        TokenValidationParameters tokenValidationParameters, 
        ApplicationDbContext apiDbContext, 
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _tokenValidationParameters = tokenValidationParameters;
        _apiDbContext = apiDbContext;
        _roleManager = roleManager;
        _jwtConfig = optionsMonitor.CurrentValue;
    }

    [HttpPost]
    [Route("Register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationDto user)
    {
        if (!ModelState.IsValid)
            return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>()
                {
                    "Invalid payload"
                },
                Success = false
            });
        // we can utilise the model 
        var existingUser = await _userManager.FindByEmailAsync(user.Email);

        if (existingUser != null)
        {
            return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>()
                {
                    "Email already in use"
                },
                Success = false
            });
        }

        var newUser = new ApplicationUser()
        {
            Email = user.Email,
            UserName = user.Username
        };
        var isCreated = await _userManager.CreateAsync(newUser, user.Password);
        if (!isCreated.Succeeded)
        {
            return BadRequest(new RegistrationResponse()
            {
                Errors = isCreated.Errors.Select(x => x.Description).ToList(),
                Success = false
            });
        }

        // We need to add the user to a role
        await _userManager.AddToRoleAsync(newUser, "Mortal");
        
        var jwtToken = await GenerateJwtToken(newUser);

        return Ok(jwtToken);
    }

    [HttpPost]
    [Route("Login")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest user)
    {
        if (!ModelState.IsValid)
            return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>()
                {
                    "Invalid payload"
                },
                Success = false
            });

        var existingUser = await _userManager.FindByEmailAsync(user.Email);
        
        if (existingUser == null)
        {
            return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>()
                {
                    "Invalid login request"
                },
                Success = false
            });
        }

        var isCorrect = await _userManager.CheckPasswordAsync(existingUser, user.Password);

        if (!isCorrect)
        {
            return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>()
                {
                    "Invalid password"
                },
                Success = false
            });
        }

        var jwtToken = await GenerateJwtToken(existingUser);

        return Ok(jwtToken);
    }

    [HttpDelete]
    [Route("DeleteUser")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var currentUser = await _userManager.FindByIdAsync(id);
        if (currentUser == null)
            return NotFound();
        await _userManager.DeleteAsync(currentUser);

        return NoContent();
    }
    
    [HttpPost]
    [Route("RefreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenRequest tokenRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>()
                {
                    "Invalid payload"
                },
                Success = false
            });
        }

       var result = await VerifyAndGenerateToken(tokenRequest);

       if (result == null)
       {
           return BadRequest(new RegistrationResponse()
           {
               Errors = new List<string>()
               {
                   "Invalid tokens"
               },
               Success = false
           });
       }

       return Ok(result);
    }

    private async Task<AuthResult> VerifyAndGenerateToken(TokenRequest tokenRequest)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();

        try
        {
            // Validation one - Validation JWT token format
            _tokenValidationParameters.ValidateLifetime = false;
            var tokenInVerification = jwtTokenHandler.ValidateToken(tokenRequest.Token, _tokenValidationParameters,
                out var validatedToken);
            _tokenValidationParameters.ValidateLifetime = true;
            // Validation two - Validate encryption alg
            if (validatedToken is JwtSecurityToken jwtSecurityToken)
            {
                var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase);

                if (result == false)
                {
                    return null;
                }
            }
            
            // Validation three - Validate expiry date
            var utcExpiriedDate = long.Parse(tokenInVerification.Claims
                .FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

            var expiryDate = UnixTimeStampToDateTime(utcExpiriedDate);

            if (expiryDate > DateTime.UtcNow)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Token has not yet expired"
                    }
                };
            }
            
            // Validation four - Validate existence of the token
            var storedToken = await _apiDbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == tokenRequest.RefreshToken);

            if (storedToken == null)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Token does not exist"
                    }
                };
            }
            
            // Validation five - Validate if used
            if (storedToken.IsUsed)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Token has been used"
                    }
                };
            }
            // Validation six - Validate if revoked
            if (storedToken.IsRevorked)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Token has been revorked"
                    }
                };
            }

            // Validation Seven - Validate the id
            var jti = tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
            if (storedToken.JwtId != jti)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Token doesn't match"
                    }
                };
            }
            
            // Validation Eight - Validate stored token expiry date
            if (storedToken.ExpiredDate < DateTime.UtcNow)
            {
                return new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Refresh token has expired"
                    }
                };
            }
            
            // update current token
            storedToken.IsUsed = true;
            _apiDbContext.RefreshTokens.Update(storedToken);
            await _apiDbContext.SaveChangesAsync();

            var dbUser = await _userManager.FindByIdAsync(storedToken.UserId);
            return await GenerateJwtToken(dbUser);

        }
        catch (Exception e)
        {
            return null;
            // if(e.Message.Contains("Lifetime validation failed. The "))
        }
    }

   private async Task<AuthResult> GenerateJwtToken(ApplicationUser user)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();

        var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);

        var claims = await GetAllValidClaims(user);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(5),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = jwtTokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = jwtTokenHandler.WriteToken(token);

        var refreshToken = new RefreshToken()
        {
            JwtId = token.Id,
            IsUsed = false,
            IsRevorked = false,
            UserId = user.Id,
            AddedDate = DateTime.UtcNow,
            ExpiredDate = DateTime.UtcNow.AddMonths(2),
            Token = RandomString(35) + Guid.NewGuid()
        };

        await _apiDbContext.RefreshTokens.AddAsync(refreshToken);
        await _apiDbContext.SaveChangesAsync();

        return new AuthResult()
        {
            Token = jwtToken,
            Success = true,
            RefreshToken = refreshToken.Token
        };
    }

   // Get all valid claims for the corresponding user
   private async Task<List<Claim>> GetAllValidClaims(ApplicationUser user)
   {
       var claims = new List<Claim>
       {
           new Claim("id", user.Id),
           new Claim(JwtRegisteredClaimNames.Email, user.Email),
           new Claim(JwtRegisteredClaimNames.Sub, user.Email),
           new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
       };

       // Getting the claims that we have assigned to the user
       var userClaims = await _userManager.GetClaimsAsync(user);
       claims.AddRange(userClaims);
       
       // Get the user role and added it the claims
       var userRoles = await _userManager.GetRolesAsync(user);

       foreach (var userRole in userRoles)
       {
           var role = await _roleManager.FindByNameAsync(userRole);

           if (role != null)
           {
               claims.Add(new Claim(ClaimTypes.Role, userRole));
               
               var roleClaims = await _roleManager.GetClaimsAsync(role);
               foreach (var roleClaim in roleClaims)
               {
                   claims.Add(roleClaim);
               }
           }
       }

       return claims;
   }

    private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTimeVal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTimeVal = dateTimeVal.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dateTimeVal;
    }
    
    private string RandomString(int length)
    {
        var random = new Random();
        var chars = "QWERTYUIOPASDFGHJKLZXCVBNM0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(x => x[random.Next(x.Length)]).ToArray());

    }
}