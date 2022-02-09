# Unity project with BleWinrtDll configured for HoloLens 2 + Arduino BLE code

To try it out in Unity, open the project "BleWinrtDll Unity" in Unity. Then start the scene "Assets/Scenes/Demo.scene".

This VisualStudio-project compiles to a C++-dll that can be imported into Unity. It wraps a part of the [UWP BLE API](https://docs.microsoft.com/de-de/windows/uwp/devices-sensors/bluetooth-low-energy-overview) inside a dll. The dll can be simply dropped into your Unity project and be used in the Unity Editor and the Windows standalone version.

[comment]: <> (![Screenshot of the demo scene.]&#40;screen.jpg&#41;)

The `Demo.scene` in this repo uses threads, the script is taken from [BleWinrtDll-Unity-Demo](https://github.com/Joelx/BleWinrtDll-Unity-Demo). 

### Used
* Windows 10
* Arduino Nano 33 BLE
* Arduino IDE 2.0.0-rc3
* HoloLens 2
* Unity Engine 2020.3.20f1
* Visual Studio 2019 16.11.9 with packages installed (.NET desktop development, Desktop development with C++, Universal Windows Platform development)

## Build

There is a prebuilt dll included in this repo, `BleWinrtDll Unity\Assets\BleWinrtDll.dll` or `DebugBle\BleWinrtDll.dll` (both are the same). But you can also build the dll yourself in VisualStudio. Follow these steps:

- Open the file BleWinrtDll.sln with VisualStudio (tested with Community 2019). You may be asked to install VisualStudio components when you open the project. The needed components are C++ Desktop and UWP, or if you want to save space, just the single components "MSVC C++ Buildtools", "Windows 10 SDK", ".NET Framework 4.7.2 SDK".
- Choose configuration "Release" and "ARM64" (I think it must match your machine architecture).
- In the project explorer, right-click the project "BleWinrtDll" and choose "Compile".
- If you run into the error `wait_for is not a member of winrt::impl`, follow the steps on https://github.com/adabru/BleWinrtDll/issues/16 and leave a thumbs up. If enough thumbs accumulate, someone or me will try to make that more convenient.
- Wait until the compilation finishes successfully.

Now you find the file `BleWinrtDll.dll` in the folder `ARM64/Release`. You can copy this dll into your Unity-project. To try it out, you can also copy the file into the `DebugBle` folder (replacing the existing file) and start the DebugBle project. If your computer has bluetooth enabled, you should see some scanned bluetooth devices. If you modify the file `DebugBle/Program.cs` and change the device name, service UUID and characteristic UUIDs to match your specific BLE device, you should also receive some packages from your BLE device.

## Background

My goal was to send data from Arduino to HoloLens 2 through BLE created in Unity Engine. 

[comment]: <> (I describe this process in the medium blog.)
This repo is forked from https://github.com/adabru/BleWinrtDll, where the Dll is configured to work with UWP Unity. 
It worked well with Arduino and Unity Editor, but did not translate well to HoloLens. 
So I include here adjusted Unity Project ready to be built to HoloLens together with Arduino Code used. For more information check out the original repo: https://github.com/adabru/BleWinrtDll
