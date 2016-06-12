## Summary

This project provides an API for Universal Windows 
Platform projects to access a [siren of shame](http://sirenofshame.com) device.

## Project Structure

The Visual Studio solution consists of three components:

* SirenOfShameUwpDeviceApi - the API for accessing a siren
* SirenOfShame.HardwareTestGui - a sample project
* \Build - the Nuget spec and packages

## Getting Started

To create your own custom siren of shame device software:

1. In Visual Studio Create new Universal Windows Platform project

2. In Nuget Package Manager: 

    `Install-Package SirenOfShame.Device`

3. Add the following to the Package.appxmanifest:  

    ```xml
    <Capabilities>
      <DeviceCapability Name="humaninterfacedevice">
        <Device Id="vidpid:16D0 0646">
          <Function Type="usage:FF9C 0001"/>
        </Device>
      </DeviceCapability>
    </Capabilities>
    ```

4. Instantiate a SirenOfShameDevice, subscribe to Connected, try turning on the led's like this:

```c#
public MainPage() {
  _sirenOfShameDevice = new SirenOfShameDevice();
  _sirenOfShameDevice.StartWatching();
  _sirenOfShameDevice.Connected += SirenOfShameDeviceOnConnected;
}

private async void SirenOfShameDeviceOnConnected(object sender, EventArgs eventArgs) {
  var manualControlData = new ManualControlData
  {
      Led0 = (byte)255,
      Led1 = (byte)255,
      Led2 = (byte)255,
      Led3 = (byte)255,
      Led4 = (byte)255,
      Siren = false
  };
  await _sirenOfShameDevice.ManualControl(manualControlData);
}

public void Dispose()
{
    _sirenOfShameDevice.Dispose();
}
```

<ol start="5">
  <li>For more details on how to use the API check out the SirenOfShame.HardwareTestGui project</li>
</ol>
