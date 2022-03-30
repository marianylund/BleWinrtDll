using System;
using UnityEngine;
using UnityEngine.Serialization;

public class SensorFusion : MonoBehaviour
{
    public Transform selectedObject;
    public Transform vuforiaSimulationObject;
    private BLEBehaviour _ble;
    
    private Vector3 _lastVuforiaLocalRotation = Vector3.zero;
    private Vector3 _lastVuforiaPosition = Vector3.zero;
    private Vector3 _lastBleRotation = Vector3.zero;
    [SerializeField]
    private Vector3 lastRotationDifference = Vector3.zero;
    
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
        _lastVuforiaLocalRotation = vuforiaSimulationObject.localEulerAngles;
        _lastVuforiaPosition = vuforiaSimulationObject.position;

        lastRotationDifference = _lastVuforiaLocalRotation - _lastBleRotation;
    }

    private void Update()
    {
        selectedObject.rotation = Quaternion.Euler(_lastBleRotation + lastRotationDifference);
    }

    private void GetData(Vector3 eulerRotation)
    {
        //selectedObject.rotation = Quaternion.Euler(eulerRotation);
        _lastBleRotation = eulerRotation;
    }

    private void OnDestroy()
    {
        _ble.OnDataRead -= GetData;
    }
}
