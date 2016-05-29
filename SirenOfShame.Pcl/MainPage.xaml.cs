using System;
using System.Threading.Tasks;
using MetroLog;
using SirenOfShame.Device;

namespace SirenOfShame.Pcl
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        private static readonly ILogger Log = LogManagerFactory.DefaultLogManager.GetLogger<MainPage>();
        private readonly SirenOfShameDevice _sirenOfShameDevice;

        public MainPage()
        {
            _sirenOfShameDevice = new SirenOfShameDevice();
            InitializeComponent();
            _sirenOfShameDevice.Connected += SirenOfShameDeviceOnConnected;
            _sirenOfShameDevice.Disconnected += SirenOfShameDeviceOnDisconnected;
            _sirenOfShameDevice.StartWatching();
        }

        private void SirenOfShameDeviceOnDisconnected(object sender, EventArgs eventArgs)
        {
            
        }

        private async void SirenOfShameDeviceOnConnected(object sender, EventArgs eventArgs)
        {
            bool turnOn = true;
            while (true)
            {
                Log.Debug("device connected so turnong on led's");
                var led0 = turnOn ? (byte)10 : (byte)0;
                var manualControlData = new ManualControlData
                {
                    Led0 = led0,
                    Led1 = led0,
                    Led2 = led0,
                    Led3 = led0,
                    Led4 = led0,
                    Siren = false
                };
                if (_sirenOfShameDevice.IsConnected)
                {
                    await _sirenOfShameDevice.ManualControl(manualControlData);
                }
                turnOn = !turnOn;
                await Task.Delay(1000);
            }
        }
    }
}
