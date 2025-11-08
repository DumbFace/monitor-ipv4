using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using monitor_ip_4_tool.Interfaces;

namespace monitor_ip_4_tool.Serivces;

public class SMTPService : ISendMail
{
    private readonly ILog _logger;
    private readonly IConfigApp _configSMTP;


    public SMTPService(ILog logger, IConfigApp configSMTP)
    {
        _logger = logger;
        _configSMTP = configSMTP;
    }

    public async Task SendMail(IEnumerable<string> to = null, string subject = "", string body = "")
    {
        try
        {
            SMTPConfig config = _configSMTP.ReadConfig<SMTPConfig>();
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(config.From);
            mail.To.Add(config.To);
            mail.Subject = subject;
            mail.Body = body;

            SmtpClient smtp = new SmtpClient(config.Server, config.Port);
            smtp.Credentials = new NetworkCredential(config.From, config.Password);
            smtp.EnableSsl = true;

            try
            {
                await smtp.SendMailAsync(mail);
                _logger.Info($"Send Email Or Sync New IP: ${body}");
            }
            catch (Exception ex)
            {
                _logger.Info("Error sending mail: " + ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error sending mail: {ex.Message}");
        }
    }
}