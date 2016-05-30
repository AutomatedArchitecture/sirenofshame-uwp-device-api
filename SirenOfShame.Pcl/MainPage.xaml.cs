using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using MetroLog;
using SirenOfShame.Device;

namespace SirenOfShame.HardwareTestGui
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
            SetSirenConnectedVisibility();
            _sirenOfShameDevice.Connected += SirenOfShameDeviceOnConnected;
            _sirenOfShameDevice.Disconnected += SirenOfShameDeviceOnDisconnected;
            _sirenOfShameDevice.StartWatching();
        }

        private async void SirenOfShameDeviceOnDisconnected(object sender, EventArgs eventArgs)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, SetSirenConnectedVisibility);
        }

        private async void SirenOfShameDeviceOnConnected(object sender, EventArgs eventArgs)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await SetDeviceInfo();

                LedPatternListBox.ItemsSource = _sirenOfShameDevice.LedPatterns;
                AudioPatternListBox.ItemsSource = _sirenOfShameDevice.AudioPatterns;
                SetSirenConnectedVisibility();
            });
            //bool turnOn = true;
            //while (true)
            //{
            //    Log.Debug("device connected so turnong on led's");
            //    var led0 = turnOn ? (byte)10 : (byte)0;
            //    var manualControlData = new ManualControlData
            //    {
            //        Led0 = led0,
            //        Led1 = led0,
            //        Led2 = led0,
            //        Led3 = led0,
            //        Led4 = led0,
            //        Siren = false
            //    };
            //    if (_sirenOfShameDevice.IsConnected)
            //    {
            //        await _sirenOfShameDevice.ManualControl(manualControlData);
            //    }
            //    turnOn = !turnOn;
            //    await Task.Delay(1000);
            //}
        }

        private void SetSirenConnectedVisibility()
        {
            DisconnectedPanel.Visibility = _sirenOfShameDevice.IsConnected ? Visibility.Collapsed : Visibility.Visible;
        }

        private async Task SetDeviceInfo()
        {
            var deviceInfo = await _sirenOfShameDevice.ReadDeviceInfo();
            FirmwareVersion.Text = deviceInfo.FirmwareVersion.ToString();
            HardwareType.Text = deviceInfo.HardwareType.ToString();
            HardwareVersion.Text = deviceInfo.HardwareVersion.ToString();
            AudioMode.Text = deviceInfo.AudioMode.ToString();
            AudioDurationRemaining.Text = deviceInfo.AudioPlayDuration.ToString();
            LedMode.Text = deviceInfo.LedMode.ToString();
            LedDurationRemaining.Text = deviceInfo.LedPlayDuration.ToString();
        }

        private async void PlayLedPattern(object sender, RoutedEventArgs e)
        {
            if (LedPatternListBox.SelectedItem == null) return;
            LedPattern ledPattern = (LedPattern)LedPatternListBox.SelectedItem;
            var durationTimeSpan = GetDurationTimeSpan(LedDuration.Text);
            await _sirenOfShameDevice.PlayLightPattern(ledPattern, durationTimeSpan);
        }

        private TimeSpan? GetDurationTimeSpan(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            int ledDuration;
            if (!int.TryParse(value, out ledDuration)) return null;
            var durationTimeSpan = new TimeSpan(0, 0, 0, 0, ledDuration);
            return durationTimeSpan;
        }

        private async void PlayAudioPattern(object sender, RoutedEventArgs e)
        {
            if (AudioPatternListBox.SelectedItem == null) return;
            AudioPattern audioPattern = (AudioPattern)AudioPatternListBox.SelectedItem;
            var durationTimeSpan = GetDurationTimeSpan(LedDuration.Text);
            await _sirenOfShameDevice.PlayAudioPattern(audioPattern, durationTimeSpan);
        }
    }
}
