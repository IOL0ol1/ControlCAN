using System.Runtime.InteropServices;

namespace VCI
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VCI_BOARD_INFO
    {
        public UInt16 hw_Version;
        public UInt16 fw_Version;
        public UInt16 dr_Version;
        public UInt16 in_Version;
        public UInt16 irq_Num;
        public byte can_Num;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] str_Serial_Num;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] str_hw_Type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VCI_CAN_OBJ
    {
        public uint ID;
        public uint TimeStamp;
        public byte TimeFlag;
        public byte SendType;
        public byte RemoteFlag;
        public byte ExternFlag;
        public byte DataLen;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Data;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VCI_CAN_STATUS
    {
        public byte ErrInterrupt;
        public byte regMode;
        public byte regStatus;
        public byte regALCapture;
        public byte regECCapture;
        public byte regEWLimit;
        public byte regRECounter;
        public byte regTECounter;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VCI_ERR_INFO
    {
        public UInt32 ErrCode;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Passive_ErrData;
        public byte ArLost_ErrData;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VCI_INIT_CONFIG
    {
        public UInt32 AccCode;
        public UInt32 AccMask;
        public UInt32 Reserved;
        public byte Filter;
        public byte Timing0;
        public byte Timing1;
        public byte Mode;
    }
    public enum VCI_STATUS : UInt32
    {
        OK = 1,
        ERR = 0,
    }
    public class ControlCAN
    {
        public const string DllName = "ControlCAN";
        [DllImport(DllName)]
        public static extern UInt32 VCI_OpenDevice(UInt32 DeviceType, UInt32 DeviceInd, UInt32 Reserved);
        [DllImport(DllName)]
        public static extern UInt32 VCI_CloseDevice(UInt32 DeviceType, UInt32 DeviceInd);
        [DllImport(DllName)]
        public static extern UInt32 VCI_InitCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, [In] VCI_INIT_CONFIG pInitConfig);
        [DllImport(DllName)]
        public static extern UInt32 VCI_ReadBoardInfo(UInt32 DeviceType, UInt32 DeviceInd, ref VCI_BOARD_INFO pInfo);
        [DllImport(DllName)]
        public static extern UInt32 VCI_ReadErrInfo(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_ERR_INFO pErrInfo);
        [DllImport(DllName)]
        public static extern UInt32 VCI_ReadCANStatus(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, ref VCI_CAN_STATUS pCANStatus);
        [DllImport(DllName)]
        public static extern UInt32 VCI_GetReference(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, UInt32 RefType, IntPtr pData);
        [DllImport(DllName)]
        public static extern UInt32 VCI_SetReference(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, UInt32 RefType, IntPtr pData);
        [DllImport(DllName)]
        public static extern UInt32 VCI_GetReceiveNum(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);
        [DllImport(DllName)]
        public static extern UInt32 VCI_ClearBuffer(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);
        [DllImport(DllName)]
        public static extern UInt32 VCI_StartCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);
        [DllImport(DllName)]
        public static extern UInt32 VCI_ResetCAN(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd);
        [DllImport(DllName)]
        public static extern UInt32 VCI_Transmit(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, [In] VCI_CAN_OBJ[] pSend, UInt32 Len);
        [DllImport(DllName)]
        public static extern UInt32 VCI_Receive(UInt32 DeviceType, UInt32 DeviceInd, UInt32 CANInd, [Out] VCI_CAN_OBJ[] pReceive, UInt32 Len, Int32 WaitTime = Timeout.Infinite);
    }
}