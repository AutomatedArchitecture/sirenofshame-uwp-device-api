using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Storage;
using Windows.Storage.Streams;

namespace SirenOfShame.Device
{
    public class SirenOfShameDevice : IDisposable
    {
        private static readonly ILogger Log = LogManagerFactory.DefaultLogManager.GetLogger<SirenOfShameDevice>();

        private static ushort UsageId = 1;
        private static ushort UsagePage = 0xFF9C;
        private DeviceWatcher _deviceWatcher;
        private HidDevice _hidDevice;
        private readonly List<LedPattern> _ledPatterns = new List<LedPattern>();
        private readonly List<AudioPattern> _audioPatterns = new List<AudioPattern>();
        private const UInt16 Duration_Forever = 0xfffe;

        public List<LedPattern> LedPatterns => _ledPatterns;
        public List<AudioPattern> AudioPatterns => _audioPatterns;

        private const ushort VendorId = 0x16d0;
        private const ushort ProductId = 0x0646;

        private const int ReportId_Out_ControlPacket = 1;
        private const int ReportId_Out_Upload = 2;
        private const int ReportId_In_Info = 1;
        private const int ReportId_In_ReadAudioPacket = 3;
        private const int ReportId_In_ReadLedPacket = 4;

        private const byte LED_MODE_MANUAL = 1;

        private const int PacketSize = 1 + 37; // report id + packet length

        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public bool IsConnected => _hidDevice != null;

        private async Task SendControlPacket(
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
            var controlPacket = new UsbControlPacket
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
            await SetOutputReport(controlPacket);
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

        public async Task<bool> TryConnect()
        {
            var sosDevice = await GetDeviceInfo();
            _hidDevice = await HidDevice.FromIdAsync(sosDevice.Id, FileAccessMode.ReadWrite);
            if (_hidDevice != null)
            {
                await ReadDeviceInfo();
                await ReadAudioPatterns();
                await ReadLedPatterns();
                return true;
            }
            var deviceAccessStatus = DeviceAccessInformation.CreateFromId(sosDevice.Id).CurrentStatus;
            var notificationMessage = GetErrorMessage(deviceAccessStatus, sosDevice);
            Log.Debug(notificationMessage);
            return false;
        }

        private async Task ReadLedPatterns()
        {
            _ledPatterns.Clear();
            await SendControlPacket(readLedIndex: 0);

            while (true)
            {
                var ledPatternPacket = await GetInputReport<UsbReadLedPacket>(ReportId_In_ReadLedPacket);
                if (ledPatternPacket.Id == 0xff)
                {
                    return;
                }
                _ledPatterns.Add(new LedPattern
                {
                    Id = ledPatternPacket.Id,
                    Name = new string(ledPatternPacket.Name).TrimEnd('\0')
                });
            }
        }

        private async Task<T> GetInputReport<T>(ushort reportId)
        {
            var inputReport = await _hidDevice.GetInputReportAsync(reportId);
            IBuffer buffer = inputReport.Data;
            byte[] bytes = new byte[buffer.Length];
            buffer.CopyTo(bytes);
            var result = ToInfoReport<T>(bytes);
            return result;
        }

        private async Task ReadAudioPatterns()
        {
            _audioPatterns.Clear();
            await SendControlPacket(readAudioIndex: 0);
            while (true)
            {
                var audioPatternPacket = await GetInputReport<UsbReadAudioPacket>(ReportId_In_ReadAudioPacket);
                if (audioPatternPacket.Id == 0xff)
                {
                    return;
                }
                _audioPatterns.Add(new AudioPattern
                {
                    Id = audioPatternPacket.Id,
                    Name = new string(audioPatternPacket.Name).TrimEnd('\0')
                });
            }
        }

        public async Task<UsbInfoPacket> ReadDeviceInfo()
        {
            var infoPacket = await GetInputReport<UsbInfoPacket>(ReportId_In_Info);
            Log.Debug("Info packet receieved:");
            Log.Debug("\tVersion: " + infoPacket.FirmwareVersion);
            Log.Debug("\tHardwareType: " + infoPacket.HardwareType);
            Log.Debug("\tHardwareVersion: " + infoPacket.HardwareVersion);
            Log.Debug("\tExternalMemorySize: " + infoPacket.ExternalMemorySize);
            Log.Debug("\tAudioMode: " + infoPacket.AudioMode);
            Log.Debug("\tAudioPlayDuration: " + infoPacket.AudioPlayDuration);
            Log.Debug("\tLedMode: " + infoPacket.LedMode);
            Log.Debug("\tLedPlayDuration: " + infoPacket.LedPlayDuration);
            FirmwareVersion = infoPacket.FirmwareVersion;
            HardwareType = infoPacket.HardwareType;
            HardwareVersion = infoPacket.HardwareVersion;
            return infoPacket;
        }

        public static HardwareType HardwareType { get; set; }

        public static byte HardwareVersion { get; set; }

        public static ushort FirmwareVersion { get; set; }

        private static T ToInfoReport<T>(byte[] buffer)
        {
            GCHandle pinnedPacket = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                T result = (T)Marshal.PtrToStructure(pinnedPacket.AddrOfPinnedObject(), typeof(T));
                return result;
            }
            finally
            {
                pinnedPacket.Free();
            }
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
            _hidDevice.Dispose();
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
            await SendControlPacket(
                ledMode: LED_MODE_MANUAL,
                audioMode: manualSiren,
                manualLeds0: manualControlData.Led0,
                manualLeds1: manualControlData.Led1,
                manualLeds2: manualControlData.Led2,
                manualLeds3: manualControlData.Led3,
                manualLeds4: manualControlData.Led4);
        }

        public void Dispose()
        {
            if (_hidDevice != null)
            {
                _hidDevice.Dispose();
                _hidDevice = null;
            }
        }

        private void EnsureConnected()
        {
            if (IsConnected)
            {
                return;
            }
            if (!IsConnected)
            {
                throw new DeviceUnavailableException();
            }
        }

        public async Task PlayAudioPattern(AudioPattern audioPattern, TimeSpan? durationTimeSpan)
        {
            EnsureConnected();
            var timespanIsZero = durationTimeSpan.HasValue && durationTimeSpan.Value.Ticks == 0;
            if (audioPattern == null || timespanIsZero)
            {
                await SendControlPacket(audioMode: 0, audioDuration: 0);
            }
            else
            {
                UInt16 duration = CalculateDurationFromTimeSpan(durationTimeSpan);
                await SendControlPacket(audioMode: (byte)audioPattern.Id, audioDuration: duration);
            }
        }

        public async Task PlayLightPattern(LedPattern lightPattern, TimeSpan? durationTimeSpan)
        {
            EnsureConnected();
            var timespanIsZero = durationTimeSpan.HasValue && durationTimeSpan.Value.Ticks == 0;
            if (lightPattern == null || timespanIsZero)
            {
                await SendControlPacket(ledMode: 0, ledDuration: 0);
            }
            else
            {
                UInt16 duration = CalculateDurationFromTimeSpan(durationTimeSpan);
                await SendControlPacket(ledMode: (byte)lightPattern.Id, ledDuration: duration);
            }
        }

        private UInt16 CalculateDurationFromTimeSpan(TimeSpan? durationTimeSpan)
        {
            if (durationTimeSpan == null)
            {
                return Duration_Forever;
            }
            UInt32 result = (UInt32)(durationTimeSpan.Value.TotalSeconds * 10.0);
            if (result > UInt16.MaxValue - 10)
            {
                return Duration_Forever;
            }
            return (UInt16)result;
        }
    }
}
