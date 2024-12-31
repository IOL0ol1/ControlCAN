using NLog;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BYTE = byte;
using DWORD = int;
using INT = int;
using UCHAR = byte;
using UINT = uint;
using ULONG = uint;
using USHORT = ushort;

namespace ControlCAN
{
    public enum VCI_ERR : DWORD
    {
        /* CAN error codes */
        CAN_OVERFLOW = 0x0001,  // CAN controller internal FIFO overflow
        CAN_ERRALARM = 0x0002,  // CAN controller error alarm
        CAN_PASSIVE = 0x0004,  // CAN controller passive error
        CAN_LOSE = 0x0008,  // CAN controller arbitration lost
        CAN_BUSERR = 0x0010,  // CAN controller bus error
        CAN_BUSOFF = 0x0020,  // Bus off error
        CAN_BUFFER_OVERFLOW = 0x0040,  // CAN controller internal BUFFER overflow
        /* General error codes */
        DEVICEOPENED = 0x0100,  // Device already opened
        DEVICEOPEN = 0x0200,  // Open device error
        DEVICENOTOPEN = 0x0400,  // Device not opened
        BUFFEROVERFLOW = 0x0800,  // Buffer overflow
        DEVICENOTEXIST = 0x1000,  // Device does not exist
        LOADKERNELDLL = 0x2000,  // Load dynamic library failed
        CMDFAILED = 0x4000,  // Execute command failed error code
        BUFFERCREATE = 0x8000,  // Insufficient memory
    }

    // Function call return status values
    public enum VCI_STATUS : DWORD
    {
        OK = 1,
        ERR = 0,
    }

    // 1. Data type of ZLGCAN series interface card information.
    public unsafe struct VCI_BOARD_INFO
    {
        public USHORT hw_Version;
        public USHORT fw_Version;
        public USHORT dr_Version;
        public USHORT in_Version;
        public USHORT irq_Num;
        public BYTE can_Num;
        public fixed BYTE str_Serial_Num[20];
        public fixed BYTE str_hw_Type[40];
        public fixed USHORT Reserved[4];
    }

    // 2. Define the data type of CAN information frame.
    public unsafe struct VCI_CAN_OBJ
    {
        public UINT ID;
        public UINT TimeStamp;
        public BYTE TimeFlag;
        public BYTE SendType;
        public BYTE RemoteFlag; // Is it a remote frame
        public BYTE ExternFlag; // Is it an extended frame
        public BYTE DataLen;
        public fixed BYTE Data[8];
        public fixed BYTE Reserved[3];    // Reserved[0] The 0th bit indicates a special blank line or highlighted frame
    }

    // 3. Define the data type of CAN controller status.
    public struct VCI_CAN_STATUS
    {
        public UCHAR ErrInterrupt;
        public UCHAR regMode;
        public UCHAR regStatus;
        public UCHAR regALCapture;
        public UCHAR regECCapture;
        public UCHAR regEWLimit;
        public UCHAR regRECounter;
        public UCHAR regTECounter;
        public DWORD Reserved;
    }

    // 4. Define the data type of error information.
    public unsafe struct VCI_ERR_INFO
    {
        public UINT ErrCode;
        public fixed BYTE Passive_ErrData[3];
        public BYTE ArLost_ErrData;
    }

    // 5. Define the data type for initializing CAN
    public struct VCI_INIT_CONFIG
    {
        public DWORD AccCode;
        public DWORD AccMask;
        public DWORD Reserved;
        public UCHAR Filter;
        public UCHAR Timing0;
        public UCHAR Timing1;
        public UCHAR Mode;
    }

    ///////// new add struct for filter /////////
    public struct VCI_FILTER_RECORD
    {
        public DWORD ExtFrame;   // Is it an extended frame
        public DWORD Start;
        public DWORD End;
    }

    // Timed auto-send frame structure
    public struct VCI_AUTO_SEND_OBJ
    {
        public BYTE Enable;     // Enable this message 0: disable 1: enable
        public BYTE Index;      // Message number, supports up to 32 messages
        public DWORD Interval;   // Timed send time in milliseconds
        public VCI_CAN_OBJ Obj;    // Message
    }

