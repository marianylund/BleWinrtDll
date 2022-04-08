using System;
using UnityEngine;
using System.IO.Ports;

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

    float _duration = 0.01f;
    private float _raySize = 0.3f;

    private float _pitchAcc;
    private float _rollAcc;
    private float _pitchGyro;
    private float _rollGyro;
    private float _yawGyro;
    private float _pitchComp;
    private float _rollComp;
    private Vector3 _integratedGyroData; 

    
    private Vector3 _accVector;

    
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

    void Start()
    {
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
    

    float[] EulerToQuat(float x, float y, float z)
    {
        // Expects RADIANS
        float yaw = x;
        float pitch = y;
        float roll = z;

        double yawOver2 = yaw * 0.5f;
        float cosYawOver2 = (float)Math.Cos(yawOver2);
        float sinYawOver2 = (float)Math.Sin(yawOver2);
        double pitchOver2 = pitch * 0.5f;
        float cosPitchOver2 = (float)Math.Cos(pitchOver2);
        float sinPitchOver2 = (float)Math.Sin(pitchOver2);
        double rollOver2 = roll * 0.5f;
        float cosRollOver2 = (float)Math.Cos(rollOver2);
        float sinRollOver2 = (float)Math.Sin(rollOver2);    
        
        float[] quat = new float[4];
        quat[0] = sinYawOver2 * cosPitchOver2 * cosRollOver2 + cosYawOver2 * sinPitchOver2 * sinRollOver2; // x
        quat[1] = cosYawOver2 * sinPitchOver2 * cosRollOver2 - sinYawOver2 * cosPitchOver2 * sinRollOver2; // y
        quat[2] = cosYawOver2 * cosPitchOver2 * sinRollOver2 - sinYawOver2 * sinPitchOver2 * cosRollOver2; // z
        quat[3] = cosYawOver2 * cosPitchOver2 * cosRollOver2 + sinYawOver2 * sinPitchOver2 * sinRollOver2; // w

        return quat;
    }

    private void OnDisable()
    {
        _pitchGyro =0;
        _yawGyro =0;
        _rollGyro =0;
        gyroController.rotation = Quaternion.identity;
        fusedController.rotation = Quaternion.identity;
        rightUpTransform.rotation = Quaternion.identity;
    }

    void Update()
    {
        
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

                    _pitchAcc = Mathf.Atan2(_accVector.x, _accVector.y) * 180/Mathf.PI;
                    _rollAcc = Mathf.Atan2(_accVector.z, _accVector.y) * 180/Mathf.PI;
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
                        _integratedGyroData = new Vector3(
                            _gy * _gdelta * changeCoordinateSystemGyro.x,
                            _gz * _gdelta * changeCoordinateSystemGyro.y,
                            _gx * _gdelta * changeCoordinateSystemGyro.z);
                        this.gyroController.rotation *= Quaternion.Euler(_integratedGyroData);
                    }
                    this._lastGyroReadTime = _gtime;
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

        _pitchGyro += _integratedGyroData.x;
        _yawGyro += _integratedGyroData.y;
        _rollGyro += _integratedGyroData.z;
        
        _pitchComp = _pitchGyro * (1f - driftCompensationRatio) + _pitchAcc * driftCompensationRatio;
        _rollComp = _rollGyro * (1f - driftCompensationRatio) + _rollAcc * driftCompensationRatio;

        fusedController.rotation = Quaternion.Euler(_pitchComp, _yawGyro, _rollComp);
        
        // Drawing rays
        var gyroPos = gyroController.position;
        Debug.DrawRay(gyroPos, gyroController.forward * _raySize, Color.blue, _duration);
        Debug.DrawRay(gyroPos, gyroController.right * _raySize, Color.red, _duration);
        Debug.DrawRay(gyroPos, gyroController.up * _raySize, Color.green, _duration);

        var fusedPos = fusedController.position;
        Debug.DrawRay(fusedPos, fusedController.forward * _raySize, Color.blue, _duration);
        Debug.DrawRay(fusedPos, fusedController.right * _raySize, Color.red, _duration);
        Debug.DrawRay(fusedPos, fusedController.up * _raySize, Color.green, _duration);

        var rightUpPos = rightUpTransform.position;
        Debug.DrawRay(rightUpPos, rightUpTransform.forward * _raySize, Color.blue, _duration);
        Debug.DrawRay(rightUpPos, rightUpTransform.right * _raySize, Color.red, _duration);
        Debug.DrawRay(rightUpPos, rightUpTransform.up * _raySize, Color.green, _duration);
    }
    
    
    
}
    
