namespace monitor_ip_4_tool.Interfaces;

public interface ISendMail
{
    Task SendMail(IEnumerable<string> to = null, string subject = "", string body = "");
}