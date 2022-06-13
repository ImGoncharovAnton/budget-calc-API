using System.Security.Claims;
using Budget;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace aspnetcore_auth.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClaimsSetupController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<ClaimsSetupController> _logger;

    public ClaimsSetupController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager, 
        RoleManager<ApplicationRole> roleManager, 
        ILogger<ClaimsSetupController> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllClaims(string email)
    {
        // Check if the user exist
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null) // User does not exist
        {
            _logger.LogInformation($"The user with the {email} does not exist");
            return BadRequest(new
            {
                error = "User does not exist"
            });
        }

        var userClaims = await _userManager.GetClaimsAsync(user);
        return Ok(userClaims);
    }

    [HttpPost]
    [Route("AddClaimsToUser")]
    public async Task<IActionResult> AddClaimsToUser(string email, string claimName, string claimValue)
    {
        // Check if the user exist
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null) // User does not exist
        {
            _logger.LogInformation($"The user with the {email} does not exist");
            return BadRequest(new
            {
                error = "User does not exist"
            });
        }
        
        // Check if the claim exist
        var userClaims = await _userManager.GetClaimsAsync(user);

        if (userClaims != null)
        {
            if ((from item in userClaims let itemType = item.Type let itemValue = item.Value where itemType == claimName && itemValue == claimValue select itemType).Any())
            {
                _logger.LogInformation($"The claim {claimName} already contains {claimValue}, does not exist");
                return BadRequest(new
                {
                    error = $"The claim ({claimName}) already contains ({claimValue}) "
                });
            }
        }
       
        var userClaim = new Claim(claimName, claimValue);

        var result = await _userManager.AddClaimAsync(user, userClaim);

        if (result.Succeeded)
        {
            return Ok(new
            {
                result = $"User {user.Email} has a claim {claimName} added to them"
            });
        }

        return BadRequest(new
        {
            error = $"Unable to add claim {claimName} to the user {user.Email}"
        });
    }

}