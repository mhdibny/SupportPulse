# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SupportPulse.App/SupportPulse.App.csproj", "SupportPulse.App/"]
COPY ["SupportPulse.Core/SupportPulse.Core.csproj", "SupportPulse.Core/"]
COPY ["SupportPulse.Data/SupportPulse.Data.csproj", "SupportPulse.Data/"]
RUN dotnet restore "./SupportPulse.App/SupportPulse.App.csproj"
COPY . .
WORKDIR "/src/SupportPulse.App"
RUN dotnet build "./SupportPulse.App.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SupportPulse.App.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create upload directory with write permissions for the non‑root user
RUN mkdir -p /app/UploadedFiles/Chats && chmod 777 /app/UploadedFiles/Chats
VOLUME ["/app/UploadedFiles"]

ENTRYPOINT ["dotnet", "SupportPulse.App.dll"]