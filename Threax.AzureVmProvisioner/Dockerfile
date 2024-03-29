FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base

# update apt-get and install curl
RUN apt-get update && apt-get install -y curl

# install docker
RUN curl -sSL https://get.docker.com/ | sh

# Install PowerShell
# Rollback and lock on to 7.2.7 due to issues reading public ip
# https://github.com/Azure/azure-powershell/issues/18721
RUN curl -L -o /tmp/powershell.tar.gz https://github.com/PowerShell/PowerShell/releases/download/v7.2.7/powershell-7.2.7-linux-x64.tar.gz
RUN mkdir -p /opt/microsoft/powershell/7
RUN tar zxf /tmp/powershell.tar.gz -C /opt/microsoft/powershell/7
RUN chmod +x /opt/microsoft/powershell/7/pwsh
RUN ln -s /opt/microsoft/powershell/7/pwsh /usr/bin/pwsh

#Install Az Powershell
RUN pwsh -c Install-Module -Name Az -Scope CurrentUser -Repository PSGallery -Force -RequiredVersion 6.0.0

#Install VSTeam
RUN pwsh -c Install-Module -Name VSTeam -Scope CurrentUser -Repository PSGallery -Force -RequiredVersion 7.2.0

# Install Openssh
RUN apt-get install -y openssh-client

# Install git
RUN apt-get install -y git

# Install certbot
RUN apt-get install -y python3 python3-venv libaugeas0
RUN python3 -m venv /opt/certbot/
RUN /opt/certbot/bin/pip install --upgrade pip
RUN /opt/certbot/bin/pip install certbot
RUN ln -s /opt/certbot/bin/certbot /usr/bin/certbot

# Install Dig
RUN apt-get install -y dnsutils

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Threax.AzureVmProvisioner/Threax.AzureVmProvisioner.csproj", "Threax.AzureVmProvisioner/"]
COPY ["Threax.Provision.AzPowershell/Threax.Provision.AzPowershell.csproj", "Threax.Provision.AzPowershell/"]
COPY ["Threax.Provision.Azure.Core/Threax.Provision.Azure.Core.csproj", "Threax.Provision.Azure.Core/"]
RUN dotnet restore "Threax.AzureVmProvisioner/Threax.AzureVmProvisioner.csproj"
COPY . .
WORKDIR "/src/Threax.AzureVmProvisioner"
RUN dotnet build "Threax.AzureVmProvisioner.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Threax.AzureVmProvisioner.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Threax.AzureVmProvisioner.dll"]