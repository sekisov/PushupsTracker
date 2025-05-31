namespace PushupsTracker.Core.Entities;

public class PushupsRecord
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public int Count { get; set; }
    public DateTime RecordedAt { get; set; }
    public User User { get; set; }
}