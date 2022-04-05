using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Threading;
using TMPro;
using UnityEngine.Serialization;
using Random = System.Random;

public class BLEBehaviour : MonoBehaviour
{
    public delegate void BLEEvent(Quaternion rotation);
    public BLEEvent OnDataRead;

    public TMP_Text TextIsScanning, TextTargetDeviceConnection, TextTargetDeviceData, TextDiscoveredDevices;
    
    // Change this to match your device.
    string targetDeviceName = "Arduino";
    string serviceUuid = "{19b10000-e8f2-537e-4f6c-d104768a1214}";
    string[] characteristicUuids = {
        "{19b10001-e8f2-537e-4f6c-d104768a1214}",      // writeData
        "{19b10002-e8f2-537e-4f6c-d104768a1214}",      // bool if previous has been changed
        // "{19b10003-e8f2-537e-4f6c-d104768a1214}",      // CUUID 1
        "{19b10004-e8f2-537e-4f6c-d104768a1214}",      // readData
    };

    BLE ble;
    BLE.BLEScan scan;
    public bool isScanning = false, isConnected = false;
    string deviceId = null;  
    IDictionary<string, string> discoveredDevices = new Dictionary<string, string>();
    int devicesCount = 0;
    byte[] valuesToWrite;
    private Quaternion newRotation;
    private Vector3 newEulerRotation;
    private string result;

    [SerializeField]
    private float readingFrameRate = 0.3f; // Sample rate should be around 30 Hz
    private float _readingTimer = 0f;
    private int _frames = 0;

    // BLE Threads 
    Thread scanningThread, connectionThread, readingThread, writingThread;

    void Start()
    {
        ble = new BLE();
        
        TextTargetDeviceConnection.text = targetDeviceName + " not found.";
        readingThread = new Thread(ReadBleData);
    }


    void Update()
    {
        if (isScanning)
        {
            if (discoveredDevices.Count > devicesCount)
            {
                UpdateGuiText("scan");

                devicesCount = discoveredDevices.Count;
            }                
        } else
        {
            if (TextIsScanning.text != "Not scanning.")
            {
                TextIsScanning.color = Color.white;
                TextIsScanning.text = "Not scanning.";
            }
        }

        // The target device was found.
        if (deviceId != null && deviceId != "-1")
        {
            // Target device is connected and GUI knows.
            if (ble.isConnected && isConnected && _readingTimer > readingFrameRate)
            {
                //Debug.Log($"timer: {_readingTimer}, frameRate: {readingFrameRate}, frames: {_frames}");
                _readingTimer = 0f;
                _frames = 0;
                UpdateGuiText("readData");
            }
            // Target device is connected, but GUI hasn't updated yet.
            else if (ble.isConnected && !isConnected)
            {
                UpdateGuiText("connected");
                isConnected = true;
                // Device was found, but not connected yet. 
            } else if (!isConnected)
            {
                TextTargetDeviceConnection.text = "Found target device:\n" + targetDeviceName;
            } 
        }

        _readingTimer += Time.deltaTime;
        _frames += 1;
    }
    
    public void StartScanHandler()
    {
        devicesCount = 0;
        isScanning = true;
        discoveredDevices.Clear();
        scanningThread = new Thread(ScanBleDevices);
        scanningThread.Start();
        TextIsScanning.color = new Color(244, 180, 26);
        TextIsScanning.text = "Scanning...";
        TextIsScanning.text +=
            $"Searching for {targetDeviceName} with \nservice {serviceUuid} and \ncharacteristic {characteristicUuids[0]}";
        TextDiscoveredDevices.text = "";
    }
    
    void ScanBleDevices()
    {
        scan = BLE.ScanDevices();
        Debug.Log("BLE.ScanDevices() started.");
        scan.Found = (_deviceId, deviceName) =>
        {
            if (!discoveredDevices.ContainsKey(_deviceId))
            {
                Debug.Log("found device with name: " + deviceName);
                discoveredDevices.Add(_deviceId, deviceName);
            }

            if (deviceId == null && deviceName == targetDeviceName)
            {
                deviceId = _deviceId;
            }
        };

        scan.Finished = () =>
        {
            isScanning = false;
            Debug.Log("scan finished");
            if (deviceId == null)
                deviceId = "-1";
        };
        while (deviceId == null) 
            Thread.Sleep(500);
        scan.Cancel();
        scanningThread = null;
        isScanning = false;
        
        if (deviceId == "-1")
        {
            Debug.Log($"Scan is finished. {targetDeviceName} was not found.");
            return;
        }
        Debug.Log($"Found {targetDeviceName} device with id {deviceId}.");
        StartConHandler();
    }
    
    public void StartConHandler()
    {
        connectionThread = new Thread(ConnectBleDevice);
        connectionThread.Start();
    }

