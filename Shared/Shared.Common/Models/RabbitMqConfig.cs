using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Shared.Shared.Common.Models;

public class RabbitMqConfig
{
    public RabbitMqConfig()
    {
    }

    public string Host { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }

    public int Port { get; set; }

    public IEnumerable<string> Queues { get; set; }

    public string ToStringJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
}