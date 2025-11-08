using monitor_ip_4_tool.Serivces;

namespace monitor_ip_4_tool.Interfaces;

public interface IConfigApp
{
    T ReadConfig<T>();
}