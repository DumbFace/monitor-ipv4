using Microsoft.Data.Sqlite;
using monitor_ip_4_tool.Interfaces;

namespace monitor_ip_4_tool.Database;

public class SqlLite : IDatabase
{
    private const string DbPath = "ip_log.db";
    private static SqliteConnection connect;

    public SqlLite()
    {
    }

    public async Task ConnectDb()
    {
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

        INSERT INTO IpLog (Ip, CreatedAt) VALUES ('127.0.0.0', datetime('now'));
        ";
        cmd.ExecuteNonQueryAsync();
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
        var cmd = connect.CreateCommand();
        cmd.CommandText = "SELECT ip FROM Iplog ORDER BY id DESC LIMIT 1;";
        var result = await cmd.ExecuteScalarAsync() as string;
        return result;
    }
}