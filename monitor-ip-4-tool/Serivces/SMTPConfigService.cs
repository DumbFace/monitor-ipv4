using Microsoft.Extensions.Configuration;
using monitor_ip_4_tool.Interfaces;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace monitor_ip_4_tool.Serivces;

public class SMTPConfigService : IConfigApp
{
    readonly IConfiguration _config;


    public SMTPConfigService(IConfiguration config)
    {
        _config = config;
    }

    public SMTPConfig ReadConfig<SMTPConfig>()
    {
        return _config.GetSection("smtp").Get<SMTPConfig>();
    }
}

public class SMTPConfig
{
    public string From { get; set; }

    public string To { get; set; }

    public string Password { get; set; }

    public string Server { get; set; }

    public int Port { get; set; }
}