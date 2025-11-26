using System.Text.Json;

namespace Shared.Shared.Common.Models
{
    public class RedisConfig
    {
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
