using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Storage;
using Windows.UI.Xaml;
using MetroLog;

namespace SirenOfShame.Pcl
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        private static readonly ILogger Log = LogManagerFactory.DefaultLogManager.GetLogger<MainPage>();

        private static ushort UsageId = 1;
        private static ushort UsagePage = 0xFF9C;
        private const ushort VendorId = 0x16d0;
        private const ushort ProductId = 0x0646;

        public MainPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            WatchForSosDeviceConnection();
        }

        private void WatchForSosDeviceConnection()
        {
            var selector = GetDeviceSelector();
            var deviceWatcher = DeviceInformation.CreateWatcher(selector);
            deviceWatcher.Added += OnDeviceAdded;
            deviceWatcher.Removed += OnDeviceRemoved;
            deviceWatcher.Start();
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Log.Debug("Sos device was disconnected");
        }

        private async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
            Log.Debug("Sos device was connected");
            await TryConnect();
        }

        private static async Task TryConnect()
        {
            var sosDevice = await GetDeviceInfo();
            using (HidDevice hidDevice = await HidDevice.FromIdAsync(sosDevice.Id, FileAccessMode.ReadWrite))
            {
                if (hidDevice != null)
                {
                    Log.Debug("Successfully connected with sos device");
                }
                else
                {
                    var deviceAccessStatus = DeviceAccessInformation.CreateFromId(sosDevice.Id).CurrentStatus;
                    var notificationMessage = GetErrorMessage(deviceAccessStatus, sosDevice);
                    Log.Debug(notificationMessage);
                }
            }
        }

        private static string GetErrorMessage(DeviceAccessStatus deviceAccessStatus, DeviceInformation sosDevice)
        {
            if (deviceAccessStatus == DeviceAccessStatus.DeniedByUser)
            {
                return "Access to the device was blocked by the user : " + sosDevice.Id;
            }
            if (deviceAccessStatus == DeviceAccessStatus.DeniedBySystem)
            {
                // This status is most likely caused by app permissions (did not declare the device in the app's package.appxmanifest)
                // This status does not cover the case where the device is already opened by another app.
                return "Access to the device was blocked by the system : " + sosDevice.Id;
            }

            // Most likely the device is opened by another app, but cannot be sure
            return "Unknown error, possibly opened by another app : " + sosDevice.Id;
        }

        private static async Task<DeviceInformation> GetDeviceInfo()
        {
            var deviceSelector = GetDeviceSelector();
            var devices = await DeviceInformation.FindAllAsync(deviceSelector);
            return devices.AsEnumerable().FirstOrDefault();
        }

        private static string GetDeviceSelector()
        {
            var deviceSelector = HidDevice.GetDeviceSelector(UsagePage, UsageId, VendorId, ProductId);
            return deviceSelector;
        }
    }
}
