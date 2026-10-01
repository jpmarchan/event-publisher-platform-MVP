FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/EventService/EventService.Api/EventService.Api.csproj", "src/EventService/EventService.Api/"]
COPY ["src/EventService/EventService.Application/EventService.Application.csproj", "src/EventService/EventService.Application/"]
COPY ["src/EventService/EventService.Infrastructure/EventService.Infrastructure.csproj", "src/EventService/EventService.Infrastructure/"]
COPY ["src/EventService/EventService.Domain/EventService.Domain.csproj", "src/EventService/EventService.Domain/"]
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "src/Shared.Contracts/"]
RUN dotnet restore "src/EventService/EventService.Api/EventService.Api.csproj"

COPY ["src/EventService/", "src/EventService/"]
COPY ["src/Shared.Contracts/", "src/Shared.Contracts/"]
RUN dotnet publish "src/EventService/EventService.Api/EventService.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "EventService.Api.dll"]
