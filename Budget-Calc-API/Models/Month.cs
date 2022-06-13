namespace Budget;

public class Month
{
    public int Id { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.Now;
    public DateTime UpdateDate { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = string.Empty;
    public int MonthNum { get; set; }
    public int Year { get; set; }
    // public string UserId { get; set; }
    public string ApplicationUserId { get; set; }
    public virtual List<Item> Items { get; set; }
}