# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["GLMS.Web/GLMS.Web.csproj", "GLMS.Web/"]
RUN dotnet restore "GLMS.Web/GLMS.Web.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/GLMS.Web"
RUN dotnet publish "GLMS.Web.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "GLMS.Web.dll"]