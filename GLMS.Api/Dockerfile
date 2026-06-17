# Stage 1: Build
# Use the full SDK image to compile the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies first
# Docker caches this layer so it only re-runs when .csproj changes
COPY ["GLMS.Api/GLMS.Api.csproj", "GLMS.Api/"]
RUN dotnet restore "GLMS.Api/GLMS.Api.csproj"

# Copy all remaining source code
COPY . .

# Build and publish the release version
WORKDIR "/src/GLMS.Api"
RUN dotnet publish "GLMS.Api.csproj" -c Release -o /app/publish

# Stage 2: Runtime
# Use only the lightweight runtime image — not the full SDK
# This keeps the final image small and secure
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "GLMS.Api.dll"]