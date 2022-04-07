using System;
using UnityEngine;
using System.IO.Ports;
using AHRS;

// The code inspired by CreatiXR Controllers: https://github.com/CreatiXR/Controllers/blob/master/Unity/ControllerModel/Assets/ArduinoComms.cs
public class ArduinoComms : MonoBehaviour
{
    SerialPort _port;
    float _lastGyroReadTime = float.NaN;

    [SerializeField] private float magicValue = 0.35f;
    [SerializeField]
    Vector3 changeCoordinateSystemGyro = -Vector3.one;
    
    [SerializeField]
    Vector3 changeCoordinateSystemAccel = Vector3.one;
    
    [SerializeField]
    Vector3 offsetAcc = Vector3.zero;

    [SerializeField] private Transform vuforiaTransform;
    
    [SerializeField]
    Transform gyroController;
    
    [SerializeField]
    Transform upTransform;
    
    [SerializeField]
    Transform rightUpTransform;

    [SerializeField]
    Transform rightTransform;
    
    [SerializeField]
    Transform fusedController;

    [SerializeField]
    float driftCompensationRatio = 0.02f;

    float duration = 0.01f;
    private float raySize = 0.3f;

    private float pitchAcc;
    private float rollAcc;
    private float pitchGyro;
    private float rollGyro;
    private float yawGyro;
    private float pitchComp;
    private float rollComp;
    private Vector3 integratedGyroData; 

    
    private Vector3 _accRotation;
    private Vector3 _accVector;
    private Vector3 _tilt;
    private Vector3 _right; //east
    private Vector3 _forward; // north;
    
    private string _line;
    private string[] _gtokens;
    private float _gtime;
    private float _gx;
    private float _gy;
    private float _gz;
    private float _gdelta;
    
    private string[] _atokens;
    private float _atime;
    private float _ax;
    private float _ay;
    private float _az;
    private float _adelta;

    private MadgwickAHRS _ahrs;
    
    void Start()
    {
        Test();
        return;
        System.ComponentModel.IContainer components =
            new System.ComponentModel.Container();
        _port = new System.IO.Ports.SerialPort(components);
        _port.PortName = "COM3";
        _port.BaudRate = 9600;
        _port.DtrEnable = true;
        _port.ReadTimeout = 0;
        _port.WriteTimeout = 0;
        _port.Open();
        Debug.Assert(_port.IsOpen);
    }

    private void Test()
    {
        float tol = 0.001f;
        Vector3 vec = new Vector3(0, 0, 0);
        var quat = eulerToQuat(vec.x, vec.y, vec.z);
        var ans = Quaternion.Euler(vec);
        Debug.Assert(Math.Abs(quat[0] - ans.x) < tol && Math.Abs(quat[1] - ans.y) < tol && Math.Abs(quat[2] - ans.z) < tol && Math.Abs(quat[3] - ans.w) < tol, 
            $"vec: {vec}, quat:[{quat[0]}, {quat[1]}, {quat[2]}, {quat[3]}], ans: {ans}");
        
        vec = new Vector3(90, 0, 0);
        quat = eulerToQuat(vec.x, vec.y, vec.z);
        ans = Quaternion.Euler(vec);
        Debug.Assert(Math.Abs(quat[0] - ans.x) < tol && Math.Abs(quat[1] - ans.y) < tol && Math.Abs(quat[2] - ans.z) < tol && Math.Abs(quat[3] - ans.w) < tol, 
            $"vec: {vec}, quat:[{quat[0]}, {quat[1]}, {quat[2]}, {quat[3]}], ans: {ans}");
        
        vec = new Vector3(0, 180, 0);
        quat = eulerToQuat(vec.x, vec.y, vec.z);
        ans = Quaternion.Euler(vec);
        Debug.Assert(Math.Abs(quat[0] - ans.x) < tol && Math.Abs(quat[1] - ans.y) < tol && Math.Abs(quat[2] - ans.z) < tol && Math.Abs(quat[3] - ans.w) < tol, 
            $"vec: {vec}, quat:[{quat[0]}, {quat[1]}, {quat[2]}, {quat[3]}], ans: {ans}");
        
        vec = new Vector3(0, 0, -270);
        quat = eulerToQuat(vec.x, vec.y, vec.z);
        ans = Quaternion.Euler(vec);
        Debug.Assert(Math.Abs(quat[0] - ans.x) < tol && Math.Abs(quat[1] - ans.y) < tol && Math.Abs(quat[2] - ans.z) < tol && Math.Abs(quat[3] - ans.w) < tol, 
            $"vec: {vec}, quat:[{quat[0]}, {quat[1]}, {quat[2]}, {quat[3]}], ans: {ans}");
        
        vec = new Vector3(30, -90, -270);
        quat = eulerToQuat(vec.x, vec.y, vec.z);
        ans = Quaternion.Euler(vec);
        Debug.Assert(Math.Abs(quat[0] - ans.x) < tol && Math.Abs(quat[1] - ans.y) < tol && Math.Abs(quat[2] - ans.z) < tol && Math.Abs(quat[3] - ans.w) < tol, 
            $"vec: {vec}, quat:[{quat[0]}, {quat[1]}, {quat[2]}, {quat[3]}], ans: {ans}");
        
        vec = new Vector3(-90, 232, 180);
        quat = eulerToQuat(vec.x, vec.y, vec.z);
        ans = Quaternion.Euler(vec);
        Debug.Assert(Math.Abs(quat[0] - ans.x) < tol && Math.Abs(quat[1] - ans.y) < tol && Math.Abs(quat[2] - ans.z) < tol && Math.Abs(quat[3] - ans.w) < tol, 
            $"vec: {vec}, quat:[{quat[0]}, {quat[1]}, {quat[2]}, {quat[3]}], ans: {ans}");

    }

