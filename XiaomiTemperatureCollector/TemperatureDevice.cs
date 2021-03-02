using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace XiaomiTemperatureCollector
{
    public class DeviceData
    {
        public double Temperature { get; set; }
        public int Humidity { get; set; }
    }

    public sealed class TemperatureDevice
    {
        public BluetoothLEDevice Device { get; private set; }

        private GattCharacteristic _temperatureCharacteristic;

        private readonly List<DeviceData> _collectedData = new List<DeviceData>();

        public IReadOnlyList<DeviceData> DeviceData => _collectedData.AsReadOnly();

        public TemperatureDevice()
        {
        }

        public async Task FindDevice(ulong address)
        {
            BluetoothLEDevice device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);

            if (device != null)
            {
                Device = device;
            }

            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };
            DeviceWatcher deviceWatcher =
            DeviceInformation.CreateWatcher(
                    BluetoothLEDevice.GetDeviceSelectorFromBluetoothAddress(address),
                    requestedProperties);

            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Start();

            Device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);

            if(Device == null)
            {
                throw new KeyNotFoundException();
            }
        }

        public async Task StartCollectingData()
        {
            Guid serviceUuid = new Guid("ebe0ccb0-7a0a-4b0c-8a1a-6ff2997da3a6");
            var serviceResult = await Device.GetGattServicesForUuidAsync(serviceUuid);

            foreach (var service in serviceResult.Services)
            {
                if (service != null && service.Uuid == serviceUuid)
                {
                    Console.WriteLine(service.Uuid);
                    var characteristics = await service.GetCharacteristicsAsync();

                    foreach (var characteristic in characteristics.Characteristics)
                    {
                        //var desc = await characteristic.GetDescriptorsAsync();

                        if (characteristic.Uuid == new Guid("ebe0ccc1-7a0a-4b0c-8a1a-6ff2997da3a6"))
                        {
                            _temperatureCharacteristic = characteristic;
                            characteristic.ValueChanged += Item_ValueChanged;
                        }
                    }
                }
            }
        }

        public void StopCollecting()
        {
            _temperatureCharacteristic.ValueChanged -= Item_ValueChanged;
            Device.Dispose();
        }

        public Task<DeviceData> GetData()
        {
            return Task.Run(() =>
            {
                while (_collectedData.Count == 0)
                {
                    Task.Delay(400);
                }
                return _collectedData.FirstOrDefault();
            });
        }


        private void Item_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] input = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(input);

            Console.WriteLine($"temp: {BitConverter.ToInt16(input.Take(2).ToArray()) * 0.01}");
            Console.WriteLine($"humidity: {input[2]}");

            this._collectedData.Add(new DeviceData()
            {
                Temperature = BitConverter.ToInt16(input.Take(2).ToArray()) * 0.01,
                Humidity = input[2]
            });
            StopCollecting();
        }

        private static void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            // Method intentionally left empty.
        }

        private static void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            // Method intentionally left empty.
        }

        private static void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            // Method intentionally left empty.
        }
    }
}
