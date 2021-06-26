$scriptPath = Split-Path $script:MyInvocation.MyCommand.Path

docker build $scriptPath -f Threax.AzureVmProvisioner/Dockerfile -t localhost:5000/threax/azurevmprovisioner:1.0 --progress plain