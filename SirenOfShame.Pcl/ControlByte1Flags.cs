using System;

namespace SirenOfShame.Pcl
{
    [Flags]
    public enum ControlByte1Flags : byte
    {
        Ignore = 0xff,
        FirmwareUpgrade = 0x01,
        EchoOn = 0x02,
        Debug = 0x04
    }
}