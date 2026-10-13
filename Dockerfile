
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["SaiRamTours.Api.csproj", "./"]
RUN dotnet restore "SaiRamTours.Api.csproj"

COPY . .
RUN dotnet publish "SaiRamTours.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "SaiRamTours.Api.dll"]
