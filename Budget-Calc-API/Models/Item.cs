namespace Budget;

public class Item
{
    public int Id { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.Now;
    public DateTime UpdateDate { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Description { get; set; }
    public IncomeOrExpense Type { get; set; }
    
    // public virtual string UserId { get; set; }
    public virtual int MonthId { get; set; }

    public enum IncomeOrExpense
    {
        Income = 0,
        Expense = 1
    }
}