    void ConnectBleDevice()
    {
        if (deviceId != null)
        {
            try
            {
                Debug.Log($"Attempting to connect to {targetDeviceName} device with id {deviceId} ...");
                ble.Connect(deviceId,
                    serviceUuid,
                    characteristicUuids);
            } catch(Exception e)
            {
                Debug.Log("Could not establish connection to device with ID " + deviceId + "\n" + e);
            }
        }
        if (ble.isConnected)
            Debug.Log("Connected to: " + targetDeviceName);
    }
    
    void UpdateGuiText(string action)
    {
        switch(action) {
            case "scan":
                TextDiscoveredDevices.text = "";
                foreach (KeyValuePair<string, string> entry in discoveredDevices)
                {
                    TextDiscoveredDevices.text += "DeviceID: " + entry.Key + "\nDeviceName: " + entry.Value + "\n\n";
                    Debug.Log("Added device: " + entry.Key);
                }
                break;
            case "connected":
                TextTargetDeviceConnection.text = "Connected to target device:\n" + targetDeviceName;
                break;
            case "readData":
                if (!readingThread.IsAlive)
                {
                    readingThread = new Thread(ReadBleData);
                    readingThread.Start();
                    
                    //TextTargetDeviceData.text = "Quaternion: " + result;
                    TextTargetDeviceData.text = "Euler: " + result;
                    //selectedObject.rotation = newRotation;
                    OnDataRead?.Invoke(newRotation);
                }
                break;
        }
    }
    
    private void OnDestroy()
    {
        CleanUp();
    }

    private void OnApplicationQuit()
    {
        CleanUp();
    }

    // Prevent threading issues and free BLE stack.
    // Can cause Unity to freeze and lead
    // to errors when omitted.
    private void CleanUp()
    {
        try
        {
            scan.Cancel();
            ble.Close();
            scanningThread.Abort();
            connectionThread.Abort();
            readingThread.Abort();
            writingThread.Abort();
        } catch(NullReferenceException e)
        {
            Debug.Log("Thread or object never initialized.\n" + e);
        }        
    }

    public void StartWritingHandler(Quaternion newCalibratedRotation)
    {
        if (deviceId == "-1" || !isConnected || (writingThread?.IsAlive ?? false))
        {
            Debug.Log("Cannot write yet");
            return;
        }

        string strValues = $"{newCalibratedRotation.x},{newCalibratedRotation.y},{newCalibratedRotation.z},{newCalibratedRotation.w};";
        TextTargetDeviceData.text = "Writing some new: " + strValues;
        valuesToWrite = Encoding.ASCII.GetBytes(strValues); 
        
        writingThread = new Thread(WriteBleData);
        writingThread.Start();
    }
    
    private void WriteBleData()
    {
        bool ok = BLE.WritePackage(deviceId,
            serviceUuid,
            characteristicUuids[0],
            valuesToWrite);

        Debug.Log($"Writing status: {ok}. {BLE.GetError()}");
        // Notify the central that the value is updated
        byte[] bytes = new byte[] {1};
        ok = BLE.WritePackage(deviceId,
            serviceUuid,
            characteristicUuids[1],
            bytes);
        Debug.Log($"Writing status: {ok}. {BLE.GetError()}");
        writingThread = null;
    }

    private void ReadBleData(object obj)
    {
        byte[] packageReceived = BLE.ReadBytes(out string charId);
        if (charId == characteristicUuids[0])
        {
            Debug.Log("Reading data from writeCharacteristic: " + Encoding.UTF8.GetString(packageReceived));
            return;
        }

        if (charId == characteristicUuids[2])
        {
            result = Encoding.UTF8.GetString(packageReceived).Split(';')[0]; // ; signals the end of the message data
            // Quaternion arrives of the form: f,f,f,f; where f is a float
            //Debug.Log("result: " + result);
            string[] splitResult = result.Split(',');
            float x = float.Parse(splitResult[0]);
            float y = float.Parse(splitResult[1]);
            float z = float.Parse(splitResult[2]);
            float w = float.Parse(splitResult[3]);
            
            newRotation = new Quaternion(float.Parse(splitResult[0]), float.Parse(splitResult[1]), float.Parse(splitResult[2]), float.Parse(splitResult[3]));
            // Following: https://gamedev.stackexchange.com/questions/157946/converting-a-quaternion-in-a-right-to-left-handed-coordinate-system
            //newRotation = new Quaternion(y, z, x, w);
            //Vector3 ahrs = new Vector3(float.Parse(splitResult[0]), float.Parse(splitResult[1]), float.Parse(splitResult[2]));
            //newEulerRotation = new Vector3(-ahrs.y, ahrs.z, -ahrs.x);
        }
    }

}
