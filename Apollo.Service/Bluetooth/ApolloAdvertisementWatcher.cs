using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace Apollo.Service.Bluetooth
{
    public class ApolloAdvertisementWatcher
    {
        private readonly BluetoothLEAdvertisementWatcher mWatcher;
        private readonly Dictionary<string, ApolloBluetoothDevice> mDiscoveredDevices = new Dictionary<string, ApolloBluetoothDevice>();
        private readonly object mThreadLock = new object();
        public bool Listening => mWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;

        public IReadOnlyCollection<ApolloBluetoothDevice> DiscoveredDevices
        {
            get
            {
                CleanupTimeouts();
                lock (mThreadLock)
                {
                    return mDiscoveredDevices.Values.ToList().AsReadOnly();
                }
            }
        }

        public int HeartbeatTimeout { get; set; } = 30;

        public ApolloAdvertisementWatcher()
        {
            mWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            mWatcher.Received += WatcherAdvertisementReceivedAsync;
        }

        private async void WatcherAdvertisementReceivedAsync(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            CleanupTimeouts();
            var device = await GetBluetoothLEDeviceAsync(
                args.BluetoothAddress,
                args.Timestamp,
                args.RawSignalStrengthInDBm);

            if (device == null)
                return;
            
            lock (mThreadLock)
            {
                if (!Listening) // Don't add the device if listening was stopped.
                    return;

                mDiscoveredDevices[device.DeviceId] = device;
            }
        }

        private async Task<ApolloBluetoothDevice> GetBluetoothLEDeviceAsync(ulong address, DateTimeOffset broadcastTime, short rssi)
        {
            using var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask();

            if (device == null)
                return null;

            return new ApolloBluetoothDevice
            (
                deviceId: device.DeviceId,
                address: device.BluetoothAddress,
                name: device.Name,
                broadcastTime: broadcastTime,
                rssi: rssi,
                connected: device.ConnectionStatus == BluetoothConnectionStatus.Connected,
                canPair: device.DeviceInformation.Pairing.CanPair,
                paired: device.DeviceInformation.Pairing.IsPaired
            );
        }

        private void CleanupTimeouts()
        {
            lock (mThreadLock)
            {
                var threshold = DateTime.UtcNow - TimeSpan.FromSeconds(HeartbeatTimeout);
                mDiscoveredDevices.Where(f => f.Value.BroadcastTime < threshold).ToList()
                    .ForEach(device => mDiscoveredDevices.Remove(device.Key));
            }
        }

        public void StartListening()
        {
            lock (mThreadLock)
            {
                // If already listening...
                if (Listening)
                    // Do nothing more
                    return;

                // Start the underlying watcher
                mWatcher.Start();
            }
        }

        public void StopListening()
        {
            lock (mThreadLock)
            {
                // If we are no currently listening...
                if (!Listening)
                    // Do nothing more
                    return;

                mWatcher.Stop();
                mDiscoveredDevices.Clear();
            }
        }

        public static async Task PairToDeviceAsync(string deviceId)
        {
            using var device = await BluetoothLEDevice.FromIdAsync(deviceId).AsTask();

            if (device == null)
                throw new InvalidOperationException("Failed to get information about the Bluetooth device");

            if (device.DeviceInformation.Pairing.IsPaired)
                return;

            device.DeviceInformation.Pairing.Custom.PairingRequested += (sender, args) => args.Accept();

            var result = await device.DeviceInformation.Pairing.Custom.PairAsync(
                    DevicePairingKinds.ConfirmOnly,
                    DevicePairingProtectionLevel.None
                ).AsTask();

            if (result.Status != DevicePairingResultStatus.Paired)
                throw new InvalidOperationException("Unable to pair with bluetooth device: " + result.Status);
        }
    }
}
