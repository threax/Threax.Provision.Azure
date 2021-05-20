#!/bin/bash

# Needs Error Checking

# Setup Firewall
sudo ufw allow http
sudo ufw allow https
sudo ufw allow 22
sudo ufw --force enable

# Setup Docker
sudo apt-get update && \
sudo apt-get install apt-transport-https ca-certificates curl gnupg-agent software-properties-common -y && \
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add - && \
sudo apt-key fingerprint 0EBFCD88 && \
sudo add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" && \
sudo apt-get update && \
sudo apt-get install git docker-ce docker-ce-cli containerd.io -y

# Setup Network
sudo docker network create -d bridge appnet

# Create app dir
sudo mkdir /app

# Setup Threax.DockerTools

toolsBaseDir="/app/.tools/Threax.DockerTools"
cloneDir="$toolsBaseDir/src"
binaryDir="$toolsBaseDir/bin"

mkdir $toolsBaseDir
mkdir $cloneDir
mkdir $binaryDir
git clone https://github.com/threax/Threax.DockerTools.git $cloneDir
sudo bash "$cloneDir/Threax.DockerTools/Build.sh" 'linux-x64' $binaryDir
sudo chmod 700 "$binaryDir/Threax.DockerTools"
sudo /app/.tools/Threax.DockerTools/bin/Threax.DockerTools