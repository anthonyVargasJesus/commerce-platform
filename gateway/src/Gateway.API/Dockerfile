FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["src/Gateway.API/Gateway.API.csproj", "src/Gateway.API/"]
RUN dotnet restore "src/Gateway.API/Gateway.API.csproj"

COPY . .
RUN dotnet publish "src/Gateway.API/Gateway.API.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Gateway.API.dll"]
