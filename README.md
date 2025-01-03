# ControlCAN.Uart

The Serial TTL CAN module of the AT command is encapsulated as a ControlCAN.dll so that it can be called by other software.
AOT compilation using C# is used to generate ControlCAN.dll usable in C.


## File
**SerialCAN.cs** => Serial TTL CAN Core.      
**ControlCAN.cs** => ControlCAN.dll (Native dll) for CANPro.exe or other ControlCAN based software.    
**EcanFDVci.cs** => EcanFDVci.dll (Native dll) for GCANTools.exe.       

All of their export functions are in ControlCAN.dll.    
if you need EcanFDVci.dll, rename by yourself.    
[Download Release](https://github.com/IOL0ol1/ControlCAN/releases)


## How to use:

Please refer to how to use ControlCAN.

### Timing list

|Timing0<<8\|Timing1|bps|    
|-------|----------|    
|0xBFFF | 5000|
|0x311C | 10000|
|0x181C | 20000|
|0x87FF | 40000|
|0x091C | 50000|
|0x83FF | 80000|
|0x041C | 100000|
|0x031C | 125000|
|0x81FA | 200000|
|0x011C | 250000|
|0x80FA | 400000|
|0x001C | 500000|
|0x80B6 | 666000|
|0x0016 | 800000|
|0x0014 | 1000000|

**NOTE**: In fact, the serial TTL CAN module supports 14 filters, and any baud rate within 1M.    
The Timing list is intended to be compatible with ControlCAN usage.    
For details, please refer to [SerialCAN.InitCAN](/ControlCAN/SerialCAN.cs)

## TODO

- ControlCAN VCI_INIT_CONFIG.Filter not support.
- ControlCAN VCI_CAN_OBJ.SendType not support.
