using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Shared.Shared.Common.Models;

public class SystemConfig
{
    [Range(1, 3600, ErrorMessage = "Set scan value between 1 and 100")]
    public int ScanIPv4 { get; set; }

    public int ScanIPv4FromSecond => ScanIPv4 * 1000;

    public string ToStringJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    public OperatingConfig LinuxOperating { get; set; }

    public OperatingConfig WindowOperating { get; set; }

    public string TEST_IP_PUBLIC { get; set; } = String.Empty;

}