    float[] eulerToQuat(float x, float y, float z)
    {
        // Expects RADIANS
        float yaw = x;
        float pitch = y;
        float roll = z;

        double yawOver2 = yaw * 0.5f;
        float cosYawOver2 = (float)System.Math.Cos(yawOver2);
        float sinYawOver2 = (float)System.Math.Sin(yawOver2);
        double pitchOver2 = pitch * 0.5f;
        float cosPitchOver2 = (float)System.Math.Cos(pitchOver2);
        float sinPitchOver2 = (float)System.Math.Sin(pitchOver2);
        double rollOver2 = roll * 0.5f;
        float cosRollOver2 = (float)System.Math.Cos(rollOver2);
        float sinRollOver2 = (float)System.Math.Sin(rollOver2);    
        
        float[] quat = new float[4];
        quat[0] = sinYawOver2 * cosPitchOver2 * cosRollOver2 + cosYawOver2 * sinPitchOver2 * sinRollOver2; // x
        quat[1] = cosYawOver2 * sinPitchOver2 * cosRollOver2 - sinYawOver2 * cosPitchOver2 * sinRollOver2; // y
        quat[2] = cosYawOver2 * cosPitchOver2 * sinRollOver2 - sinYawOver2 * sinPitchOver2 * cosRollOver2; // z
        quat[3] = cosYawOver2 * cosPitchOver2 * cosRollOver2 + sinYawOver2 * sinPitchOver2 * sinRollOver2; // w

        return quat;
    }

    private void OnDisable()
    {
        pitchGyro =0;
        yawGyro =0;
        rollGyro =0;
        gyroController.rotation = Quaternion.identity;
        fusedController.rotation = Quaternion.identity;
        rightUpTransform.rotation = Quaternion.identity;
    }

    void Update()
    {
        return;
        try
        {
            while (true)
            {
                _line = _port.ReadLine();
                //Debug.Log(_line);
                
                if (_line.StartsWith("ACCEL:"))
                {
                    _atokens = _line.Split('\t');
                    _atime = float.Parse(_atokens[1].Substring(0, _atokens[1].Length - 2)) / 1000f;
                    _ax = float.Parse(_atokens[2]);
                    _ay = float.Parse(_atokens[3]);
                    _az = float.Parse(_atokens[4]);
                    this._accVector = new Vector3(_ay * changeCoordinateSystemAccel.x, _az * changeCoordinateSystemAccel.y, _ax* changeCoordinateSystemAccel.z);
                    this.upTransform.position = this._accVector.normalized * magicValue;

                    pitchAcc = Mathf.Atan2(_accVector.x, _accVector.y) * 180/Mathf.PI;
                    rollAcc = Mathf.Atan2(_accVector.z, _accVector.y) * 180/Mathf.PI;
                }

                if (_line.StartsWith("GYRO:")) // Expects deg per sec
                {
                    _gtokens = _line.Split('\t');
                    // microseconds controller app time
                    _gtime = float.Parse(_gtokens[1]);
                    // degrees per second
                    _gx = float.Parse(_gtokens[2]);
                    _gy = float.Parse(_gtokens[3]);
                    _gz = float.Parse(_gtokens[4]);
                    // seconds since last gyro reading
                    _gdelta = (_gtime - _lastGyroReadTime) / 1000000f;
                    if (!float.IsNaN(this._lastGyroReadTime))
                    {
                        integratedGyroData = new Vector3(
                            _gy * _gdelta * changeCoordinateSystemGyro.x,
                            _gz * _gdelta * changeCoordinateSystemGyro.y,
                            _gx * _gdelta * changeCoordinateSystemGyro.z);
                        this.gyroController.rotation *= Quaternion.Euler(integratedGyroData);
                    }
                    this._lastGyroReadTime = _gtime;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }

        pitchGyro += integratedGyroData.x;
        yawGyro += integratedGyroData.y;
        rollGyro += integratedGyroData.z;
        
        pitchComp = pitchGyro * (1f - driftCompensationRatio) + pitchAcc * driftCompensationRatio;
        rollComp = rollGyro * (1f - driftCompensationRatio) + rollAcc * driftCompensationRatio;

        fusedController.rotation = Quaternion.Euler(pitchComp, yawGyro, rollComp);
        
        // Drawing rays
        Debug.DrawRay(gyroController.position, gyroController.forward * raySize, Color.blue, duration);
        Debug.DrawRay(gyroController.position, gyroController.right * raySize, Color.red, duration);
        Debug.DrawRay(gyroController.position, gyroController.up * raySize, Color.green, duration);
        
        Debug.DrawRay(fusedController.position, fusedController.forward * raySize, Color.blue, duration);
        Debug.DrawRay(fusedController.position, fusedController.right * raySize, Color.red, duration);
        Debug.DrawRay(fusedController.position, fusedController.up * raySize, Color.green, duration);
        
        Debug.DrawRay(rightUpTransform.position, rightUpTransform.forward * raySize, Color.blue, duration);
        Debug.DrawRay(rightUpTransform.position, rightUpTransform.right * raySize, Color.red, duration);
        Debug.DrawRay(rightUpTransform.position, rightUpTransform.up * raySize, Color.green, duration);
    }
    
    
    
}
    
