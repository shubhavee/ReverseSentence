# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["ReverseSentence/ReverseSentence.csproj", "ReverseSentence/"]
RUN dotnet restore "ReverseSentence/ReverseSentence.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/ReverseSentence"
RUN dotnet build "ReverseSentence.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ReverseSentence.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReverseSentence.dll"]