using System.ComponentModel.DataAnnotations;

namespace aspnetcore_auth.Models.DTOs.Requests;

public class UserRegistrationDto
{
    [Required]
    public string Username { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    public string Password { get; set; }
}