using ControlCAN;
using NLog;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ECanFDVci
{ 
    public enum SEND_MODE : byte
    {
        POSITIVE_SEND = 0,
        PASSIVE_SEND,
    }
     
    public enum RECEIVE_MODE : byte
    {
        SPECIFIED_RECEIVE = 0,
        GLOBAL_STANDARD_RECEIVE,
        GLOBAL_EXTENDED_RECEIVE,
        GLOBAL_STANDARD_AND_EXTENDED_RECEIVE,
    }
     
    public enum BAUD_RATE:byte
    {
        BAUDRATE_1M = 0,
        BAUDRATE_800K,
        BAUDRATE_500K,
        BAUDRATE_400K,
        BAUDRATE_250K,
        BAUDRATE_200K,
        BAUDRATE_125K,
        BAUDRATE_100K,
        BAUDRATE_80K,
        BAUDRATE_62500,
        BAUDRATE_50K,
        BAUDRATE_40K,
        BAUDRATE_25K,
        BAUDRATE_20K,
        BAUDRATE_10K,
        BAUDRATE_5K,
    }
     
    public enum DATA_RATE:byte
    {
        DATARATE_5M = 0,
        DATARATE_4M,
        DATARATE_2M,
        DATARATE_1M,
        DATARATE_800K,
        DATARATE_500K,
        DATARATE_400K,
        DATARATE_250K,
        DATARATE_200K,
        DATARATE_125K,
        DATARATE_100K,
        DATARATE_80K,
        DATARATE_62500,
        DATARATE_50K,
        DATARATE_40K,
        DATARATE_25K,
        DATARATE_20K,
        DATARATE_10K,
        DATARATE_5K,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CANFDFRAME_TYPE
    {
        private byte data; 

        // 1Bit proto (0CAN/1CANFD)
        public byte proto
        {
            get => (byte)(data & 1);
            set => data = (byte)(data & 0xFE | (value & 1)); 
        }

        // 1Bit format (0STD/1EXD)
        public byte format
        {
            get => (byte)((data >> 1) & 1);
            set => data = (byte)(data & 0xFD | ((value & 1) << 1)); 
        }

        // 1Bit type (0DATA/1RTR)
        public byte type
        {
            get => (byte)((data >> 2) & 1);
            set => data = (byte)(data & 0xFB | ((value & 1) << 2));  
        }

        // 1Bit bitratemode (bitrate switch 0off/1on)
        public byte bitratemode
        {
            get => (byte)((data >> 3) & 1);
            set => data = (byte)(data & 0xF7 | ((value & 1) << 3));  
        }
    }

    // CANFD INIT
    [StructLayout(LayoutKind.Sequential)]
    public struct INIT_CONFIG
    {
        public RECEIVE_MODE CanReceMode;
        public SEND_MODE CanSendMode;
        public uint NominalBitRate;
        public uint DataBitRate;
        public byte FilterUsedBits;
        public byte StdOrExdBits;
        public BAUD_RATE NominalBitRateSelect;
        public DATA_RATE DataBitRateSelect;
        // CanReceMode == SPECIFIED_RECEIVE 
        public uint StandardORExtendedfilter1; // Standard Or Extended filter1
        public uint StandardORExtendedfilter1Mask; // Standard Or Extended filter1 Mask
        public uint StandardORExtendedfilter2; // Extended Or Extended filter2
        public uint StandardORExtendedfilter2Mask; // Extended Or Extended filter2 Mask
        public uint StandardORExtendedfilter3; // Standard Or Extended filter3
        public uint StandardORExtendedfilter3Mask; // Standard Or Extended filter3 Mask
        public uint StandardORExtendedfilter4; // Extended Or Extended filter4
        public uint StandardORExtendedfilter4Mask; // Extended Or Extended filter4 Mask
        public uint StandardORExtendedfilter5; // Standard Or Extended filter5
        public uint StandardORExtendedfilter5Mask; // Standard Or Extended filter5 Mask
        public uint StandardORExtendedfilter6; // Extended Or Extended filter6
        public uint StandardORExtendedfilter6Mask; // Extended Or Extended filter6 Mask
        public uint StandardORExtendedfilter7; // Standard Or Extended filter7
        public uint StandardORExtendedfilter7Mask; // Standard Or Extended filter7 Mask
        public uint StandardORExtendedfilter8; // Extended Or Extended filter8
        public uint StandardORExtendedfilter8Mask; // Extended Or Extended filter8 Mask
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct TIMESTAMP_TYPE
    {
        public byte mday;
        public byte hour;
        public byte minute;
        public byte second;
        public ushort millisecond;
        public ushort microsecond;
    }

    // CANFD OBJ
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CANFD_OBJ
    {
        public CANFDFRAME_TYPE CanORCanfdType; // 
        public byte DataLen; // DLC
        public fixed byte Reserved[2]; // 
        public uint ID; // CANID
        public TIMESTAMP_TYPE TimeStamp; // TS
        public fixed byte Data[64]; // DATA
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CANFD_ECR_TYPE
    {
        private uint data;

        // 8Bit TEC
        public byte TEC
        {
            get => (byte)(data & 0xFF);
            set => data = (data & 0xFFFFFF00) | (uint)(value & 0xFF);
        }

        // 7Bit REC
        public byte REC
        {
            get => (byte)((data >> 8) & 0x7F);
            set => data = (data & 0xFFFF80FF) | ((uint)(value & 0x7F) << 8);
        }

        // 1Bit RP
        public byte RP
        {
            get => (byte)((data >> 15) & 0x01);
            set => data = (data & 0xFFFF7FFF) | ((uint)(value & 0x01) << 15);
        }

        // 8Bit CEL
        public byte CEL
        {
            get => (byte)((data >> 16) & 0xFF);
            set => data = (data & 0xFF00FFFF) | ((uint)(value & 0xFF) << 16);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CANFD_PSR_TYPE
    {
        private uint data;

        // 3Bit LEC
        public byte LEC
        {
            get => (byte)(data & 0x07);
            set => data = (data & 0xFFFFFFF8) | (uint)(value & 0x07);
        }

        // 2Bit ACT
        public byte ACT
        {
            get => (byte)((data >> 3) & 0x03);
            set => data = (data & 0xFFFFE3FF) | ((uint)(value & 0x03) << 3);
        }

        // 1Bit EP
        public byte EP
        {
            get => (byte)((data >> 5) & 0x01);
            set => data = (data & 0xFFFFFFDF) | ((uint)(value & 0x01) << 5);
        }

        // 1Bit EW
        public byte EW
        {
            get => (byte)((data >> 6) & 0x01);
            set => data = (data & 0xFFFFFFBF) | ((uint)(value & 0x01) << 6);
        }

        // 1Bit BO
        public byte BO
        {
            get => (byte)((data >> 7) & 0x01);
            set => data = (data & 0xFFFFFF7F) | ((uint)(value & 0x01) << 7);
        }

        // 3Bit DLEC
        public byte DLEC
        {
            get => (byte)((data >> 8) & 0x07);
            set => data = (data & 0xFFFFF1FF) | ((uint)(value & 0x07) << 8);
        }

        // 1Bit RESI
        public byte RESI
        {
            get => (byte)((data >> 11) & 0x01);
            set => data = (data & 0xFFFFDFFF) | ((uint)(value & 0x01) << 11);
        }

        // 1Bit RBRS
        public byte RBRS
        {
            get => (byte)((data >> 12) & 0x01);
            set => data = (data & 0xFFFFEFFF) | ((uint)(value & 0x01) << 12);
        }

        // 1Bit RFDF
        public byte RFDF
        {
            get => (byte)((data >> 13) & 0x01);
            set => data = (data & 0xFFFFDFFF) | ((uint)(value & 0x01) << 13);
        }

        // 1Bit PXE
        public byte PXE
        {
            get => (byte)((data >> 14) & 0x01);
            set => data = (data & 0xFFFFBFFF) | ((uint)(value & 0x01) << 14);
        }

        // 7Bit TDCV
        public byte TDCV
        {
            get => (byte)((data >> 16) & 0x7F);
            set => data = (data & 0xFF80FFFF) | ((uint)(value & 0x7F) << 16);
        }

    }

    // CANFD error
    [StructLayout(LayoutKind.Sequential)]
    public struct ERR_FRAME
    {
        public TIMESTAMP_TYPE can_timestamp; //  
        public CANFD_ECR_TYPE can_ecr_register; //  
        public CANFD_PSR_TYPE can_psr_register; //  
    }

    // CANFD Status
    [StructLayout(LayoutKind.Sequential)]
    public struct CANFD_STATUS
    {
        public ushort LeftSendBufferNum; // 
        public TIMESTAMP_TYPE can0_timestamp; //  
        public CANFD_ECR_TYPE can0_ecr_register; //  
        public CANFD_PSR_TYPE can0_psr_register; //  
        public uint can0_RxLost_Cnt; //  
        public uint can0_TxFail_Cnt; //  
        public float can0_Load_Rate; //  

        public TIMESTAMP_TYPE can1_timestamp; //  
        public CANFD_ECR_TYPE can1_ecr_register; //  
        public CANFD_PSR_TYPE can1_psr_register; //  
        public uint can1_RxLost_Cnt; //  
        public uint can1_TxFail_Cnt; //  
        public float can1_Load_Rate; //  
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct BOARD_INFO
    {
        public ushort hw_Version;
        public ushort fw_Version;
        public ushort dr_Version;
        public ushort in_Version;
        public ushort irq_Num;
        public byte can_Num;
        public fixed byte str_Serial_Num[20];
        public fixed byte str_hw_Type[40];
        public fixed ushort Reserved[4];
    }

    public unsafe static class ECanFDVci
    {
        public const int CANFD_DEVICE_MAX_NUMBER = 16;  
        public const int CANFD_ERRFRAME_MAX_NUMBER = 1000; 
        public const int CANFD_RECEFBUFFER_MAX_NUMBER = 10000; 

        public const int CAN_0 = 0;
        public const int CAN_1 = 1;

        public const int NONUSE = 0;
        public const int USED = 1;

        public const int STATUS_OK = 0;
        public const int ERR_CAN_NOINIT = 0x0001;
        public const int ERR_CAN_DISABLE = 0x0002;
        public const int ERR_CAN_BUSOFF = 0x0004;

        public const int ERR_DATA_LEN = 0x0010;
        public const int ERR_USB_WRITE = 0x0020;
        public const int ERR_USB_READ = 0x0040;

        public const int ERR_DEVICEOPENED = 0x0100;
        public const int ERR_DEVICEOPEN = 0x0200;
        public const int ERR_DEVICENOTOPEN = 0x0400;
        public const int ERR_BUFFEROVERFLOW = 0x0800;
        public const int ERR_DEVICENOTEXIST = 0x1000;
        public const int ERR_DEVICECLOSE = 0x2000;

        private static ILogger logger = LogManager.GetCurrentClassLogger();
        private static Dictionary<uint, SerialCAN> serialPorts = new Dictionary<uint, SerialCAN>();


        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(OpenDeviceFD))]
        public static uint OpenDeviceFD(uint DeviceType, uint DeviceInd)
        {
            logger.Info($"OpenDeviceFD DeviceType:{DeviceType},DeviceInd:{DeviceInd}");
            if (!serialPorts.ContainsKey(DeviceInd))
            {
                var serial = new SerialCAN();
                if (serial.Open((int)DeviceInd) == 1)
                {
                    serialPorts[DeviceInd] = serial;
                    return STATUS_OK;
                }
            }
            return ERR_DEVICEOPEN;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(CloseDeviceFD))]
        public static uint CloseDeviceFD(uint DeviceType, uint DeviceInd)
        {
            logger.Info($"CloseDeviceFD DeviceType:{DeviceType},DeviceInd:{DeviceInd}");

            if (serialPorts.TryGetValue(DeviceInd, out var serial))
            {
                serial.Close();
                serialPorts.Remove(DeviceInd);
                return STATUS_OK;
            }
            return ERR_DEVICECLOSE;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(InitCANFD))]
        public static uint InitCANFD(uint DeviceType, uint DeviceInd, byte CANInd, INIT_CONFIG* pInitConfig)
        {
            logger.Info($"InitCANFD DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANId:{CANInd}");

            if (serialPorts.TryGetValue(DeviceInd, out var device))
            {
                if (CANInd == 0)
                {
                    if (device.InitCAN(CANInd, new SerialConfig()) == 1)
                        return STATUS_OK;
                    else
                        return ERR_DEVICEOPEN;
                }
                return STATUS_OK;
            }
            return ERR_DEVICENOTEXIST;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(TransmitFD))]
        public static uint TransmitFD(uint DeviceType, uint DeviceInd, byte CANInd, CANFD_OBJ* pCanfdMQ, uint Len)
        {
            logger.Info($"TransmitFD DeviceType:{DeviceType},DeviceInd:{DeviceInd},Len:{Len}");
            if (serialPorts.TryGetValue(DeviceInd, out var device))
            {
                try
                {
                    for (uint i = 0; i < Len;)
                    {
                        var frame = new SerialFrame
                        {
                            Id = pCanfdMQ[i].ID,
                            Dlc = pCanfdMQ[i].DataLen,
                            Ext = pCanfdMQ[i].CanORCanfdType.format,
                            Rtr = pCanfdMQ[i].CanORCanfdType.type,
                            Data = new byte[8],
                        };
                        Marshal.Copy((nint)pCanfdMQ[i].Data, frame.Data, 0, 8);
                        var ret = (uint)device.TransmitCAN(CANInd, [frame], 1);
                        if (ret == 1) i++;
                    }
                    logger.Info($"TransmitFD DeviceType:{DeviceType},DeviceInd:{DeviceInd} success");
                    return STATUS_OK;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"TransmitFD DeviceType:{DeviceType},DeviceInd:{DeviceInd} error");
                }
            }
            return ERR_DEVICENOTEXIST;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(Receive_buffer_thread))]
        public static uint Receive_buffer_thread(uint DeviceType, uint DeviceInd, uint WaitTime)
        {
            logger.Info($"Receive_buffer_thread DeviceType:{DeviceType},DeviceInd:{DeviceInd},WaitTime:{WaitTime}");
            return STATUS_OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(ReceiveFD))]
        public static uint ReceiveFD(uint DeviceType, uint DeviceInd, byte CANInd, CANFD_OBJ* pCanfdMQ, uint* Len)
        {
            logger.Info($"ReceiveFD DeviceType:{DeviceType},DeviceInd:{DeviceInd},Len:{*Len}");
            if (serialPorts.TryGetValue(DeviceInd, out var device))
            {
                uint i = 0;
                int count = *Len == 0 ? Math.Min(100, device.GetReceiveCAN(CANInd)) : (int)*Len;
                for (; i < count;)
                {
                    var frames = new SerialFrame[1];
                    if (device.ReceiveCAN(CANInd, frames, 1) > 0)
                    {
                        var ts = DateTime.Now;
                        pCanfdMQ[i].TimeStamp.microsecond = (ushort)ts.Microsecond;
                        pCanfdMQ[i].TimeStamp.millisecond = (ushort)ts.Millisecond;
                        pCanfdMQ[i].TimeStamp.second = (byte)ts.Second;
                        pCanfdMQ[i].TimeStamp.minute = (byte)ts.Minute;
                        pCanfdMQ[i].TimeStamp.hour = (byte)ts.Hour;
                        pCanfdMQ[i].TimeStamp.mday = (byte)ts.Day;

                        pCanfdMQ[i].CanORCanfdType.proto = 0;
                        pCanfdMQ[i].CanORCanfdType.format = frames[0].Ext;
                        pCanfdMQ[i].CanORCanfdType.type = frames[0].Rtr;
                        pCanfdMQ[i].DataLen = frames[0].Dlc;
                        pCanfdMQ[i].ID = frames[0].Id;
                        Marshal.Copy(frames[0].Data, 0, (nint)pCanfdMQ[i].Data, 8);
                        i++;
                    }
                }
                *Len = i;
                return STATUS_OK;
            }
            return ERR_DEVICENOTEXIST;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(GetErrFrame))]
        public static uint GetErrFrame(uint DeviceType, uint DeviceInd, byte CANInd, ERR_FRAME* pCanfdErrbuffer, uint* Len)
        {
            logger.Info($"GetErrFrame DeviceType:{DeviceType},DeviceInd:{DeviceInd},Len:{*Len}");

            *Len = 0;
            return STATUS_OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(ResetCANFD))]
        public static uint ResetCANFD(uint DeviceType, uint DeviceInd, byte CANInd)
        {
            logger.Info($"ResetCANFD DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANInd:{CANInd}");

            if (serialPorts.TryGetValue(DeviceInd, out var device))
            {
                if (device.ResetCAN(CANInd) == 1)
                    return STATUS_OK;
                return ERR_DEVICENOTOPEN;
            }
            return ERR_DEVICENOTEXIST;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(StartCANFD))]
        public static uint StartCANFD(uint DeviceType, uint DeviceInd, byte CANInd)
        {
            logger.Info($"StartCANFD DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANInd:{CANInd}");

            if (serialPorts.TryGetValue(DeviceInd, out var device))
            {
                if (device.StartCAN(CANInd) == 1)
                    return STATUS_OK;
                return ERR_DEVICENOTOPEN;
            }
            return ERR_DEVICENOTEXIST;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(StopCANFD))]
        public static uint StopCANFD(uint DeviceType, uint DeviceInd, byte CANInd)
        {
            logger.Info($"StopCANFD DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANInd:{CANInd}");
            if (serialPorts.TryGetValue(DeviceInd, out var device))
            {
                if (device.ClearBufferCAN(CANInd) == 1)
                    return STATUS_OK;
                return ERR_DEVICENOTOPEN;
            }
            return STATUS_OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(GetCanfdBusStatus))]
        public static uint GetCanfdBusStatus(uint DeviceType, uint DeviceInd, CANFD_STATUS* p_canfd_status) // 获取CANFD设备状态信息
        {
            logger.Info($"GetCanfdBusStatus DeviceType:{DeviceType},DeviceInd:{DeviceInd}");

            return STATUS_OK;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = nameof(GetReference))]
        public static uint GetReference(uint DeviceType, uint DeviceInd, byte CANInd, uint RefType, BOARD_INFO* pInfo) // 获取CANFD设备状态信息
        {
            logger.Info($"GetReference DeviceType:{DeviceType},DeviceInd:{DeviceInd},CANInd:{CANInd},RefType:{RefType}");
            if (serialPorts.TryGetValue(DeviceInd, out var device))
            {
                if (device.ReadInfo(out var info) == 1)
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
                    return STATUS_OK;
                }
                return ERR_DEVICENOTOPEN;
            }
            return ERR_DEVICENOTEXIST;
        }
    }
}
