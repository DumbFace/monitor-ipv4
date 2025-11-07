using monitor_ip_4_tool.Caching;

namespace TestProject1;

public class MemoryCachingTest
{
    [Fact]
    public void SetPublicIPTest()
    {
        MicrosoftMemoryCache caching = new MicrosoftMemoryCache();
        caching.Set<string>(MicrosoftMemoryCache.PUBLIC_IP_TEST, "192.168.5.2");
        
        Assert.True(true);
    }
}