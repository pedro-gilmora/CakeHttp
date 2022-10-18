#!/bin/bash

if [ -f /usr/share/dotnet6 ]; then
    echo "Update is done
"
else
    echo "Creating and moving to a new dotnet folder and download and deploy last version
"
    cd /usr/share
    sudo mv dotnet dotnet6
    sudo mkdir dotnet && cd dotnet && sudo curl -Lo dotnet.tar.gz https://download.visualstudio.microsoft.com/download/pr/f5c74056-330b-452b-915e-d98fda75024e/18076ca3b89cd362162bbd0cbf9b2ca5/dotnet-sdk-7.0.100-rc.2.22477.23-linux-x64.tar.gz && sudo tar -xf dotnet.tar.gz && sudo rm dotnet.tar.gz;
    sudo rmdir dotnet6 --ignore-fail-on-non-empty
fi

# if [[ $DOTNET == "" ]]; then
#     echo "\$DOTNET doesn't exists
# "
#     DOTNET=$PWD/dotnet
#     export DOTNET;
#     $DOTNET --version;
# fi