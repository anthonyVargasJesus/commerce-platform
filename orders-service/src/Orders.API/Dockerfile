FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["src/Orders.Domain/Orders.Domain.csproj", "src/Orders.Domain/"]
COPY ["src/Orders.Application/Orders.Application.csproj", "src/Orders.Application/"]
COPY ["src/Orders.Infrastructure/Orders.Infrastructure.csproj", "src/Orders.Infrastructure/"]
COPY ["src/Orders.API/Orders.API.csproj", "src/Orders.API/"]
RUN dotnet restore "src/Orders.API/Orders.API.csproj"

COPY . .
RUN dotnet publish "src/Orders.API/Orders.API.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Orders.API.dll"]
