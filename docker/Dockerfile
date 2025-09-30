# Create Dockerfile for the new structure
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY stibe.api.csproj .
RUN dotnet restore

# Copy source code
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Copy configuration files
COPY config/ ./config/

# Create logs directory
RUN mkdir -p /app/logs

# Set entry point
ENTRYPOINT ["dotnet", "stibe.api.dll"]