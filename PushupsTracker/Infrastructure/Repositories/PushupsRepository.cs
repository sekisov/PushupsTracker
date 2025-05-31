using Dapper;
using PushupsTracker.Core.Entities;
using PushupsTracker.Core.Interfaces;
using PushupsTracker.Core.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace PushupsTracker.Infrastructure.Repositories;

public class PushupsRepository : IPushupsRepository
{
    private readonly string _connectionString;

    public PushupsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new MySqlConnection(_connectionString);

    public async Task<PushupsRecord> AddRecord(long userId, string userName, int count)
    {
        await EnsureUserExists(userId, userName);

        var record = new PushupsRecord
        {
            UserId = userId,
            Count = count,
            RecordedAt = DateTime.Now
        };

        const string query = @"
            INSERT INTO PushupsRecords (UserId, Count, RecordedAt)
            VALUES (@UserId, @Count, @RecordedAt);
            SELECT LAST_INSERT_ID();";

        using var connection = CreateConnection();
        record.Id = await connection.ExecuteScalarAsync<int>(query, record);
        return record;
    }

    private async Task EnsureUserExists(long userId, string userName)
    {
        const string query = @"
            INSERT IGNORE INTO Users (UserId, UserName, RegisteredAt)
            VALUES (@UserId, @UserName, @RegisteredAt);";

        using var connection = CreateConnection();
        await connection.ExecuteAsync(query, new
        {
            UserId = userId,
            UserName = userName,
            RegisteredAt = DateTime.Now
        });
    }
    public async Task<int> GetTodayUserPushupsCount(long userId)
    {
        const string query = @"
        SELECT COALESCE(SUM(Count), 0) 
        FROM PushupsRecords 
        WHERE UserId = @UserId AND DATE(RecordedAt) = CURDATE()";

        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(query, new { UserId = userId });
    }
    public async Task<IEnumerable<UserStatistic>> GetTodayStatistics()
    {
        const string query = @"
    SELECT 
        u.UserId,
        u.UserName, 
        SUM(pr.Count) as TotalCount
    FROM PushupsRecords pr
    JOIN Users u ON pr.UserId = u.UserId
    WHERE DATE(pr.RecordedAt) = CURDATE()
    GROUP BY u.UserId, u.UserName
    ORDER BY TotalCount DESC;";

        using var connection = CreateConnection();
        return await connection.QueryAsync<UserStatistic>(query);
    }

    public async Task<IEnumerable<DailyStatistic>> GetAllTimeStatistics()
    {
        const string query = @"
            SELECT 
                DATE(pr.RecordedAt) as Date,
                u.UserName,
                SUM(pr.Count) as TotalCount
            FROM PushupsRecords pr
            JOIN Users u ON pr.UserId = u.UserId
            GROUP BY DATE(pr.RecordedAt), u.UserId, u.UserName
            ORDER BY Date DESC, TotalCount DESC;";

        using var connection = CreateConnection();
        return await connection.QueryAsync<DailyStatistic>(query);
    }

    public async Task<int> GetRemainingPushups()
    {
        // Реализуйте эту логику по вашему усмотрению
        return 0;
    }

    // Реализация остальных методов IRepository<PushupsRecord>
    public async Task<PushupsRecord> GetById(int id)
    {
        const string query = "SELECT * FROM PushupsRecords WHERE Id = @Id";
        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<PushupsRecord>(query, new { Id = id });
    }

    public async Task<IEnumerable<PushupsRecord>> GetAll()
    {
        const string query = "SELECT * FROM PushupsRecords";
        using var connection = CreateConnection();
        return await connection.QueryAsync<PushupsRecord>(query);
    }

    public async Task Add(PushupsRecord entity)
    {
        const string query = @"
            INSERT INTO PushupsRecords (UserId, Count, RecordedAt)
            VALUES (@UserId, @Count, @RecordedAt)";
        using var connection = CreateConnection();
        await connection.ExecuteAsync(query, entity);
    }

    public async Task Update(PushupsRecord entity)
    {
        const string query = @"
            UPDATE PushupsRecords 
            SET UserId = @UserId, Count = @Count, RecordedAt = @RecordedAt
            WHERE Id = @Id";
        using var connection = CreateConnection();
        await connection.ExecuteAsync(query, entity);
    }

    public async Task Delete(PushupsRecord entity)
    {
        const string query = "DELETE FROM PushupsRecords WHERE Id = @Id";
        using var connection = CreateConnection();
        await connection.ExecuteAsync(query, new { entity.Id });
    }
}