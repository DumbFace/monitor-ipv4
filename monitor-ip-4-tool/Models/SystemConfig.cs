using System.ComponentModel.DataAnnotations;

namespace monitor_ip_4_tool.Models;

public class SystemConfig
{
    [Range(1, 100, ErrorMessage = "Set scan value between 1 and 100")]
    public int ScanIPv4 { get; set; }

    public int ScanIPv4FromSecond => ScanIPv4 * 1000;
}