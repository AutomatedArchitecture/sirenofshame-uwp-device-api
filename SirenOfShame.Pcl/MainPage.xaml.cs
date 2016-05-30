using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
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
        private Timer _deviceInfoTimer;

        public MainPage()
        {
            _sirenOfShameDevice = new SirenOfShameDevice();
            InitializeComponent();
            if (!DesignMode.DesignModeEnabled)
            {
                SetSirenConnectedVisibility();
                _deviceInfoTimer = new Timer(DeviceInfoTimerOnTick, null, 0, 500);
                _sirenOfShameDevice.Connected += SirenOfShameDeviceOnConnected;
                _sirenOfShameDevice.Disconnected += SirenOfShameDeviceOnDisconnected;
                _sirenOfShameDevice.StartWatching();
            }
        }

        private async void DeviceInfoTimerOnTick(object state)
        {
            if (_sirenOfShameDevice.IsConnected)
            {
                var deviceInfo = await _sirenOfShameDevice.ReadDeviceInfo();
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, 
                    () => SetRealtimeDeviceInfo(deviceInfo));
            }
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
                LedPatternListBox.SelectedIndex = 0;
                AudioPatternListBox.ItemsSource = _sirenOfShameDevice.AudioPatterns;
                AudioPatternListBox.SelectedIndex = 0;
                SetSirenConnectedVisibility();
            });
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
            SetRealtimeDeviceInfo(deviceInfo);
        }

        private void SetRealtimeDeviceInfo(UsbInfoPacket deviceInfo)
        {
            AudioMode.Text = deviceInfo.AudioMode.ToString();
            AudioDurationRemaining.Text = ToTimespanString(deviceInfo.AudioPlayDuration);
            LedMode.Text = deviceInfo.LedMode.ToString();
            LedDurationRemaining.Text = ToTimespanString(deviceInfo.LedPlayDuration); ;
        }

        private static string ToTimespanString(ushort playDuration)
        {
            if (playDuration == 0) return "Not playing";
            var timeSpan = new TimeSpan(0, 0, 0, 0, playDuration * 100);
            return timeSpan.Minutes.ToString("00") + ":" + timeSpan.Seconds.ToString("00") + " remaining";
        }

        private async void PlayLedPattern(object sender, RoutedEventArgs e)
        {
            if (LedPatternListBox.SelectedItem == null) return;
            LedPattern ledPattern = (LedPattern)LedPatternListBox.SelectedItem;
            var durationTimeSpan = GetDurationTimeSpan(LedDuration.Text);
            await _sirenOfShameDevice.PlayLightPattern(ledPattern, durationTimeSpan);
        }

        private async void StopLedPattern(object sender, RoutedEventArgs e)
        {
            await _sirenOfShameDevice.PlayLightPattern(null, null);
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
            var durationTimeSpan = GetDurationTimeSpan(AudioDuration.Text);
            await _sirenOfShameDevice.PlayAudioPattern(audioPattern, durationTimeSpan);
        }

        private async void StopAudioPattern(object sender, RoutedEventArgs e)
        {
            await _sirenOfShameDevice.PlayAudioPattern(null, null);
        }

        private async void ManualLed_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var manualControlData = new ManualControlData
            {
                Led0 = ToByte(ManualLed1.Value),
                Led1 = ToByte(ManualLed2.Value),
                Led2 = ToByte(ManualLed3.Value),
                Led3 = ToByte(ManualLed4.Value),
                Led4 = ToByte(ManualLed5.Value),
                Siren = false
            };
            await _sirenOfShameDevice.ManualControl(manualControlData);
        }

        private byte ToByte(double val)
        {
            return (byte) val;
        }
    }
}
