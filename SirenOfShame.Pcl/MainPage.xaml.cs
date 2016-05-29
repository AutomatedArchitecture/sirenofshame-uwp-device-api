using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Storage;
using Windows.UI.Xaml;

namespace SirenOfShame.Pcl
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        private static ushort UsageId = 1;
        private static ushort UsagePage = 0xFF9C;
        private const ushort VendorId = 0x16d0;
        private const ushort ProductId = 0x0646;

        public MainPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var sosDevice = await GetDeviceInfo();
            using (HidDevice hidDevice = await HidDevice.FromIdAsync(sosDevice.Id, FileAccessMode.ReadWrite))
            {
                if (hidDevice != null)
                {
                    System.Diagnostics.Debug.WriteLine(hidDevice.UsageId);
                    System.Diagnostics.Debug.WriteLine(hidDevice.UsagePage);
                    System.Diagnostics.Debug.WriteLine(hidDevice.ProductId);
                    System.Diagnostics.Debug.WriteLine(hidDevice.VendorId);
                }
                else
                {
                    var deviceAccessStatus = DeviceAccessInformation.CreateFromId(sosDevice.Id).CurrentStatus;

                    string notificationMessage;
                    if (deviceAccessStatus == DeviceAccessStatus.DeniedByUser)
                    {
                        notificationMessage = "Access to the device was blocked by the user : " + sosDevice.Id;
                    }
                    else if (deviceAccessStatus == DeviceAccessStatus.DeniedBySystem)
                    {
                        // This status is most likely caused by app permissions (did not declare the device in the app's package.appxmanifest)
                        // This status does not cover the case where the device is already opened by another app.
                        notificationMessage = "Access to the device was blocked by the system : " + sosDevice.Id;
                    }
                    else
                    {
                        // Most likely the device is opened by another app, but cannot be sure
                        notificationMessage = "Unknown error, possibly opened by another app : " + sosDevice.Id;
                    }
                    System.Diagnostics.Debug.WriteLine(notificationMessage);
                }
            }
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
