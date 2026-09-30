FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["RaigonArts.Api.csproj", "./"]
RUN dotnet restore "RaigonArts.Api.csproj"

COPY . .
RUN dotnet publish "RaigonArts.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "RaigonArts.Api.dll"]