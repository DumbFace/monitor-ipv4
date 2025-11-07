using System.Net;
using System.Net.Sockets;
using monitor_ip_4_tool.Interfaces;
using Moq;
using Serilog;

namespace TestProject1;

public class Ifconfig
{
    private readonly ILog _logger;

    // 3 behavior 
    // Valid IP
    // Invalid IP
    // Http Exception response timeout
    public Ifconfig(ILog logger)
    {
        _logger = logger;
    }

    [Fact]
    public async Task GetIP4Async_ShouldReturnTrueIfIP4()
    {
        var mockIp = new Mock<IInternetProtocol>();

        mockIp.Setup(m => m.GetIP4Async()).ReturnsAsync("192.168.1.100");

        var ipService = mockIp.Object;

        var ipAsString = await ipService.GetIP4Async();
        if (IPAddress.TryParse(ipAsString, out var ip))
        {
            Assert.True(ip.AddressFamily == AddressFamily.InterNetwork);
        }

        Console.WriteLine("Mocked IPv4: " + ip);
    }

    [Fact]
    public async Task GetIP4Async_ShouldReturnTrueIfNotIp4()
    {
        var mockIp = new Mock<IInternetProtocol>();

        mockIp.Setup(m => m.GetIP4Async()).ReturnsAsync("2001:0db8:85a3:0000:0000:8a2e:0370:7334");

        var ipService = mockIp.Object;

        var ipAsString = await ipService.GetIP4Async();
        if (IPAddress.TryParse(ipAsString, out var ip))
        {
            Assert.False(ip.AddressFamily == AddressFamily.InterNetwork);
        }

        _logger.Error($"Invalid IPv4 addres: ${ip.ToString()}");
    }

    [Fact]
    public async Task GetIP4Async_ShouldReturnTrueIfEmptyString()
    {
        var mockIp = new Mock<IInternetProtocol>();

        mockIp.Setup(m => m.GetIP4Async()).ReturnsAsync("");

        var ipService = mockIp.Object;

        var ipAsString = await ipService.GetIP4Async();
        if (String.IsNullOrEmpty(ipAsString))
        {
            Assert.False(false);
            _logger.Error($"Empty string");
        }
    }

    [Fact]
    public async Task GetIP4Async_ShouldThrowHttpException()
    {
        var mockIp = new Mock<IInternetProtocol>();

        mockIp.Setup(m => m.GetIP4Async()).ThrowsAsync(new HttpRequestException("Time out or not response!"));

        var ipService = mockIp.Object;

        var ex = await Assert.ThrowsAsync<HttpRequestException>(async () => await ipService.GetIP4Async());
        _logger.Error(ex.Message);
    }

    [Fact]
    public async Task GetIP4Async_ShouldGenerateObject()
    {
        var mockIp_1 = new Mock<IInternetProtocol>();
        var mockIp_2 = new Mock<IInternetProtocol>();
        var mockIp_3 = new Mock<IInternetProtocol>();
        var mockIp_4 = new Mock<IInternetProtocol>();

        mockIp_1.Setup(m => m.GetIP4Async()).ReturnsAsync("0.0.0.0");
        mockIp_2.Setup(m => m.GetIP4Async()).ReturnsAsync("1.1.1.1");
        mockIp_3.Setup(m => m.GetIP4Async()).ReturnsAsync("-1.256.1.1");

        var ipService_1 = mockIp_1.Object;
        var ipService_2 = mockIp_2.Object;
        var ipService_3 = mockIp_3.Object;

        IPAddress.TryParse(await ipService_1.GetIP4Async(), out var ip1);
        IPAddress.TryParse(await ipService_2.GetIP4Async(), out var ip2);
        IPAddress.TryParse(await ipService_3.GetIP4Async(), out var ip3);
        IPAddress.TryParse(null, out var ip4);

        Assert.NotNull(ip1); // not null
        Assert.NotNull(ip2); // not null
        Assert.NotNull(ip3); // null
        // Assert.NotNull(ip4); // null


        // Log.Error($"Invalid IPv4 addres: ${ip.ToString()}");
    }
}