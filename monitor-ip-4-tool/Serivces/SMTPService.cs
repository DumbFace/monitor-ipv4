using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using monitor_ip_4_tool.Interfaces;
using monitor_ip_4_tool.Models;

namespace monitor_ip_4_tool.Serivces;

public class SMTPService : ISendMail
{
    private readonly ILog _logger;

    readonly IConfiguration _config;

    readonly IOptionsMonitor<SMTPConfig> _smtpConfigMonitoor;
    public SMTPService(
        ILog logger,
        IConfiguration config,
        IOptionsMonitor<SMTPConfig> smtpConfigMonitoor
        )
    {
        _smtpConfigMonitoor = smtpConfigMonitoor;
        _logger = logger;
        _config = config;

        _smtpConfigMonitoor.OnChange((config) =>
        {
            _logger.Info($"SMTP change config {config.ToStringJson()}");
        });

    }

    public async Task SendMail(CancellationToken token, IEnumerable<string> to = null, string subject = "", string body = "")
    {
        SMTPConfig config = _smtpConfigMonitoor.CurrentValue;
        if (config is null) throw new Exception("Cannot read config or config null");
        MailMessage mail = new MailMessage();
        mail.From = new MailAddress(config.From);
        mail.To.Add(config.To);
        mail.Subject = subject;
        mail.Body = body;

        SmtpClient smtp = new SmtpClient(config.Server, config.Port);
        smtp.Credentials = new NetworkCredential(config.From, config.Password);
        smtp.EnableSsl = true;

        //TODO remove later


        await Task.Delay(2000);
        // await smtp.SendMailAsync(mail, token);
        _logger.Info($"Send Email Or Sync New IP: ${body}");
    }
}