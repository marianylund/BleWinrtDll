using UnityEngine;
using System.IO.Ports;

// The code inspired by CreatiXR Controllers: https://github.com/CreatiXR/Controllers/blob/master/Unity/ControllerModel/Assets/ArduinoComms.cs
public class ArduinoComms : MonoBehaviour
{
    SerialPort port;
    float lastGyroReadTime = float.NaN;

    [SerializeField] private float magicValue = 0.35f;
    
    [SerializeField]
    Vector3 changeCoordinateSystemGyro = -Vector3.one;
    
    [SerializeField]
    Vector3 changeCoordinateSystemAccel = Vector3.one;

    [SerializeField] private Transform vuforiaTransform;
    
    [SerializeField]
    Transform gytoController;
    
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

    private Vector3 up;
    private Vector3 right; //east
    private Vector3 forward; // north;
    
    private string line;
    private string[] gtokens;
    private float gtime;
    private float gx;
    private float gy;
    private float gz;
    private float gdelta;
    
    private string[] atokens;
    private float atime;
    private float ax;
    private float ay;
    private float az;
    private float adelta;
    
    void Start()
    {
        System.ComponentModel.IContainer components =
            new System.ComponentModel.Container();
        port = new System.IO.Ports.SerialPort(components);
        port.PortName = "COM3";
        port.BaudRate = 9600;
        port.DtrEnable = true;
        port.ReadTimeout = 0;
        port.WriteTimeout = 0;
        port.Open();
        Debug.Log(port.IsOpen);
    }
    
    void Update()
    {
        try
        {
            while (true)
            {
                line = port.ReadLine();
                Debug.Log(line);
                
                if (line.StartsWith("ACCEL:"))
                {
                    atokens = line.Split('\t');
                    atime = float.Parse(atokens[1].Substring(0, atokens[1].Length - 2)) / 1000f;
                    ax = float.Parse(atokens[2]);
                    ay = float.Parse(atokens[3]);
                    az = float.Parse(atokens[4]);
                    this.up = new Vector3(ay * changeCoordinateSystemAccel.x, az * changeCoordinateSystemAccel.y, ax* changeCoordinateSystemAccel.z);
                    this.upTransform.position = this.up.normalized * magicValue;
                }

                if (line.StartsWith("GYRO:"))
                {
                    gtokens = line.Split('\t');
                    // microseconds controller app time
                    gtime = float.Parse(gtokens[1]);
                    // degrees per second
                    gx = float.Parse(gtokens[2]);
                    gy = float.Parse(gtokens[3]);
                    gz = float.Parse(gtokens[4]);
                    // seconds since last gyro reading
                    gdelta = (gtime - lastGyroReadTime) / 1000000f;
                    if (!float.IsNaN(this.lastGyroReadTime))
                    {
                        this.gytoController.rotation *= Quaternion.Euler(
                            gy * gdelta * changeCoordinateSystemGyro.x,
                            gz * gdelta * changeCoordinateSystemGyro.y,
                            gx * gdelta * changeCoordinateSystemGyro.z);
                    }
                    this.lastGyroReadTime = gtime;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }
        
        // TODO: Use the acceleromater and magnetometer to compensate gyro drift.
        this.right = Vector3.Cross(this.up, gytoController.forward);
        rightTransform.position = right.normalized * magicValue;

        var accelMagneRotation = Quaternion.Inverse(Quaternion.LookRotation(this.right, this.up));
        this.rightUpTransform.rotation = accelMagneRotation;

        this.fusedController.rotation = Quaternion.Lerp(this.fusedController.rotation, vuforiaTransform.rotation * accelMagneRotation, this.driftCompensationRatio);

    }
}
    
