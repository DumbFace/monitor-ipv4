namespace monitor_ip_4_tool.Utils;

public static class InternetProtocolExtensions
{
    public static async Task<string> OrNextAsync(this Task<string> current, Func<Task<string>> next)
    {
        var result = await current;
        return string.IsNullOrEmpty(result) ? await next() : result;
    }
}