using Microsoft.Data.Sqlite;
using monitor_ip_4_tool.Interfaces;
using IDatabase = monitor_ip_4_tool.Interfaces.IDatabase;

namespace monitor_ip_4_tool.Database;

public class SqlLite : IDatabase
{
    private static readonly string DbPath = Path.Combine(AppContext.BaseDirectory, "ip_log.db");
    private static SqliteConnection connect;
    private readonly ILog _logger;

    public SqlLite(ILog logger)
    {
        _logger = logger;
    }

    public async Task ConnectDb()
    {
        _logger.Info($"Path: {DbPath}");
        connect = new SqliteConnection("Data Source=" + DbPath);
        connect.Open();
    }

    public async Task CloseDb()
    {
        connect.DisposeAsync();
        connect.CloseAsync();
    }

    public async Task InitDb()
    {
        var cmd = connect.CreateCommand();
        cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS IpLog (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Ip TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        );
        INSERT INTO IpLog (Ip, CreatedAt) VALUES ('127.0.0.1', datetime('now'));

        ";

        await cmd.ExecuteNonQueryAsync();

        try
        {
            cmd.CommandText = @"
               INSERT INTO IpLog (Ip, CreatedAt) VALUES ('127.0.0.1', datetime('now'));
            ";

            var result = await cmd.ExecuteScalarAsync();
            _logger.Info($"Init Db result: ${result}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error init first row: ${ex.Message}");
        }
    }

    public async Task<int> SaveIP(string ip)
    {
        var cmd = connect.CreateCommand();
        cmd.CommandText = "INSERT INTO IpLog (Ip, CreatedAt) VALUES ($ip, datetime('now'));";
        cmd.Parameters.AddWithValue("$ip", ip);
        var result = await cmd.ExecuteNonQueryAsync();
        return result;
    }

    public async Task<string> GetLastIP()
    {
        string result;
        try
        {
            var cmd = connect.CreateCommand();
            cmd.CommandText = "SELECT ip FROM Iplog ORDER BY id DESC LIMIT 1;";
            result = await cmd.ExecuteScalarAsync() as string;
        }
        catch (Exception ex)
        {
            _logger.Error($"Get Last IP {ex.Message}");
            result = null;
        }
        return result;
    }
}