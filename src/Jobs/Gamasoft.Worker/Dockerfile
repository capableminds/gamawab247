#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/Jobs/Gamasoft.Worker/Gamasoft.Worker.csproj", "src/Jobs/Gamasoft.Worker/"]
COPY ["src/Libraries/Application/Application.csproj", "src/Libraries/Application/"]
COPY ["src/Libraries/ApiCustomization/ApiCustomization.csproj", "src/Libraries/ApiCustomization/"]
COPY ["src/Libraries/Infrastructure/Infrastructure.csproj", "src/Libraries/Infrastructure/"]
COPY ["src/Libraries/Infrastructure.Data/Infrastructure.Data.csproj", "src/Libraries/Infrastructure.Data/"]
COPY ["src/Libraries/Domian/Domain.csproj", "src/Libraries/Domian/"]
RUN dotnet restore "src/Jobs/Gamasoft.Worker/Gamasoft.Worker.csproj"
COPY . .
WORKDIR "/src/src/Jobs/Gamasoft.Worker"
RUN dotnet build "Gamasoft.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Gamasoft.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gamasoft.Worker.dll"]
