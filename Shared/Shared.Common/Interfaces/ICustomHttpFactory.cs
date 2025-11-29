namespace Shared.Shared.Common.Interfaces
{
    public interface ICustomHttpFactory
    {
        HttpClient GetHttpClientDefault();

        HttpClient GetIPv4Client();
    }
}
