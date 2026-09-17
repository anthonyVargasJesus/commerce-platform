FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["src/Inventory.Domain/Inventory.Domain.csproj", "src/Inventory.Domain/"]
COPY ["src/Inventory.Application/Inventory.Application.csproj", "src/Inventory.Application/"]
COPY ["src/Inventory.Infrastructure/Inventory.Infrastructure.csproj", "src/Inventory.Infrastructure/"]
COPY ["src/Inventory.API/Inventory.API.csproj", "src/Inventory.API/"]
RUN dotnet restore "src/Inventory.API/Inventory.API.csproj"

COPY . .
RUN dotnet publish "src/Inventory.API/Inventory.API.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Inventory.API.dll"]
