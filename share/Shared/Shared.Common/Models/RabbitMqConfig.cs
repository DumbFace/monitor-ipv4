using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace monitor_ip_4_tool.Models;

public class FirebaseConfig
{
    public string SecretKey { get; set; }

    public string Server { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }

    public IEnumerable<string> Queues { get; set; }
}
