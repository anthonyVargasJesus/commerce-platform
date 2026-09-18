FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["src/Notifications.Domain/Notifications.Domain.csproj", "src/Notifications.Domain/"]
COPY ["src/Notifications.Application/Notifications.Application.csproj", "src/Notifications.Application/"]
COPY ["src/Notifications.Infrastructure/Notifications.Infrastructure.csproj", "src/Notifications.Infrastructure/"]
COPY ["src/Notifications.API/Notifications.API.csproj", "src/Notifications.API/"]
RUN dotnet restore "src/Notifications.API/Notifications.API.csproj"

COPY . .
RUN dotnet publish "src/Notifications.API/Notifications.API.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Notifications.API.dll"]
