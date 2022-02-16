#include <Arduino.h>
#include <ArduinoBLE.h>
#include <Arduino_LSM9DS1.h>  // Extended 2.0 LSM9DS1 library written by Femme Verbeek, https://github.com/FemmeVerbeek/Arduino_LSM9DS1
#include <MadgwickAHRS.h>     // (!!!) Added getQuaternion() to https://github.com/arduino-libraries/MadgwickAHRS as described in one of the pull requests

// initialize a Madgwick filter:
Madgwick filter;

const char* deviceServiceUuid = "19b10000-e8f2-537e-4f6c-d104768a1214";
// const char* rollCharacteristicUuid = "19b10001-e8f2-537e-4f6c-d104768a1214";
const char* quatCharacteristicUuid = "19b10004-e8f2-537e-4f6c-d104768a1214";
const int quatBufferSizeBytes = 40;

BLEService imuService(deviceServiceUuid); 
// BLEFloatCharacteristic rollCharacteristic(rollCharacteristicUuid, BLERead | BLENotify);
BLECharacteristic quatCharacteristic(quatCharacteristicUuid, BLERead | BLENotify, quatBufferSizeBytes);


long previousMillis = 0;  // last timechecked, in ms
unsigned long micros_per_reading, micros_previous;

float _q0, _q1, _q2, _q3;

void setup()
{
  Serial.begin(115200);

  pinMode(LED_BUILTIN, OUTPUT); // initialize the built-in LED pin to indicate when a central is connected

  // attempt to start the IMU:
  if (!IMU.begin())
  {
    Serial.println("Failed to initialize IMU");
    while (1);
  }

  if (!BLE.begin()) {
    Serial.println("starting BLE failed!");
    while (1);
  }

  // Setup bluetooth
  BLE.setLocalName("Arduino");
  BLE.setAdvertisedService(imuService);
  // imuService.addCharacteristic(rollCharacteristic);
  imuService.addCharacteristic(quatCharacteristic);
  BLE.addService(imuService);

  Serial.print("Quaternion characteristic established with buffer size: "); Serial.println(quatBufferSizeBytes);
  
  // Setup IMU
  IMU.setMagnetFS(0); // ±400 µT
  IMU.setMagnetODR(8); // 400 Hz
  IMU.setMagnetOffset(2.434692, 18.985596, -11.926880);
  IMU.setMagnetSlope (1.366798, 1.182490, 1.482770);

  // Accelerometer code
  IMU.setAccelFS(2); // ±4 g
  IMU.setAccelODR(3); // 119 Hz
  IMU.setAccelOffset(0.003847, -0.009861, -0.013316);
  IMU.setAccelSlope (0.996795, 0.992095, 1.002343);

  IMU.gyroUnit= DEGREEPERSECOND;   
  IMU.setGyroFS(2); // ±1000 °/s
  IMU.setGyroODR(3); // 119 Hz
  IMU.setGyroOffset (-0.057953, 0.468018, -0.475281);
  IMU.setGyroSlope (1.154156, 1.157964, 1.123394);

  // start advertising
  BLE.advertise();
  Serial.println("Bluetooth device active, waiting for connections...");

  // start the filter to run at the sample rate:
  float sensorRate = min(IMU.getGyroODR(),IMU.getMagnetODR()); // The slowest ODR determines the sensor rate, Accel and Gyro share their ODR
  Serial.print("Sensor rate: "); Serial.println(sensorRate);
  filter.begin(sensorRate);

  delay(10000);

  Serial.println("Gyro settting ");  
  Serial.print("Gyroscope FS= ");   Serial.print(IMU.getGyroFS());
  Serial.print("Gyroscope ODR=");   Serial.println(IMU.getGyroODR());
  Serial.print("Gyro unit=");       Serial.println(IMU.gyroUnit);

  micros_per_reading = 1000000 / sensorRate;
  micros_previous = micros();
}

void sendSensorData(){
  // values for acceleration & rotation:
  float xAcc, yAcc, zAcc;
  float xGyro, yGyro, zGyro;
  float xMag, yMag, zMag;
  static int count=0;  

  // read all 9 DOF of the IMU:
  IMU.readAcceleration(xAcc, yAcc, zAcc);
  IMU.readGyro(xGyro, yGyro, zGyro);
  IMU.readMagneticField(xMag, yMag, zMag);

  // update the filter, which computes orientation:
  // note X and Y are swapped, X is inverted
  filter.update( yGyro,xGyro, zGyro, yAcc, xAcc, zAcc, yMag, -xMag, zMag);
  // (!!!) Had to add getQuaternion() in the MadgwickAHRS library manually 
  filter.getQuaternion(&_q0, &_q1, &_q2, &_q3);

  // Convert floats to strings
  char q0[10], q1[10], q2[10], q3[10];
  dtostrf(_q0, 5, 2, q0);
  dtostrf(_q1, 5, 2, q1);
  dtostrf(_q2, 5, 2, q2);
  dtostrf(_q3, 5, 2, q3);
  // Merge string values into one with ',' as a separator 
  // important to have it at the end too - to ignore the extra data after the floats
  char buffer[quatBufferSizeBytes];
  sprintf(buffer, "%s,%s,%s,%s;", q0, q1, q2, q3);
 
  quatCharacteristic.writeValue(buffer);

  count++;
  if (count > 20) // The optimum is probably something close to the refresh rate of your monitor.
  {  
    count = 0;   
    // values for orientation:
    int roll, pitch, heading;
    roll = filter.getRoll();
    pitch = filter.getPitch();
    heading = filter.getYaw();

    Serial.print(roll);
    Serial.print('\t');
    Serial.print(pitch);
    Serial.print('\t');
    Serial.println(heading);

    Serial.print(_q0);
    Serial.print('\t');
    Serial.print(_q1);
    Serial.print('\t');
    Serial.print(_q2);
    Serial.print('\t');
    Serial.println(_q3);
  }
}


void loop()
{ 
  // wait for a BLE central
  BLEDevice central = BLE.central();

  // if a BLE central is connected to the peripheral:
  if (central) {
    Serial.print("Connected to central: ");
    // print the central's BT address:
    Serial.println(central.address());
    // turn on the LED to indicate the connection:
    digitalWrite(LED_BUILTIN, HIGH);

    // while the central is connected:
    while (central.connected()) {
      unsigned long micros_now;
      micros_now = micros();

      if (micros_now - micros_previous >= micros_per_reading) {
        if (IMU.accelerationAvailable() && IMU.gyroscopeAvailable() && IMU.magneticFieldAvailable()) { // XX
          sendSensorData();
          micros_previous = micros_previous + micros_per_reading;
        }
      }
    }
    // when the central disconnects, turn off the LED:
    digitalWrite(LED_BUILTIN, LOW);
    Serial.print("Disconnected from central: ");
    Serial.println(central.address());
  }
  
}

// from: https://github.com/arduino/Arduino/blob/a2e7413d229812ff123cb8864747558b270498f1/hardware/arduino/sam/cores/arduino/avr/dtostrf.c
  char *dtostrf (double val, signed char width, unsigned char prec, char *sout) {
    char fmt[20];
    sprintf(fmt, "%%%d.%df", width, prec);
    sprintf(sout, fmt, val);
    return sout;
}

