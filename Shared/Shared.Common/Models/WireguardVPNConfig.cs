namespace Shared.Shared.Common.Models;

public class WireguardVPNConfig
{
    public string VPNService { get; set; }

    public string VPNDirectory { get; set; }

    public string VPNConfigDirectory { get; set; }

    public string SubnetVPN { get; set; }

    public string PublicKey { get; set; }

    public string PrivateKey { get; set; }

    public string Address { get; set; }
}
