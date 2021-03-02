using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace XiaomiTemperatureCollector
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var device = new TemperatureDevice();
            await device.FindDevice(181149783211336);
            await device.StartCollectingData();
            DeviceData data = await device.GetData();

            Console.WriteLine(data.Temperature);

        }
    }
}
