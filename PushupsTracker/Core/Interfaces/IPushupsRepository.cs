using PushupsTracker.Core.Entities;
using PushupsTracker.Core.Models;

namespace PushupsTracker.Core.Interfaces;

public interface IPushupsRepository : IRepository<PushupsRecord>
{
    Task<IEnumerable<UserStatistic>> GetTodayStatistics();
    Task<IEnumerable<DailyStatistic>> GetAllTimeStatistics();
    Task<int> GetRemainingPushups();
    Task<PushupsRecord> AddRecord(long userId, string userName, int count);
    Task<int> GetTodayUserPushupsCount(long userId);
}