# Docknet ![Nuget](https://img.shields.io/nuget/v/docknet) ![Master Release Pipeline](https://github.com/bduman/docknet/workflows/Master%20Release%20Pipeline/badge.svg)
Pull docker image without docker installation - dotnet tool

# Installation

`dotnet tool install --global Docknet`

# Usage
Download image with this command.

`docknet pull ubuntu`

![image](https://user-images.githubusercontent.com/5374623/89088869-3923d100-d3a3-11ea-9aa5-93ab5a9b5508.png)

Then load image to docker client installed host 

`docker load -i library_ubuntu.tar`

# Thanks

Ported from python version - [NotGlop/docker-drag](https://github.com/NotGlop/docker-drag)
