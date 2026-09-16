FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Dymatrix.Notifications.sln", "."]
COPY ["src/Dymatrix.Notifications.Api/Dymatrix.Notifications.Api.csproj", "src/Dymatrix.Notifications.Api/"]
COPY ["src/Dymatrix.Notifications.Application/Dymatrix.Notifications.Application.csproj", "src/Dymatrix.Notifications.Application/"]
COPY ["src/Dymatrix.Notifications.Domain/Dymatrix.Notifications.Domain.csproj", "src/Dymatrix.Notifications.Domain/"]
COPY ["src/Dymatrix.Notifications.Infrastructure/Dymatrix.Notifications.Infrastructure.csproj", "src/Dymatrix.Notifications.Infrastructure/"]
RUN dotnet restore "src/Dymatrix.Notifications.Api/Dymatrix.Notifications.Api.csproj"

COPY src/ src/
RUN dotnet publish "src/Dymatrix.Notifications.Api/Dymatrix.Notifications.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Dymatrix.Notifications.Api.dll"]
