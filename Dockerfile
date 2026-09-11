# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for caching restore step
COPY ["BookManagement.Api/BookManagement.Api.csproj", "BookManagement.Api/"]
COPY ["BookManagement.Service/BookManagement.Service.csproj", "BookManagement.Service/"]
COPY ["BookManagement.Repository/BookManagement.Repository.csproj", "BookManagement.Repository/"]
RUN dotnet restore "BookManagement.Api/BookManagement.Api.csproj"

# Copy source code and publish
COPY . .
WORKDIR "/src/BookManagement.Api"
RUN dotnet publish "BookManagement.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Container
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Default HTTP Port for .NET 8 (Render will override via PORT environment variable)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BookManagement.Api.dll"]
