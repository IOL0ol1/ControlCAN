using NLog;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;

namespace ControlCAN
{
    public struct SerialInfo
    {
        public ushort HwVersion;
        public ushort FwVersion;
        public ushort DrVersion;
        public ushort InVersion;
        public ushort IrqNum;
        public byte CanNum;
        public string SerialNum;
        public string HwType;
    }


    public struct SerialFilter
    {
        /// <summary>
        /// Filter 0-13
        /// </summary>
        public byte Number;
        /// <summary>
        /// Enable Filter
        /// </summary>
        public bool Enable;
        /// <summary>
        /// 0:Mask 1:List
        /// </summary>
        public byte Mode;
        /// <summary>
        /// 0-0xFFFFFFFF
        /// </summary>
        public uint Filter;
        /// <summary>
        /// 0-0xFFFFFFFF
        /// </summary>
        public uint Mask;
    }

    public struct SerialConfig
    {
        public SerialConfig()
        {
            BaudRate = 1000000;
            Mode = 0;
            Filters = new List<SerialFilter>();
        }
        /// <summary>
        /// bps,default 1000000
        /// </summary>
        public int BaudRate;
        /// <summary>
        /// 0:Normal(default), 1:Loopback
        /// </summary>
        public byte Mode;
        /// <summary>
        /// Filter list(default Empty)
        /// </summary>
        public List<SerialFilter> Filters;
    }

    public struct SerialFrame
    {
        public uint Id;
        public byte Rtr;
        public byte Ext;
        public byte Dlc;
        public byte[] Data;
    }

    public unsafe class SerialCAN
    {
        private readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly BlockingCollection<SerialFrame> messages = new BlockingCollection<SerialFrame>();
        private readonly List<byte> received = new List<byte>();
        private readonly SerialPort serialPort = new SerialPort() { ReadTimeout = 500, NewLine = "\r\n", BaudRate = 9600, Encoding = Encoding.ASCII };
        private bool isStart = false;

        /// <summary>
        /// Open serial port
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public int Open(int port)
        {
            try
            {
                logger.Info($"Open COM{port} start");
                lock (serialPort)
                {
                    serialPort.PortName = $"COM{port}";
                    serialPort.Open();
                    serialPort.Close();
                }
                logger.Info($"Open COM{port} success");
                return 1;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Open COM{port} error");
                return 0;
            }
        }

        /// <summary>
        /// Close serial port
        /// </summary>
        /// <returns></returns>
        public int Close()
        {
            try
            {
                logger.Info($"Close {serialPort.PortName} start");
                lock (serialPort)
                {
                    if (isStart)
                    {
                        serialPort.DataReceived -= SerialPort_DataReceived;
                        isStart = false;
                    }
                    if (serialPort.IsOpen)
                        serialPort.Close();
                }
                logger.Info($"Close {serialPort.PortName} success");
                return 1;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Close {serialPort.PortName} error");
                return 0;
            }
        }

        public int ReadInfo(out SerialInfo info)
        {
            info = new SerialInfo();
            try
            {
                logger.Info($"ReadInfo {serialPort.PortName} start");
                info.HwVersion = 5;
                info.FwVersion = 4;
                info.DrVersion = 3;
                info.IrqNum = 0;
                info.CanNum = 1;
                info.SerialNum = $"Serial_{serialPort.PortName}";
                info.HwType = $"Serial_{serialPort.PortName}";
                logger.Info($"ReadInfo {serialPort.PortName} success");
                return 1;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"ReadInfo {serialPort.PortName} error");
                return 0;
            }
        }

