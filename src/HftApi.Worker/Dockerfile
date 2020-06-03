FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim
WORKDIR /app
EXPOSE 5000
COPY . .
ENTRYPOINT ["dotnet", "Lykke.HftApi.Worker.dll"]
