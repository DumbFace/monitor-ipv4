namespace Shared.Shared.Common.Interfaces;

public interface ISendMail
{
    Task SendMail(CancellationToken token, IEnumerable<string> to = null, string subject = "", string body = "");
}
