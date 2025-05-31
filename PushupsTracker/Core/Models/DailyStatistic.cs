namespace PushupsTracker.Core.Models;

public class DailyStatistic
{
    public DateTime Date { get; set; }
    public string UserName { get; set; }
    public string Name { get; set; }
    public int TotalCount { get; set; }
}