    public unsafe static class ControlCAN
    {
        private static ILogger logger = LogManager.GetCurrentClassLogger();
        private static Dictionary<DWORD, SerialCAN> serialPorts = new Dictionary<DWORD, SerialCAN>();
        private static DateTime openTime = DateTime.Now;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(VCI_OpenDevice))]
        public static DWORD VCI_OpenDevice(DWORD DeviceType, DWORD DeviceInd, DWORD Reserved)
        {
            logger.Info($"VCI_OpenDevice DeviceType:{DeviceType},DeviceInd:{DeviceInd}");
            if (!serialPorts.ContainsKey(DeviceInd))
            {
                var serial = new SerialCAN();
                if (serial.Open(DeviceInd) == 1)
                {
                    openTime = DateTime.Now;
                    serialPorts[DeviceInd] = serial;
                    return (DWORD)VCI_STATUS.OK;
                }
            }
            return (DWORD)VCI_STATUS.ERR;
        }
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(VCI_CloseDevice))]
        public static DWORD VCI_CloseDevice(DWORD DeviceType, DWORD DeviceInd)
        {
            logger.Info($"VCI_CloseDevice DeviceType:{DeviceType},DeviceInd:{DeviceInd}");

            if (serialPorts.TryGetValue(DeviceInd, out var serial))
            {
                serial.Close();
                serialPorts.Remove(DeviceInd);
            }
            return (DWORD)VCI_STATUS.OK;
        }

        public static Dictionary<ushort, int> timings = new Dictionary<ushort, int>
        {
            [0xBFFF] = 5000,
            [0x311C] = 10000,
            [0x181C] = 20000,
            [0x87FF] = 40000,
            [0x091C] = 50000,
            [0x83FF] = 80000,
            [0x041C] = 100000,
            [0x031C] = 125000,
            [0x81FA] = 200000,
            [0x011C] = 250000,
            [0x80FA] = 400000,
            [0x001C] = 500000,
            [0x80B6] = 666000,
            [0x0016] = 800000,
            [0x0014] = 1000000,
        };

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(VCI_InitCAN))]
        public static DWORD VCI_InitCAN(DWORD DeviceType, DWORD DeviceInd, DWORD CANInd, VCI_INIT_CONFIG* pInitConfig)
        {
            logger.Info($"VCI_InitCAN DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANId:{CANInd}");

            if (serialPorts.TryGetValue(DeviceInd, out var device))
            {
                var config = new SerialConfig { Mode = pInitConfig->Mode };
                var bkey = (ushort)((pInitConfig->Timing0 << 8) | pInitConfig->Timing1);
                if (timings.TryGetValue(bkey, out var bps))
                    config.BaudRate = bps;
                return device.InitCAN(CANInd, new SerialConfig { Mode = pInitConfig->Mode });
            }
            return (DWORD)VCI_STATUS.ERR;
        }
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(VCI_ReadBoardInfo))]
        public static DWORD VCI_ReadBoardInfo(DWORD DeviceType, DWORD DeviceInd, VCI_BOARD_INFO* pInfo)
        {
            logger.Info($"VCI_ReadBoardInfo DeviceType:{DeviceType},DeviceInd:{DeviceInd}");

            if (serialPorts.TryGetValue(DeviceInd, out var device))
            {
                var ret = device.ReadInfo(out var info);
                if (ret == (DWORD)VCI_STATUS.OK)
                {
                    pInfo->can_Num = info.CanNum;
                    pInfo->dr_Version = info.DrVersion;
                    pInfo->fw_Version = info.FwVersion;
                    pInfo->hw_Version = info.HwVersion;
                    pInfo->in_Version = info.InVersion;
                    pInfo->irq_Num = info.IrqNum;
                    var sn = Encoding.ASCII.GetBytes(info.SerialNum);
                    Marshal.Copy(sn, 0, (nint)pInfo->str_Serial_Num, sn.Length);
                    var hw = Encoding.ASCII.GetBytes(info.HwType);
                    Marshal.Copy(hw, 0, (nint)pInfo->str_hw_Type, hw.Length);
                }
                return ret;
            }
            return (DWORD)VCI_STATUS.ERR;
        }
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(VCI_ReadErrInfo))]
        public static DWORD VCI_ReadErrInfo(DWORD DeviceType, DWORD DeviceInd, DWORD CANInd, VCI_ERR_INFO* pErrInfo)
        {
            logger.Info($"VCI_ReadErrInfo DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANId:{CANInd}");
            pErrInfo->ErrCode = 0;
            pErrInfo->ArLost_ErrData = 0;
            pErrInfo->Passive_ErrData[0] = 0;
            pErrInfo->Passive_ErrData[1] = 0;
            pErrInfo->Passive_ErrData[2] = 0;
            return (DWORD)VCI_STATUS.OK;
        }
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(VCI_ReadCANStatus))]
        public static DWORD VCI_ReadCANStatus(DWORD DeviceType, DWORD DeviceInd, DWORD CANInd, VCI_CAN_STATUS* pCANStatus)
        {
            logger.Info($"VCI_ReadCANStatus DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANId:{CANInd}");

            return (DWORD)VCI_STATUS.OK;
        }
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(VCI_GetReference))]
        public static DWORD VCI_GetReference(DWORD DeviceType, DWORD DeviceInd, DWORD CANInd, DWORD RefType, void* pData)
        {
            logger.Info($"VCI_GetReference DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANId:{CANInd},RefType:{RefType}");

            if (serialPorts.TryGetValue(DeviceInd, out var device))
                return device.GetReference(CANInd, RefType, (nint)pData);
            return (DWORD)VCI_STATUS.ERR;
        }
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(VCI_SetReference))]
        public static DWORD VCI_SetReference(DWORD DeviceType, DWORD DeviceInd, DWORD CANInd, DWORD RefType, void* pData)
        {
            logger.Info($"VCI_SetReference DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANId:{CANInd},RefType:{RefType}");

            if (serialPorts.TryGetValue(DeviceInd, out var device))
                return device.SetReference(CANInd, RefType, (nint)pData);
            return (DWORD)VCI_STATUS.ERR;
        }
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(VCI_GetReceiveNum))]
        public static DWORD VCI_GetReceiveNum(DWORD DeviceType, DWORD DeviceInd, DWORD CANInd)
        {
            logger.Info($"VCI_GetReceiveNum DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANId:{CANInd}");
            if (serialPorts.TryGetValue(DeviceInd, out var device))
                return device.GetReceiveCAN(CANInd);
            return (DWORD)VCI_STATUS.ERR;
        }
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(VCI_ClearBuffer))]
        public static DWORD VCI_ClearBuffer(DWORD DeviceType, DWORD DeviceInd, DWORD CANInd)
        {
            logger.Info($"VCI_ClearBuffer DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANId:{CANInd}");

            if (serialPorts.TryGetValue(DeviceInd, out var device))
                return device.ClearBufferCAN(CANInd);
            return (DWORD)VCI_STATUS.ERR;
        }
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(VCI_StartCAN))]
        public static DWORD VCI_StartCAN(DWORD DeviceType, DWORD DeviceInd, DWORD CANInd)
        {
            logger.Info($"VCI_StartCAN DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANId:{CANInd}");

            if (serialPorts.TryGetValue(DeviceInd, out var device))
                return device.StartCAN(CANInd);
            return (DWORD)VCI_STATUS.ERR;
        }
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(VCI_ResetCAN))]
        public static DWORD VCI_ResetCAN(DWORD DeviceType, DWORD DeviceInd, DWORD CANInd)
        {
            logger.Info($"VCI_ResetCAN DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANId:{CANInd}");

            if (serialPorts.TryGetValue(DeviceInd, out var device))
                return device.ResetCAN(CANInd);
            return (DWORD)VCI_STATUS.ERR;
        }
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(VCI_Transmit))]
        public static ULONG VCI_Transmit(DWORD DeviceType, DWORD DeviceInd, DWORD CANInd, VCI_CAN_OBJ* pSend, ULONG Len)
        {
            logger.Info($"VCI_Transmit DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANId:{CANInd}");

            if (serialPorts.TryGetValue(DeviceInd, out var device))
            {
                var frames = new SerialFrame[Len];
                for (ulong i = 0; i < Len; i++)
                {
                    frames[i] = new SerialFrame
                    {
                        Id = pSend[i].ID,
                        Dlc = pSend[i].DataLen,
                        Ext = pSend[i].ExternFlag,
                        Rtr = pSend[i].RemoteFlag,
                        Data = new byte[8],
                    };
                    Marshal.Copy((nint)pSend[i].Data, frames[i].Data, 0, 8);
                }
                return (ULONG)device.TransmitCAN(CANInd, frames, (int)Len);
            }
            return (DWORD)VCI_STATUS.ERR;
        }
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(VCI_Receive))]
        public static ULONG VCI_Receive(DWORD DeviceType, DWORD DeviceInd, DWORD CANInd, VCI_CAN_OBJ* pReceive, ULONG Len, INT WaitTime = -1)
        {
            logger.Info($"VCI_Receive DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANId:{CANInd},Len:{Len},WaitTime:{WaitTime}");

            if (serialPorts.TryGetValue(DeviceInd, out var device))
            {
                var frames = new SerialFrame[Len];
                var len = device.ReceiveCAN(CANInd, frames, (int)Len, WaitTime);
                for (int i = 0; i < len; i++)
                {
                    pReceive[i].ID = frames[i].Id;
                    pReceive[i].DataLen = (byte)frames[i].Dlc;
                    pReceive[i].ExternFlag = frames[i].Ext;
                    pReceive[i].RemoteFlag = frames[i].Rtr;
                    pReceive[i].TimeFlag = 1;
                    pReceive[i].TimeStamp = (uint)((frames[i].Time - openTime).TotalMicroseconds / 100);
                    Marshal.Copy(frames[i].Data, 0, (nint)pReceive[i].Data, 8);
                }
                return (ULONG)len;
            }
            return (ULONG)VCI_STATUS.ERR;
        }
    }
}