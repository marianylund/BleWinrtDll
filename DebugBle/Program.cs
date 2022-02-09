using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DebugBle
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("You can use this program to test the BleWinrtDll.dll. Make sure your Computer has Bluetooth enabled.");
            BLE ble = new BLE();
            string deviceId = null;

            BLE.BLEScan scan = BLE.ScanDevices();
            scan.Found = (_deviceId, deviceName) =>
            {
                Console.WriteLine("found device with name: " + deviceName);
                if (deviceId == null && deviceName == "Arduino")
                    deviceId = _deviceId;
            };
            scan.Finished = () =>
            {
                Console.WriteLine("scan finished");
                if (deviceId == null)
                    deviceId = "-1";
            };
            while (deviceId == null)
                Thread.Sleep(500);

            scan.Cancel();
            if (deviceId == "-1")
            {
                Console.WriteLine("no device found!");
                return;
            }

            ble.Connect(deviceId,
                "{19b10000-e8f2-537e-4f6c-d104768a1214}", 
                new string[] { "{19b10001-e8f2-537e-4f6c-d104768a1214}" });

            for(int guard = 0; guard < 2000; guard++)
            {
                //Console.WriteLine("Trying to read the package ...");
                //BLE.ReadPackage();
                Console.WriteLine("Trying to write to char ...");
                bool ok = BLE.WritePackage(deviceId,
                    "{19b10000-e8f2-537e-4f6c-d104768a1214}",
                    "{19b10001-e8f2-537e-4f6c-d104768a1214}",
                    new byte[] { 1 });
                Console.WriteLine(ok);
                Console.WriteLine(BLE.GetError());
                Thread.Sleep(500);
            }

            Console.WriteLine("Press enter to exit the program...");
            Console.ReadLine();
        }
    }
}
