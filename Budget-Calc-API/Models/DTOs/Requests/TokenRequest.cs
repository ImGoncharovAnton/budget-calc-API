using System.ComponentModel.DataAnnotations;

namespace aspnetcore_auth.Models.DTOs.Requests;

public class TokenRequest
{
    [Required]
    public string Token { get; set; }
    
    [Required]
    public string RefreshToken { get; set; }
}