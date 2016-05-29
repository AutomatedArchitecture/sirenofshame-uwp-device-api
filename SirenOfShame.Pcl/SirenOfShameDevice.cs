using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Storage;
using Windows.Storage.Streams;
using MetroLog;

namespace SirenOfShame.Pcl
{
    public class SirenOfShameDevice : IDisposable
    {
        private static readonly ILogger Log = LogManagerFactory.DefaultLogManager.GetLogger<SirenOfShameDevice>();

        private static ushort UsageId = 1;
        private static ushort UsagePage = 0xFF9C;
        private DeviceWatcher _deviceWatcher;
        private static HidDevice _hidDevice;
        private const ushort VendorId = 0x16d0;
        private const ushort ProductId = 0x0646;

        public const int ReportId_Out_ControlPacket = 1;
        public const int ReportId_Out_Upload = 2;
        public const int ReportId_In_Info = 1;
        public const int ReportId_In_ReadAudioPacket = 3;
        public const int ReportId_In_ReadLedPacket = 4;

        private const byte LED_MODE_MANUAL = 1;

        private const int PacketSize = 1 + 37; // report id + packet length

        public event EventHandler Connected;
        public event EventHandler Disconnected;

        private static UsbControlPacket GetControlPacket(
            ControlByte1Flags controlByte = ControlByte1Flags.Ignore,
            byte audioMode = (byte)0xff, UInt16 audioDuration = (UInt16)0xffff,
            byte ledMode = (byte)0xff, UInt16 ledDuration = (UInt16)0xffff,
            byte readAudioIndex = (byte)0xff,
            byte readLedIndex = (byte)0xff,
            byte manualLeds0 = (byte)0xff,
            byte manualLeds1 = (byte)0xff,
            byte manualLeds2 = (byte)0xff,
            byte manualLeds3 = (byte)0xff,
            byte manualLeds4 = (byte)0xff)
        {
            return new UsbControlPacket
            {
                ReportId = ReportId_Out_ControlPacket,
                ControlByte1 = controlByte,
                AudioMode = audioMode,
                AudioDuration = audioDuration,
                LedMode = ledMode,
                LedDuration = ledDuration,
                ReadAudioIndex = readAudioIndex,
                ReadLedIndex = readLedIndex,
                ManualLeds0 = manualLeds0,
                ManualLeds1 = manualLeds1,
                ManualLeds2 = manualLeds2,
                ManualLeds3 = manualLeds3,
                ManualLeds4 = manualLeds4
            };
        }

        private async Task SetOutputReport(UsbControlPacket usbControlPacket)
        {
            if (_hidDevice == null) throw new ArgumentNullException(nameof(_hidDevice));

            var outputReport = _hidDevice.CreateOutputReport(ReportId_Out_ControlPacket);

            var dataWriter = new DataWriter();

            var bytes = ToByteArray(usbControlPacket);
            dataWriter.WriteBytes(bytes);

            outputReport.Data = dataWriter.DetachBuffer();

            uint bytesWritten = await _hidDevice.SendOutputReportAsync(outputReport);

            Log.Debug("Bytes written: " + bytesWritten);
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

        private static byte[] ToByteArray(UsbControlPacket obj)
        {
            byte[] buffer = new byte[PacketSize];
            int objSize = Marshal.SizeOf(obj);
            IntPtr ptr = Marshal.AllocHGlobal(objSize);
            try
            {
                Marshal.StructureToPtr(obj, ptr, false);
                Marshal.Copy(ptr, buffer, 0, objSize);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return buffer;
        }

        private static async Task<bool> TryConnect()
        {
            var sosDevice = await GetDeviceInfo();
            _hidDevice = await HidDevice.FromIdAsync(sosDevice.Id, FileAccessMode.ReadWrite);
            if (_hidDevice != null)
                return true;
            var deviceAccessStatus = DeviceAccessInformation.CreateFromId(sosDevice.Id).CurrentStatus;
            var notificationMessage = GetErrorMessage(deviceAccessStatus, sosDevice);
            Log.Debug(notificationMessage);
            return false;
        }

        public void StartWatching()
        {
            if (_deviceWatcher != null) return;
            var selector = GetDeviceSelector();
            _deviceWatcher = DeviceInformation.CreateWatcher(selector);
            _deviceWatcher.Added += OnDeviceAdded;
            _deviceWatcher.Removed += OnDeviceRemoved;
            _deviceWatcher.Start();
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Log.Debug("Sos device was disconnected");
            _hidDevice = null;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        private async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
            Log.Debug("Sos device was connected");
            var connected = await TryConnect();
            if (connected)
            {
                Connected?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task ManualControl(ManualControlData manualControlData)
        {
            byte manualSiren = (byte)(manualControlData.Siren ? 1 : 0);
            var controlPacket = GetControlPacket(
                ledMode: LED_MODE_MANUAL,
                audioMode: manualSiren,
                manualLeds0: manualControlData.Led0,
                manualLeds1: manualControlData.Led1,
                manualLeds2: manualControlData.Led2,
                manualLeds3: manualControlData.Led3,
                manualLeds4: manualControlData.Led4);
            await SetOutputReport(controlPacket);
        }

        public void Dispose()
        {
            if (_hidDevice != null)
            {
                _hidDevice.Dispose();
                _hidDevice = null;
            }
        }
    }
}
