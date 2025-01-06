using VCI;

namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, ControlCAN!");
            ControlCAN.VCI_OpenDevice(0, 3, 0); // Open COM3
            ControlCAN.VCI_InitCAN(0, 3, 0, new VCI_INIT_CONFIG { Timing0 = 0, Timing1 = 0x14 }); // CAN0  1Mbps
            ControlCAN.VCI_StartCAN(0, 3, 0); // Start CAN0

            var tx = ControlCAN.VCI_Transmit(0, 3, 0, [new VCI_CAN_OBJ { ID = 1, DataLen = 4, Data = [1, 2, 3, 4, 5, 6, 7, 8] }], 1);

            if (ControlCAN.VCI_GetReceiveNum(0, 3, 0) > 0)
            {
                var f = new VCI_CAN_OBJ[1];
                var s = ControlCAN.VCI_Receive(0, 3, 0, f, 1);
            }
            ControlCAN.VCI_CloseDevice(0, 3); // Close COM3
            //var serial = new SerialCAN();
            //serial.Open(3);
            //serial.StartCAN(0);
            //serial.InitCAN(0, default);
            //serial.Close();
        }
    }
}
