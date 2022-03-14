To build the servers and run them:
```
$ COMPOSE_DOCKER_CLI_BUILD=1 DOCKER_BUILDKIT=1 docker-compose up -d --build
```

Check the logs for the StalingradServerLinux:
```shell
$ docker logs StalingradServerLinux
Web sockets server started at ws://0.0.0.0:7890
Listening for connections on http://StalingradServerLinux:8000/
```

Run the client StalingradClientLinux from container:


Run the client StalingradClientLinux from container:
```shell
$ docker exec -ti StalingradClientLinux /usr/local/bin/client/StalingradClientLinux
           ░░███████ ]▄▄▄▄▄▄▄▄
         ▄▄▄█████████▄▄▄ 
      [███████████████████]
     \°▲°▲°▲°▲°▲°▲°▲°▲°▲°▲°/
<<<Welcome to Stallingrad Battle V1>>
Press any key to start.
```



Debugging mongo from our host (docker host):
```shell
$ mongo 127.0.0.1:27017
> show databases;
admin        0.000GB
config       0.000GB
local        0.000GB
stalingrad0  0.000GB
```

Debugging mongo from the container:
```shell
$ docker exec -ti mongo mongo
> show databases;
admin        0.000GB
config       0.000GB
local        0.000GB
stalingrad0  0.000GB
```

Cleanup:
```shell
$ docker-compose stop
$ docker-compose rm
```