using Apollo.Service.Bluetooth;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace Apollo.Service
{
    public class Worker : BackgroundService
    {
        private readonly ApolloAdvertisementWatcher _watcher;
        private GattCharacteristic _service;
        private BluetoothLEDevice _device;

        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<Worker> _logger;
        private readonly ApolloOptions _options;
        private readonly HubConnection _hub;

        private static readonly Guid _characteristic = Guid.Parse("0000ffd9-0000-1000-8000-00805f9b34fb");

        public Worker(IHostApplicationLifetime hostApplicationLifetime, ILogger<Worker> logger, ApolloAdvertisementWatcher watcher, IOptions<ApolloOptions> options)
        {
            _logger = logger;
            _watcher = watcher;
            _options = options.Value;
            _hostApplicationLifetime = hostApplicationLifetime;

            _hub = new HubConnectionBuilder()
                .WithAutomaticReconnect(new[]
                {
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30) // Only try multiple times after waiting 60 seconds before failing to reconnect. 
                })
                .WithUrl(_options.ServiceURL)
                .Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _watcher.StartListening();

            _hub.On<bool>("LIGHT_STATE_UPDATE", OnLightStateUpdate);
            _hub.On<FadeStateUpdate>("FADE_STATE_UPDATE", OnFadeStateUpdate);
            _hub.On<ColorStateUpdate>("COLOR_STATE_UPDATE", (s) => OnColorStateUpdate(s));

            _hub.Closed += (exception) =>
            {
                _logger.LogError(exception, "The hub was closed, shutting down the service.");
                _hostApplicationLifetime.StopApplication();
                return Task.CompletedTask;
            };

            _hub.Reconnecting += (exception) =>
            {
                _logger.LogWarning(exception, "Hub is reconnecting due to losing it's connection.");
                return Task.CompletedTask;
            };

            do
            {
                var device = _watcher.DiscoveredDevices.FirstOrDefault(x => x.Name.Contains("APM"));
                if (device != null)
                {
                    _device = await BluetoothLEDevice.FromBluetoothAddressAsync(device.Address).AsTask();
                    var services = await _device.GetGattServicesAsync().AsTask();
                    foreach(var service in services.Services)
                    {
                        var gatt = service.GetAllCharacteristics()
                            .FirstOrDefault(x => x.Uuid == _characteristic);

                        if(_service == null)
                            _service = gatt;
                    }
                }

                await Task.Delay(100, stoppingToken);
            } while (_device == null && !stoppingToken.IsCancellationRequested);

            _logger.LogInformation("BluetoothLE device found, now connecting to hub.");

            await _hub.StartAsync(stoppingToken);
            _logger.LogInformation("Connected!");
            await Task.Delay(-1, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stopping hub connection due to service ending.");
            
            _device.Dispose();
            _watcher.StopListening();
            await _hub.StopAsync(stoppingToken);
            await base.StopAsync(stoppingToken);
        }

        protected Task OnFadeStateUpdate(FadeStateUpdate update)
        {
            // requesting new fade
            return Task.CompletedTask;
        }

        protected async Task OnColorStateUpdate(ColorStateUpdate update)
        {
            _logger.LogInformation("COLOR_STATE_UPDATE new state is: {state}", JsonSerializer.Serialize(update));
            DataWriter writer = new DataWriter();

            writer.WriteByte(0x56);
            writer.WriteByte((byte)update.R);
            writer.WriteByte((byte)update.G);
            writer.WriteByte((byte)update.B);
            //writer.WriteByte((byte)update.Alpha);
            writer.WriteByte(0xFF);
            writer.WriteByte(0xF0);
            writer.WriteByte(0xAA);

            await _service.WriteValueAsync(writer.DetachBuffer());
        }

        protected async Task OnLightStateUpdate(bool on)
        {
            _logger.LogInformation("LIGHT_STATE_UPDATE new state is: {on}", on ? "On" : "Off");

            var control = (on ? 0x23 : 0x24);
            DataWriter writer = new DataWriter();

            writer.WriteByte(0xCC);
            writer.WriteByte((byte)control);
            writer.WriteByte(0x33);

            await _service.WriteValueAsync(writer.DetachBuffer());
        }
    }
}