FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine as builder
WORKDIR /usr/src/backend
COPY . .

RUN apk update
RUN apk add zip
RUN apk add git

RUN dotnet publish -c release -o out -r ubuntu.18.04-x64 --self-contained
RUN ls -lsa

# FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS runtime
FROM mcr.microsoft.com/dotnet/sdk AS runtime
COPY --from=builder /usr/src/backend/out /usr/local/bin/backend
EXPOSE 8000 7890
RUN apt-get update
RUN apt-get install -y net-tools
CMD ["dotnet", "/usr/local/bin/backend/StalingradServerLinux.dll"]