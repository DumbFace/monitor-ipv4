using System.Text.Json;

namespace monitor_ip_4_tool.Models
{
    public class RedisConfig
    {
        public string From { get; set; }

        public string To { get; set; }

        public string Password { get; set; }

        public string Server { get; set; }

        public int Port { get; set; }
        public string ToStringJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }
}