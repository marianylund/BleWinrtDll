using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Threading;
using TMPro;

public class BLEBehaviour : MonoBehaviour
{
    public delegate void BLEEvent(Quaternion rotation);
    public BLEEvent OnDataRead;

    public TMP_Text textIsScanning;
    public TMP_Text textTargetDeviceConnection;
    public TMP_Text textTargetDeviceData;
    public TMP_Text textDiscoveredDevices;

    // Change this to match your device.
    private string _targetDeviceName = "Arduino";
    private string _serviceUuid = "{19b10000-e8f2-537e-4f6c-d104768a1214}";

    private readonly string[] _characteristicUuids = {
        "{19b10001-e8f2-537e-4f6c-d104768a1214}",      // writeData
        "{19b10002-e8f2-537e-4f6c-d104768a1214}",      // bool if previous has been changed
        // "{19b10003-e8f2-537e-4f6c-d104768a1214}",      // unused
        "{19b10004-e8f2-537e-4f6c-d104768a1214}",      // readData
    };

    private BLE _ble;
    private BLE.BLEScan _scan;
    public bool isScanning, isConnected, hasTriedScanning, hasDisconnected;
    private string _deviceId;
    private readonly IDictionary<string, string> _discoveredDevices = new Dictionary<string, string>();
    private int _devicesCount;
    private byte[] _valuesToWrite;
    private Quaternion _newRotation;
    private string _result;
    
    // If has not gotten answer before the time runs out, disconnect from Arduino and try again
    private float _readingTimeOut = 2f;
    private float _readingTimer;

    // BLE Threads 
    private Thread _scanningThread, _connectionThread, _readingThread, _writingThread;

    private void Start()
    {
        _ble = new BLE();
        
        textTargetDeviceConnection.text = _targetDeviceName + " not found.";
        _readingThread = new Thread(ReadBleData);
    }


    private void Update()
    {
        if (isScanning)
        {
            if (_discoveredDevices.Count > _devicesCount)
            {
                UpdateGuiText("scan");

                _devicesCount = _discoveredDevices.Count;
            }                
        } else
        {
            if (textIsScanning.text != "Not scanning.")
            {
                textIsScanning.color = Color.white;
                textIsScanning.text = "Not scanning.";
            }
        }

        // The target device was found.
        if (_deviceId != null && _deviceId != "-1")
        {
            if (hasDisconnected)
            {
                UpdateGuiText("disconnect");
                hasDisconnected = false;
            }
            // Target device is connected and GUI knows.
            if (_ble.isConnected && isConnected)
            {
                UpdateGuiText("readData");
            }
            // Target device is connected, but GUI hasn't updated yet.
            else if (_ble.isConnected && !isConnected)
            {
                UpdateGuiText("connected");
                isConnected = true;
                // Device was found, but not connected yet. 
            } else if (!isConnected)
            {
                textTargetDeviceConnection.text = "Found target device:\n" + _targetDeviceName;
            } 
        }
    }
    
    public void StartScanHandler()
    {
        if (!hasTriedScanning)
        {
            _devicesCount = 0;
            isScanning = true;
            hasTriedScanning = true;
            _discoveredDevices.Clear();
            _scanningThread = new Thread(ScanBleDevices);
            _scanningThread.Start();
            textIsScanning.color = new Color(244, 180, 26);
            textIsScanning.text = "Scanning...";
            textIsScanning.text +=
                $"Searching for {_targetDeviceName} with \nservice {_serviceUuid} and \ncharacteristic {_characteristicUuids[0]}";
            textDiscoveredDevices.text = "";
        }
        else if (!_ble.isConnected) // Do not reset if it is connected and well
        {
            CleanUp();
            _ble = new BLE();
            textTargetDeviceConnection.text = _targetDeviceName + " not found. Restarted ...";
            hasTriedScanning = false;
            StartScanHandler();
        }
    }

    private void ScanBleDevices()
    {
        _scan = BLE.ScanDevices();
        Debug.Log("BLE.ScanDevices() started.");
        _scan.Found = (deviceId, deviceName) =>
        {
            if (!_discoveredDevices.ContainsKey(deviceId))
            {
                Debug.Log("found device with name: " + deviceName);
                _discoveredDevices.Add(deviceId, deviceName);
            }

            if (this._deviceId == null && deviceName == _targetDeviceName)
            {
                this._deviceId = deviceId;
            }
        };

        _scan.Finished = () =>
        {
            isScanning = false;
            Debug.Log("scan finished");
            _deviceId ??= "-1";
        };
        while (_deviceId == null) 
            Thread.Sleep(500);
        _scan.Cancel();
        _scanningThread = null;
        isScanning = false;
        
        if (_deviceId == "-1")
        {
            Debug.Log($"Scan is finished. {_targetDeviceName} was not found.");
            Disconnect();
            return;
        }
        Debug.Log($"Found {_targetDeviceName} device with id {_deviceId}.");
        StartConHandler();
    }

    private void StartConHandler()
    {
        if ( !_connectionThread?.IsAlive ?? true)
        {
            _connectionThread = new Thread(ConnectBleDevice);
            _connectionThread.Start();
        }
        else
        {
            Debug.LogWarning("Connection thread is still running, but it is trying to connect again");
        }

    }

