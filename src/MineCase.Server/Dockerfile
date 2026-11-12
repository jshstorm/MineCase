FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG source
WORKDIR /app
EXPOSE 30000 8080
COPY ${source:-obj/Docker/publish} .
COPY ${source:-obj/Docker/publish}/config.docker.json config.json
ENTRYPOINT ["dotnet", "MineCase.Server.dll"]
