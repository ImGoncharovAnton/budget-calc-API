using Budget;

namespace aspnetcore_auth.Models.UI;

public class ItemUserData
{
    public string CreatedBy { get; set; }
    public double Value { get; set; }
    public string Description { get; set; }
    public Item.IncomeOrExpense Type { get; set; }
    public int MonthId { get; set; }
}