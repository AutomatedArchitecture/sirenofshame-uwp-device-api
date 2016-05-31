# Install.ps1
param($installPath, $toolsPath, $package, $project)

$xml = New-Object xml

# find the Web.config file
$config = $project.ProjectItems | where {$_.Name -eq "Package.appxmanifest"}

# find its path on the file system
$localPath = $config.Properties | where {$_.Name -eq "LocalPath"}

# load as XML
$xml.Load($localPath.Value)



$root = $xml.DocumentElement

$capabilities = $root.GetElementsByTagName('Capabilities')
if ($capabilities.Count -eq 0) {
    $capabilities = $xml.CreateElement('Capabilities')
    $root.AppendChild($capabilities)
}

$deviceCapability = $xml.CreateElement('DeviceCapability')
$nameAttr = $xml.CreateAttribute('Name')
$nameAttr.Value = "humaninterfacedevice"
$deviceCapability.Attributes.Append($nameAttr)
$capabilities.AppendChild($deviceCapability);

$device = $xml.CreateElement('Device')
$idAttr = $xml.CreateAttribute('Id')
$idAttr.Value = "vidpid:16D0 0646"
$device.Attributes.Append($idAttr)
$deviceCapability.AppendChild($device)

$function = $xml.CreateElement('Function')
$typeAttr = $xml.CreateAttribute('Type')
$typeAttr.Value = "usage:FF9C 0001"
$function.Attributes.Append($typeAttr)
$device.AppendChild($function);



# save the Web.config file
$xml.Save($localPath.Value)