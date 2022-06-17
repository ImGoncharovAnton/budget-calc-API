using Budget;

namespace aspnetcore_auth.Models.DTOs.Responses;

public class ItemDto
{
    public int Id { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Description { get; set; }
    public Item.IncomeOrExpense Type { get; set; }
    public int MonthId { get; set; }
}