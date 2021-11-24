using System;

namespace Apollo.Service.Bluetooth
{
    public class ApolloBluetoothDevice
    {
        public DateTimeOffset BroadcastTime { get; }
        public ulong Address { get; }
        public string Name { get; }
        public short SignalStrengthInDB { get; }
        public bool Connected { get; }
        public bool CanPair { get; }
        public bool Paired { get; }
        public string DeviceId { get; }

        public ApolloBluetoothDevice(
            ulong address,
            string name,
            short rssi,
            DateTimeOffset broadcastTime,
            bool connected,
            bool canPair,
            bool paired,
            string deviceId
            )
        {
            Address = address;
            Name = name;
            SignalStrengthInDB = rssi;
            BroadcastTime = broadcastTime;
            Connected = connected;
            CanPair = canPair;
            Paired = paired;
            DeviceId = deviceId;
        }

        public override string ToString()
        {
            return $"{ (string.IsNullOrEmpty(Name) ? "[No Name]" : Name) } [{DeviceId}] ({SignalStrengthInDB})";
        }
    }
}
