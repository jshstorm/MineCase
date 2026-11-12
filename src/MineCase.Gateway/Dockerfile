FROM mcr.microsoft.com/dotnet/runtime:10.0
ARG source
WORKDIR /app
EXPOSE 25565
COPY ${source:-obj/Docker/publish} .
ENTRYPOINT ["dotnet", "MineCase.Gateway.dll"]
