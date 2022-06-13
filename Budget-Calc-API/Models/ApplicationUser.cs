using Microsoft.AspNetCore.Identity;

namespace Budget;

public class ApplicationUser : IdentityUser
{
    public ICollection<ApplicationUserRole> UserRoles { get; set; }
    public virtual List<Month> Months { get; set; }
}