    private void ConnectBleDevice()
    {
        if (_deviceId != null)
        {
            try
            {
                Debug.Log($"Attempting to connect to {_targetDeviceName} device with id {_deviceId} ...");
                _ble.Connect(_deviceId,
                    _serviceUuid,
                    _characteristicUuids);
            } catch(Exception e)
            {
                Debug.LogWarning("Could not establish connection to device with ID " + _deviceId + "\n" + e);
                Disconnect();
            }
        }
        if (_ble.isConnected)
            Debug.Log("Connected to: " + _targetDeviceName);
    }

    public void Disconnect()
    {
        CleanUp();
        _ble = new BLE();
        hasTriedScanning = false;
        hasDisconnected = true;
    }

    private void UpdateGuiText(string action)
    {
        switch(action) {
            case "scan":
                textDiscoveredDevices.text = "";
                foreach (KeyValuePair<string, string> entry in _discoveredDevices)
                {
                    textDiscoveredDevices.text += "DeviceID: " + entry.Key + "\nDeviceName: " + entry.Value + "\n\n";
                    Debug.Log("Added device: " + entry.Key);
                }
                break;
            case "connected":
                textTargetDeviceConnection.text = "Connected to target device:\n" + _targetDeviceName;
                break;
            case "readData":
                if (isConnected && !_readingThread.IsAlive)
                {
                    _readingThread = new Thread(ReadBleData);
                    _readingThread.Start();
                    
                    textTargetDeviceData.text = "Quaternion: " + _result;
                    OnDataRead?.Invoke(_newRotation);
                }else if (_readingTimer > _readingTimeOut)
                {
                    _readingTimer = 0f;
                    CleanUp();
                    textTargetDeviceConnection.text = "Reading thread is timed out, disconnecting ...";
                    textDiscoveredDevices.text = "Discovered devices reset.";
                    Debug.Log(textTargetDeviceConnection.text);
                }
                else
                {
                    _readingTimer += Time.deltaTime;
                }
                break;
            case "disconnect":
                textTargetDeviceConnection.text = "Disconnected from " + _targetDeviceName;
                textTargetDeviceData.text = "";
                textIsScanning.text = "";
                textDiscoveredDevices.text = "";
                break;
            default:
                Debug.LogWarning("There is no case for changing UI for: " + action);
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
            isScanning = false;
            isConnected = false;
            hasTriedScanning = false;
            _discoveredDevices.Clear();
            _scan.Cancel();
            _ble.Close();
            _scanningThread.Abort();
            _connectionThread.Abort();
            _readingThread.Abort();
            _writingThread.Abort();

        } catch(NullReferenceException e)
        {
            Debug.Log("Thread or object never initialized.\n" + e);
        }        
    }

    public void StartWritingHandler(Quaternion newCalibratedRotation)
    {
        if (_deviceId == "-1" || !isConnected || (_writingThread?.IsAlive ?? false))
        {
            Debug.Log("Cannot write yet");
            return;
        }

        string strValues = $"{newCalibratedRotation.x},{newCalibratedRotation.y},{newCalibratedRotation.z},{newCalibratedRotation.w};";
        textTargetDeviceData.text = "Writing some new: " + strValues;
        _valuesToWrite = Encoding.ASCII.GetBytes(strValues); 
        
        _writingThread = new Thread(WriteBleData);
        _writingThread.Start();
    }
    
    private void WriteBleData()
    {
        bool ok = BLE.WritePackage(_deviceId,
            _serviceUuid,
            _characteristicUuids[0],
            _valuesToWrite);
        Debug.Log(ok + BLE.GetError());
        // Notify the central that the value is updated
        byte[] bytes = new byte[] {1};
        ok = BLE.WritePackage(_deviceId,
            _serviceUuid,
            _characteristicUuids[1],
            bytes);
        Debug.Log(ok + BLE.GetError());
        _writingThread = null;
    }

    private void ReadBleData(object obj)
    {
        // Go through all packages until it is the newest correct package
        byte[] prevPackage = BLE.ReadBytes(out string prevCharId);
        _readingTimer = 0f;
        byte[] packageReceived = BLE.ReadBytes(out string charId);
        _readingTimer = 0f;
        while (packageReceived.Length != 1 && packageReceived[0] == 0x0)
        {
            if (charId == _characteristicUuids[2])
            {
                prevPackage = packageReceived;
                prevCharId = charId;
            }
            packageReceived = BLE.ReadBytes(out charId);
            _readingTimer = 0f;
        }
        if (charId == _characteristicUuids[0])
        {
            Debug.Log("Reading data from writeCharacteristic: " + Encoding.UTF8.GetString(packageReceived));
            return;
        }
        

        if (prevCharId == _characteristicUuids[2])
        {
            // TODO: one day go through string split by comma and then stop when meeting ;
            _result = Encoding.UTF8.GetString(prevPackage).Split(';')[0]; // ; signals the end of the message data
            // Quaternion arrives of the form: f,f,f,f; where f is a float
            //Debug.Log("result: " + result);
            string[] splitResult = _result.Split(',');
            float x = float.Parse(splitResult[0]);
            float y = float.Parse(splitResult[1]);
            float z = float.Parse(splitResult[2]);
            float w = float.Parse(splitResult[3]);
            
            _newRotation = new Quaternion(x, y, z, w);
        }
    }

}
