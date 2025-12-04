# TestcontaienrsAutoSetup [![CI](https://github.com/DenysMalanichev/TestcontaienrsAutoSetup/actions/workflows/ci.yaml/badge.svg)](https://github.com/DenysMalanichev/TestcontaienrsAutoSetup/actions/workflows/ci.yaml)

## Docker under WSL
In case your Docker is running under WSL2 do not forget to 
expose the docker port:
``` bash
sudo mkdir -p /etc/systemd/system/docker.service.d
sudo vim /etc/systemd/system/docker.service.d/override.conf

# add the below to the override.conf file
[Service]
ExecStart=
ExecStart=/usr/bin/dockerd --host=tcp://0.0.0.0:2375 --host=unix:///var/run/docker.sock
```