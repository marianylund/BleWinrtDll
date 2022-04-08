using UnityEngine;

public class SensorFusion : MonoBehaviour
{
    public Transform selectedObject;
    public Transform vuforiaSimulationObject;
    private BLEBehaviour _ble;
    
    private Quaternion _lastBleRotation = Quaternion.identity;
    
    void Start()
    {
        _ble = GetComponent<BLEBehaviour>();
        Debug.Assert(_ble != null, "Requires BLE components to get the data from");
        _ble.OnDataRead += GetData;
    }

    /// <summary>
    /// Simulating data from Vuforia as if the Vuforia is tracking
    /// want to apply data from BLE based in the last Vuforia Rotation
    /// </summary>
    public void CalibrateBLEVuforia()
    {
       // Quaternion diff = vuforiaSimulationObject.rotation * Quaternion.Inverse(_lastBleRotation);
        //Debug.Log("Rotation difference: " + diff + ", euler: " + diff.eulerAngles);
        _ble.StartWritingHandler(Quaternion.identity);
    }

    private void FixedUpdate()
    {
        selectedObject.rotation = _lastBleRotation;
    }

    private void GetData(Quaternion rotation)
    {
        //selectedObject.rotation = Quaternion.Euler(eulerRotation);
        _lastBleRotation = rotation;
        //_lastBleRotation = Quaternion.Inverse(rotation);
    }

    private void OnDestroy()
    {
        _ble.OnDataRead -= GetData;
    }
}
