FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o published

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
ENV DOTNET_ENVIRONMENT=Production
COPY --from=build /app/published ./
ENTRYPOINT ["dotnet", "DataCollectorKitsu.Console.dll"]