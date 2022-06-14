using Budget;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aspnetcore_auth.Controllers;

[Route("api/[controller]")] // api/setup
[ApiController]
public class SetupController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<SetupController> _logger;


    public SetupController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager,
        ILogger<SetupController> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [HttpGet]
    [Route("GetAllRoles")]
    public IActionResult GetAllRoles()
    {
        var roles = _roleManager.Roles.ToList();
        return Ok(roles);
    }
    

    [HttpPost]
    [Route("CreateRole")]
    public async Task<IActionResult> CreateRole(string name)
    {
        // Check is the role exist
        var roleExist = await _roleManager.RoleExistsAsync(name);

        if (!roleExist) // checks on the role exist status
        {
            var roleResult = await _roleManager.CreateAsync(new ApplicationRole(name));
            // We need to check if the role has been added successfully
            if (roleResult.Succeeded)
            {
                _logger.LogInformation($"The role {name} has been added successfully");
                return Ok(new
                {
                    result = $"The role {name} has been added successfully"
                });
            }
            else
            {
                _logger.LogInformation($"The role {name} has not been added");
                return BadRequest(new
                {
                    error = $"The role {name} has not been added"
                });
            }
        }

        return BadRequest(new { error = "Role already exist" });
    }

    // NotAllowed add in all files...

    [HttpGet]
    [Route("[action]/{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        var monthsForUserWithItems = await _context.Months
            .Where(m => m.ApplicationUserId == id)
            .Select(m => new
            {
                id = m.Id,
                monthNum = m.MonthNum,
                year = m.Year,
                userId = m.ApplicationUserId,
                income = m.Items.Where(x => x.Type == 0).Sum(s => s.Value),
                expense = m.Items.Where(x => x.Type != 0).Sum(s => s.Value),
                incomeArr = m.Items.Where(x => x.Type == 0).Select(i => new
                    {
                        id = i.Id,
                        createdBy = i.CreatedBy,
                        value = i.Value,
                        description = i.Description,
                        monthId = i.MonthId
                    }
                ),
                expenseArr = m.Items.Where(x => x.Type != 0).Select(i => new
                    {
                        id = i.Id,
                        createdBy = i.CreatedBy,
                        value = i.Value,
                        description = i.Description,
                        monthId = i.MonthId
                    }
                )
            }).ToListAsync();

        return Ok(monthsForUserWithItems);
        
        /*try
        {
            // предупреждение вылетает при суммировании.
            var user = await _userManager.Users
                .Where(x => x.Id == id)
                .Select(u => new
                {
                    id = u.Id,
                    userName = u.UserName,
                    email = u.Email,
                    months = u.Months.Select(m => new
                    {
                        id = m.Id,
                        monthNum = m.MonthNum,
                        year = m.Year,
                        userId = m.ApplicationUserId,
                        income = m.Items.Where(x => x.Type == 0).Sum(s => s.Value),
                        expense = m.Items.Where(x => x.Type != 0).Sum(s => s.Value),
                        incomeArr = m.Items.Where(x => x.Type == 0).Select(i => new
                            {
                                id = i.Id,
                                value = i.Value,
                                createdBy = i.CreatedBy,
                                description = i.Description,
                                monthId = i.MonthId
                            }
                            ),
                        expenseArr = m.Items.Where(x => x.Type != 0).Select(i => new
                            {
                                id = i.Id,
                                value = i.Value,
                                createdBy = i.CreatedBy,
                                description = i.Description,
                                monthId = i.MonthId
                            }
                        ),
                    })
                }).FirstAsync();
            return Ok(user);
        }
        catch (Exception e)
        {
            return NotFound();
        }*/
    }

    [HttpGet]
    [Route("GetAllUsers")]
    public async Task<IActionResult> GetAllUsers()
    {


        // var allUsers = await _userManager.Users
        //     .Include(u => u.UserRoles)
        //     .ThenInclude(ur => ur.Role).ToListAsync();

        var allUsers = await _userManager.Users
            .Select(u => new
            {
                id = u.Id,
                username = u.UserName,
                email = u.Email,
                role = u.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault(),
                months = u.Months.Select(m => new
                {
                    monthId = m.Id,
                    monthNum = m.MonthNum,
                    year = m.Year,
                    income = m.Items.Where(x => x.Type == 0).Sum(s => s.Value),
                    expense = m.Items.Where(x => x.Type != 0).Sum(s => s.Value)
                })
            }).ToListAsync();
        
        
        /*{
            "id": "d64db76a-fd60-4711-8b00-9987a104ff76",
            "username": "TestUser2",
            "email": "test2@gmail.com",
            "role": "Mortal",
            "months": [
            {
                "monthNum": 6,
                "income": 1477923,
                "expense": 57589
            },
            {
                "monthNum": 7,
                "income": 12334,
                "expense": 566
            }
            ]
        }*/

        
        // --------------------------------------------------------------------------------
         /*// its working, but more queries
        // Fetch all the Users
        var users = await _userManager.Users
            .Select(u => new { User = u, Roles = new List<string>() })
            .ToListAsync();

        // Fetch all the Roles
        var roleNames = await _roleManager.Roles
            .Select(r => r.Name)
            .ToListAsync();

        foreach (var roleName in roleNames)
        {
            // For each role, fetch the users
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            
            // Populate the roles for each user in memory
            var toUpdate = users.Where(u => 
                usersInRole.Any(ur => ur.Id == u.User.Id));

            foreach (var user in toUpdate)
            {
                user.Roles.Add(roleName);
            }
        }*/
        

        // return Ok(users);
        return Ok(allUsers);
    }

    [HttpPost]
    [Route("AddUserToRole")]
    public async Task<IActionResult> AddUserToRole(string email, string roleName)
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

        // Check if the role exist
        var roleExist = await _roleManager.RoleExistsAsync(roleName);

        if (!roleExist) // checks on the role exist status
        {
            _logger.LogInformation($"The role {roleName} does not exist");
            return BadRequest(new
            {
                error = "Role does not exist"
            });
        }

        var result = await _userManager.AddToRoleAsync(user, roleName);

        // Check if the user is assigned to the role successfully
        if (result.Succeeded)
        {
            return Ok(new
            {
                result = "Success, user has been added to the role"
            });
        }
        else
        {
            _logger.LogInformation($"The user was not abel to the role");
            return BadRequest(new
            {
                error = "The user was not abel to the role"
            });
        }
    }

    [HttpGet]
    [Route("GetUserRoles")]
    public async Task<IActionResult> GetUserRoles(string email)
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

        // return the roles
        var roles = await _userManager.GetRolesAsync(user);

        return Ok(roles);
    }

    [HttpPost]
    [Route("RemoveUserFromRole")]
    public async Task<IActionResult> RemoveUserFromRole(string email, string roleName)
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

        // Check if the role exist
        var roleExist = await _roleManager.RoleExistsAsync(roleName);

        if (!roleExist) // checks on the role exist status
        {
            _logger.LogInformation($"The role {roleName} does not exist");
            return BadRequest(new
            {
                error = "Role does not exist"
            });
        }

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);

        if (result.Succeeded)
        {
            return Ok(new
            {
                result = $"User {email} has been removed from role {roleName}"
            });
        }

        return BadRequest(new
        {
            error = $"Unable to remove User {email} from role {roleName}"
        });
    }
}