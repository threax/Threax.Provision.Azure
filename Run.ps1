Param(
    [Parameter(Mandatory=$true)] [String] $command,
    [Parameter(Mandatory=$true)] [String] $path,
    [Parameter(Mandatory=$true)] [String] $glob,
    [Parameter(Mandatory=$false)] [String[]] $moreArgs
)

$scriptPath = Split-Path $script:MyInvocation.MyCommand.Path

$scriptPath = $(wsl wslpath -a "'$scriptPath'")
$path = $(wsl wslpath -a "'$path'")

wsl sudo docker run `
-it `
--rm `
-v /var/run/docker.sock:/var/run/docker.sock `
-v threax-provision-azurevm-home:/root `
-v threax-provision-azurevm-temp:/tmp `
-v ${path}:/input `
localhost:5000/threax/azurevmprovisioner:1.0 $command /input $glob @moreArgs