Param(
    [Parameter(Mandatory=$false)] [String] $dockerHost = "localhost:5000"
)

$scriptPath = Split-Path $script:MyInvocation.MyCommand.Path
$scriptPath = $(wsl wslpath -a "'$scriptPath'")

wsl sudo docker build $scriptPath -f "$scriptPath/Threax.AzureVmProvisioner/Dockerfile" -t "$dockerHost/threax/azurevmprovisioner" --progress plain -t localhost:5000/threax/azurevmprovisioner:1.0