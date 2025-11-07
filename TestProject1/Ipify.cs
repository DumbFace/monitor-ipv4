using monitor_ip_4_tool.Interfaces;
using Moq;

namespace TestProject1;

public class Ipify
{
    // 3 behavior 
    // Valid IP
    // Invalid IP
    // Http Exception response timeout
    [Fact]
    public async Task GetIP4Async_ShouldReturnValidIP()
    {
        var mockIp = new Mock<IInternetProtocol>();

        mockIp.Setup(m => m.GetIP4Async())
            .ReturnsAsync("192.168.1.100");

        var ipService = mockIp.Object;

        var ip = await ipService.GetIP4Async();

        Assert.Equal("192.168.1.100", ip);

        Console.WriteLine("Mocked IPv4: " + ip);
    }

    [Fact]
    public async Task GetIP4Async_ShouldReturnEmptyString()
    {
        var mockIp = new Mock<IInternetProtocol>();

        mockIp.Setup(m => m.GetIP4Async())
            .ReturnsAsync("");

        var ipService = mockIp.Object;

        var ip = await ipService.GetIP4Async();

        Assert.Equal("", ip);
        Console.WriteLine("Mocked invalid IP: '" + ip + "'");
    }

    [Fact]
    public async Task GetIP4Async_ShouldThrowHttpException()
    {
        var mockIp = new Mock<IInternetProtocol>();

        mockIp.Setup(m => m.GetIP4Async())
            .ThrowsAsync(new HttpRequestException("Not Response or Time out"));

        var ipService = mockIp.Object;

        var ex = await Assert.ThrowsAsync<HttpRequestException>(async () => await ipService.GetIP4Async());

        Console.WriteLine("Not Response or Time out: " + ex.Message);
    }
}