        public int InitCAN(int canIndex, SerialConfig config)
        {
            try
            {
                logger.Info($"InitCAN {serialPort.PortName} {canIndex} start");
                if (canIndex == 0)
                {
                    lock (serialPort)
                    {
                        if (!serialPort.IsOpen)
                            serialPort.Open();
                        serialPort.DiscardInBuffer();
                        serialPort.DiscardOutBuffer();
                        serialPort.WriteLine("AT+CG"); // config mode
                        logger.Info("AT+CG");
                        if (!serialPort.ReadLine().Contains("OK"))
                            return 0;

                        logger.Info($"AT+CAN_BAUD={config.BaudRate}");
                        serialPort.WriteLine($"AT+CAN_BAUD={config.BaudRate}");
                        if (!serialPort.ReadLine().Contains("OK"))
                            return 0;

                        logger.Info($"AT+CAN_MODE={config.Mode}");
                        serialPort.WriteLine($"AT+CAN_MODE={config.Mode}");
                        if (!serialPort.ReadLine().Contains("OK"))
                            return 0;

                        if (config.Filters != null)
                        {
                            foreach (var filter in config.Filters)
                            {
                                var filterConfig = $"AT+CAN_FILTER{filter.Number}={(filter.Enable ? 1 : 0)},{filter.Mode},{filter.Filter},{filter.Mask}";
                                logger.Info(filterConfig);
                                serialPort.WriteLine(filterConfig);
                                if (!serialPort.ReadLine().Contains("OK"))
                                    return 0;
                            }
                        }

                        logger.Info($"AT+AT");
                        serialPort.WriteLine("AT+AT");  // cmd mode
                        if (!serialPort.ReadLine().Contains("OK"))
                            return 0;
                    }
                }
                logger.Info($"InitCAN {serialPort.PortName} {canIndex} success");
                return 1;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"InitCAN {serialPort.PortName} {canIndex} error");
                return 0;
            }
        }

        public int StartCAN(int canIndex)
        {
            logger.Info($"StartCAN {serialPort.PortName} {canIndex} start");
            if (canIndex == 0)
            {
                lock (serialPort)
                {
                    if (!serialPort.IsOpen)
                        serialPort.Open();
                    if (!isStart)
                    {
                        serialPort.DataReceived += SerialPort_DataReceived;
                        isStart = true;
                    }
                }
            }
            logger.Info($"StartCAN {serialPort.PortName} {canIndex} success");
            return 1;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (serialPort.BytesToRead > 0)
            {
                var buf = new byte[serialPort.BytesToRead];
                lock (serialPort)
                {
                    var size = serialPort.Read(buf, 0, buf.Length);
                    received.AddRange(buf.Take(size));
                }
                var a = received.IndexOf((byte)'A');
                if (a >= 0 && received.Count > (a + 8) && received[a + 1] == (byte)'T')
                {
                    var len = received[a + 6];
                    if (received.Count > a + 6 + len + 2 && received[a + 6 + len + 1] == 0x0D && received[a + 6 + len + 2] == 0x0A)
                    {
                        var bytes = received.Skip(a).Take(6 + len + 3).ToArray();
                        logger.Info($"Received {serialPort.PortName} Bytes:{BitConverter.ToString(bytes)}");
                        messages.Add(Convert(bytes));
                        received.RemoveRange(0, a + 6 + len + 3);
                    }
                }
            }
        }

        public int GetReceiveCAN(int canIndex)
        {
            logger.Info($"GetReceiveCAN {serialPort.PortName} {canIndex}");
            if (canIndex == 0)
                return messages.Count;
            return 0;
        }
        public int ClearBufferCAN(int canIndex)
        {
            logger.Info($"ClearBufferCAN {serialPort.PortName} {canIndex} start");
            try
            {
                if (canIndex == 0)
                {
                    lock (serialPort)
                    {
                        if (!serialPort.IsOpen)
                            serialPort.Open();
                        serialPort.DiscardOutBuffer();
                        serialPort.DiscardInBuffer();
                        int count = messages.Count;
                        while (count-- > 0)
                            messages.TryTake(out _);
                    }
                }
                logger.Info($"ClearBufferCAN {serialPort.PortName} {canIndex} success");
                return 1;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"ClearBufferCAN {serialPort.PortName} {canIndex} error");
                return 0;
            }
        }
        public int ResetCAN(int canIndex)
        {
            logger.Info($"ResetCAN {serialPort.PortName} {canIndex} start");
            try
            {
                if (canIndex == 0)
                {
                    lock (serialPort)
                    {
                        if (!serialPort.IsOpen)
                            serialPort.Open();
                        serialPort.DiscardOutBuffer();
                        serialPort.DiscardInBuffer();
                        int count = messages.Count;
                        while (count-- > 0)
                            messages.TryTake(out _);
                    }
                }
                logger.Info($"ResetCAN {serialPort.PortName} {canIndex} success");
                return 1;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"ResetCAN {serialPort.PortName} {canIndex} error");
                return 0;
            }
        }

        public int TransmitCAN(int canIndex, SerialFrame[] frames, int Len)
        {
            logger.Info($"TransmitCAN {serialPort.PortName} {canIndex} Len:{Len}");
            int i = 0;
            if (canIndex == 0)
            {
                try
                {
                    for (; i < Len; i++)
                    {
                        var buf = Convert(frames[i]);
                        logger.Info($"Transmit {serialPort.PortName} {canIndex} Bytes:{BitConverter.ToString(buf)}");
                        lock (serialPort)
                            serialPort.Write(buf, 0, buf.Length);
                    }
                }
                catch (Exception ex) { logger.Error(ex, $"TransmitCAN {serialPort.PortName} {canIndex} error"); }
            }
            logger.Info($"TransmitCAN {serialPort.PortName} {canIndex} Ret:{i}");
            return i;
        }

        public int ReceiveCAN(int canIndex, SerialFrame[] frames, int Len, int WaitTime = -1)
        {
            logger.Info($"ReceiveCAN {serialPort.PortName} {canIndex} Len:{Len} WaitTime:{WaitTime}");
            int i = 0;
            if (canIndex == 0)
            {
                for (; i < Len;)
                {
                    if (messages.TryTake(out frames[i], WaitTime))
                        i++;
                    else
                        break;
                }
            }
            logger.Info($"ReceiveCAN {serialPort.PortName} {canIndex} Ret:{i}");
            return i;
        }

        public int GetReference(int canIndex, int RefType, IntPtr pData)
        {
            logger.Info($"GetReference {serialPort.PortName} {canIndex} RefType:{RefType}");
            return 1;
        }

        public int SetReference(int canIndex, int RefType, IntPtr pData)
        {
            logger.Info($"SetReference {serialPort.PortName} {canIndex} RefType:{RefType}");
            return 1;
        }

        private static byte[] Convert(SerialFrame obj)
        {
            int ext = obj.Ext == 0 ? 0 : 1;
            int rtr = obj.Rtr == 0 ? 0 : 1;
            int value = ((int)obj.Id << (ext == 0 ? 21 : 3)) | ext << 2 | rtr << 1;
            var outputs = new List<byte>(Encoding.ASCII.GetBytes("AT"));
            outputs.AddRange(BitConverter.GetBytes(value).Reverse());
            outputs.Add(rtr == 0 ? obj.Dlc : (byte)0);
            if (rtr == 0 && obj.Dlc > 0)
                outputs.AddRange(new Span<byte>(obj.Data, 0, obj.Dlc).ToArray());
            outputs.AddRange(Encoding.ASCII.GetBytes("\r\n"));
            return outputs.ToArray();
        }

        private static SerialFrame Convert(byte[] data)
        {
            /* data
             * 00000000 00100000 00000000 00000000
             * 00000000 00100000 00000000 00000010
             * 00000000 00000000 00000000 00001110
             * 00000000 00000000 00000000 00001100
             * |<---------|std id[0]
             * |<-----------------------------|ext id[0]
             *                                 |0:std 1:ext
             *                                  |0:data 1:rtr
             */
            var ident = BitConverter.ToUInt32(data.Skip(2).Take(4).Reverse().ToArray());
            var len = data[6];
            var ext = (byte)((ident >> 2) & 1);
            var frame = new SerialFrame
            {
                Id = ident >> (ext == 0 ? 21 : 3),
                Ext = ext,
                Rtr = (byte)((ident >> 1) & 1),
                Dlc = len,
                Data = data.Skip(7).Take(len).ToArray()
            };
            Array.Resize(ref frame.Data, 8);
            return frame;
        }

    }
}