# Stage 1: Base runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8080

# Stage 2: SDK image for restoring and building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore caching
COPY ["BookManagement.Api/BookManagement.Api.csproj", "BookManagement.Api/"]
COPY ["BookManagement.Repository/BookManagement.Repository.csproj", "BookManagement.Repository/"]
COPY ["BookManagement.Service/BookManagement.Service.csproj", "BookManagement.Service/"]
RUN dotnet restore "BookManagement.Api/BookManagement.Api.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/BookManagement.Api"
RUN dotnet build "BookManagement.Api.csproj" -c Release -o /app/build

# Stage 3: Publish application
FROM build AS publish
RUN dotnet publish "BookManagement.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Final runtime container
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookManagement.Api.dll"]
