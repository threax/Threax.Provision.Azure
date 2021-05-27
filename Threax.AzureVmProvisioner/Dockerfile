FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base

# update apt-get and install curl
RUN apt-get update && apt-get install -y curl

# install docker
RUN curl -sSL https://get.docker.com/ | sh

# Install PowerShell
RUN apt-get update
RUN apt-get install -y wget apt-transport-https software-properties-common
RUN wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN apt-get update
RUN apt-get install -y powershell

#Install Az Powershell
RUN pwsh -c Install-Module -Name Az -Scope CurrentUser -Repository PSGallery -Force

# Install Openssh
RUN apt-get install -y openssh-client

# Install git
RUN apt-get install -y git

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