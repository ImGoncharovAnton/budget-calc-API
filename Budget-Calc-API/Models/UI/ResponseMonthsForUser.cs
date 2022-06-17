using Budget;

namespace aspnetcore_auth.Models.UI;

public class ResponseMonthsForUser
{
    public int Id { get; set; }
    public int MonthNum { get; set; }
    public int Year { get; set; }
    public string UserId { get; set; }
    public double Income { get; set; }
    public double Expense { get; set; }
    public bool AdminChanged { get; set; }
}