#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base

# update apt-get and install curl
RUN apt-get update && apt-get install -y curl

# install docker
RUN curl -sSL https://get.docker.com/ | sh

# Update the list of packages
RUN apt-get update
# Install pre-requisite packages.
RUN apt-get install -y wget apt-transport-https software-properties-common
# Download the Microsoft repository GPG keys
RUN wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb
# Register the Microsoft repository GPG keys
RUN dpkg -i packages-microsoft-prod.deb
# Update the list of packages after we added packages.microsoft.com
RUN apt-get update
# Install PowerShell
RUN apt-get install -y powershell

RUN pwsh -c Install-Module -Name Az -Scope CurrentUser -Repository PSGallery -Force

RUN apt-get install -y openssh-client

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