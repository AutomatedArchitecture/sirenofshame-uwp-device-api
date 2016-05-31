# SirenOfShame.Device Nuget Package

To create your own custom siren of shame device software:

1. In Visual Studio Create new Universal Windows Platform project

2. In Nuget Package Manager: 

Install-Package SirenOfShame.Uwp.Device

3. Add the following to the Package.appxmanifest:

  <Capabilities>
    <DeviceCapability Name="humaninterfacedevice">
      <Device Id="vidpid:16D0 0646">
        <Function Type="usage:FF9C 0001"/>
      </Device>
    </DeviceCapability>
  </Capabilities>
  
4. Instantiate a SirenOfShameDevice, subscribe to Connected, try turning on the led's like this:

public MainPage() {
  _sirenOfShameDevice = new SirenOfShameDevice();
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

5. For more details on how to use the API check out the SirenOfShame.HardwareTestGui project