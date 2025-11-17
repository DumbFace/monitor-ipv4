using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace monitor_ip_4_tool.Models;

public class SystemConfig
{
    [Range(1, 100, ErrorMessage = "Set scan value between 1 and 100")]
    public int ScanIPv4 { get; set; }

    public int ScanIPv4FromSecond => ScanIPv4 * 1000;

    public string LinuxOpenVPNService { get; set; }

    public string WindowOpenVPNService { get; set; }

    public string ToStringJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}