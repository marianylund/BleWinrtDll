# Unity project with BleWinrtDll configured for HoloLens 2 + Arduino BLE code

To try it out in Unity, open the project "BleWinrtDll Unity" in Unity. Then start the scene "Assets/Scenes/Demo.scene".
To test on HoloLens, build as you would from Unity project. Also Arduino code included.

My goal was to send data from Arduino to HoloLens 2 through BLE created in Unity Engine.

[comment]: <> (I describe this process in the medium blog.)
This repo is forked from https://github.com/adabru/BleWinrtDll, where the Dll is configured to work with UWP Unity.
It worked well with Arduino and Unity Editor, but did not translate well to HoloLens.
So I include here adjusted Unity Project ready to be built to HoloLens together with Arduino sketch used. For more information check out the original repo: https://github.com/adabru/BleWinrtDll


|    ![alt text][blemenu]    |                                ![alt text][blemenuconnected]                                |
|:--------------------------:|:-------------------------------------------------------------------------------------------:|
| Menu showing in HoloLens 2 | After clicking "Scan", it automatically connects to the described device and characteristic |

Clicking "Write" will send 0, 1, 2 or 3 at random to Arduino. It will change colours according to the number.

The `Demo.scene` in this repo uses threads, the script is taken from [BleWinrtDll-Unity-Demo](https://github.com/Joelx/BleWinrtDll-Unity-Demo). 

### Used
* Windows 10
* Arduino Nano 33 BLE
* Arduino IDE 2.0.0-rc3
* HoloLens 2
* Unity Engine 2020.3.20f1
* Visual Studio 2019 16.11.9 with packages installed (.NET desktop development, Desktop development with C++, Universal Windows Platform development)

## Build

### Arduino Nano BLE
Arduino Code is included in ArduinoCode -> bleledgesture.ino. Build it as you would the usual sketch. 
For the code to start running, you have to have the Serial Monitor open: ![alt text][arduinomonitor]

### HoloLens 2 Application
Build it as you would the usual application, you can follow the [guide from Microsoft](https://docs.microsoft.com/en-us/learn/modules/learn-mrtk-tutorials/1-7-exercise-hand-interaction-with-objectmanipulator).

### Updating Dll
You can follow instructions in the original repository. The difference now is that you need two Dlls, one for Unity Editor and one for HoloLens.
For HoloLens choose configuration "Release" and "ARM64" and "x64" for Unity Editor when building.
Rename the Dll for Unity Editor to "BleWinrtDllx64" and replace them in the Unity project Assets folder.
You might have to reconfigure the Dlls in Unity. To do so click on the Dll and choose configurations shown in images:

| ![alt text][editorconfig] | ![alt text][holodllconfig] |
|:-------------------------:|:--------------------------:|
|          Editor           |          Hololens          |

[holodllconfig]: ./img/hololensdllconfig.png "Configuration of HoloLens Dll"
[editorconfig]: ./img/unityeditordllconfig.png "Configuration of Unity Editor Dll"
[arduinomonitor]: ./img/arduinoserialmonitor.png "Example of how Arduino Serial Monitor would look"
[blemenu]: ./img/blemenu.jpg "Menu in HoloLens when starting the application"
[blemenuconnected]: ./img/blemenuconnected.jpg "Menu in HoloLens after pressing Scan and connecting to Arduino"