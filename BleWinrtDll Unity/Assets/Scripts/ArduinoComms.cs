using System;
using UnityEngine;
using System.IO.Ports;
using AHRS;
using Microsoft.Win32;
using Packages.Rider.Editor.UnitTesting;
using UnityEngine.Profiling.Experimental;
using UnityEngine.Serialization;

// The code inspired by CreatiXR Controllers: https://github.com/CreatiXR/Controllers/blob/master/Unity/ControllerModel/Assets/ArduinoComms.cs
public class ArduinoComms : MonoBehaviour
{
    SerialPort _port;
    float _lastGyroReadTime = float.NaN;

    [SerializeField] private float magicValue = 0.35f;
    [SerializeField] private float samplePeriod = 1f / 119f;
    [SerializeField] private float beta = 0.1f;
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
        System.ComponentModel.IContainer components =
            new System.ComponentModel.Container();
        _port = new System.IO.Ports.SerialPort(components);
        _port.PortName = "COM3";
        _port.BaudRate = 9600;
        _port.DtrEnable = true;
        _port.ReadTimeout = 0;
        _port.WriteTimeout = 0;
        _port.Open();
        samplePeriod = 1f / 119f;
        _ahrs = new MadgwickAHRS(samplePeriod, beta);
        Debug.Log(_port.IsOpen);
    }

    void Update()
    {
        try
        {
            while (true)
            {
                _line = _port.ReadLine();
                Debug.Log(_line);
                
                if (_line.StartsWith("ACCEL:"))
                {
                    _atokens = _line.Split('\t');
                    _atime = float.Parse(_atokens[1].Substring(0, _atokens[1].Length - 2)) / 1000f;
                    _ax = float.Parse(_atokens[2]);
                    _ay = float.Parse(_atokens[3]);
                    _az = float.Parse(_atokens[4]);
                    this._accVector = new Vector3(_ay * changeCoordinateSystemAccel.x, _az * changeCoordinateSystemAccel.y, _ax* changeCoordinateSystemAccel.z);
                    this.upTransform.position = this._accVector.normalized * magicValue;
                }

                if (_line.StartsWith("GYRO:"))
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
                        this.gyroController.rotation *= Quaternion.Euler(
                            _gy * _gdelta * changeCoordinateSystemGyro.x,
                            _gz * _gdelta * changeCoordinateSystemGyro.y,
                            _gx * _gdelta * changeCoordinateSystemGyro.z);
                        
                        this.fusedController.rotation *= Quaternion.Euler(
                            _gy * _gdelta * changeCoordinateSystemGyro.x,
                            _gz * _gdelta * changeCoordinateSystemGyro.y,
                            _gx * _gdelta * changeCoordinateSystemGyro.z);
                    }
                    this._lastGyroReadTime = _gtime;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }

        //var accAngleForward = (Mathf.Atan(-1 * _up.x / Mathf.Sqrt((float)(Math.Pow(_up.z, 2.0) + Math.Pow(_up.y, 2.0)))) * 180 / Mathf.PI) * -1;
        //var accAngleForward = (Mathf.Atan2(-1 * _accVector.x, Mathf.Sqrt((float)(Math.Pow(_accVector.z, 2.0) + Math.Pow(_accVector.y, 2.0)))) * 180 / Mathf.PI) * -1;
        
        float vertical = _accVector.x;
        float lateral = Mathf.Sqrt(
            _accVector.y * _accVector.y +
            _accVector.z * _accVector.z);
        float accAngleForward = -(Mathf.Atan2 ( lateral, vertical) * 180.0f) / Mathf.PI;
        //accAngleForward *= Mathf.Sign(_accVector.y);
        
        float forward = _accVector.z;
        float midsagittal = Mathf.Sqrt(
            _accVector.x * _accVector.x +
            _accVector.y * _accVector.y);
        float accAngleRight = -(Mathf.Atan2 ( midsagittal, forward) * 180.0f) / Mathf.PI;

        var cosForward = Mathf.Cos(accAngleForward); // theta 
        var sinForward = Mathf.Sin(accAngleForward); // theta 
        var sinRight = Mathf.Sin(accAngleRight); // f
        var cosRight = Mathf.Cos(accAngleRight); // f

        _tilt = new Vector3(
            cosForward + sinForward * sinRight + sinForward * cosRight,
            cosRight - sinRight,
            -sinForward + cosForward * sinRight + cosForward * cosRight);

        var accAngleUp = 0f;//gyroController.transform.eulerAngles.y;//Mathf.Atan2(-tilt[1], tilt[0]);

        _accRotation = new Vector3(accAngleRight, accAngleUp, accAngleForward);
        //Debug.Log($"accAngleRight: {accAngleRight}\taccAngleForward: {accAngleForward}\taccAngleUp: {accAngleUp}=>\t{tilt}");

        // Quaternion accQuaternion = Quaternion.Euler(_accRotation);
        // _ahrs.SamplePeriod = samplePeriod;
        // _ahrs.Beta = beta;
        // _ahrs.Update(_gz *changeCoordinateSystemGyro.x, _gx*changeCoordinateSystemGyro.y, _gy*changeCoordinateSystemGyro.z, _accVector.x, _accVector.y, _accVector.z);
        // // _ahrs starts with 1, 0, 0, 0
        // this.rightUpTransform.rotation = new Quaternion(_ahrs.Quaternion[1], _ahrs.Quaternion[2], _ahrs.Quaternion[3], _ahrs.Quaternion[0]);// accQuaternion;

        // TODO: Use the accelerometer and magnetometer to compensate gyro drift.
        this._right = gyroController.right;

        this._forward = Vector3.Cross(this._accVector.normalized, gyroController.right);
        Debug.DrawRay(gyroController.position, _accVector* 1.2f, Color.yellow, duration);

        rightTransform.position = _right.normalized * magicValue;
        
        Debug.DrawRay(gyroController.position, rightUpTransform.right * raySize * 1.2f, Color.magenta, duration);
        Debug.DrawRay(gyroController.position, _forward * raySize * 1.2f, Color.cyan, duration);
        
        this.fusedController.rotation = Quaternion.Lerp(this.fusedController.rotation, accelMagneRotation, this.driftCompensationRatio);
        
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
    
