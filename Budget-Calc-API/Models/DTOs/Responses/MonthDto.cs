using Budget;

namespace aspnetcore_auth.Models.DTOs.Responses;

public class MonthDto
{
    
    public int Id { get; set; }
    public string CreatedBy { get; set; }
    public int MonthNum { get; set; }
    public int Year { get; set; }
    public string UserId { get; set; }
    public List<ItemDto> Items { get; set; }
}