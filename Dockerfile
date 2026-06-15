FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY FlowDesk.slnx ./
COPY src/FlowDesk.Domain/FlowDesk.Domain.csproj src/FlowDesk.Domain/
COPY src/FlowDesk.Contracts/FlowDesk.Contracts.csproj src/FlowDesk.Contracts/
COPY src/FlowDesk.Application/FlowDesk.Application.csproj src/FlowDesk.Application/
COPY src/FlowDesk.Infrastructure/FlowDesk.Infrastructure.csproj src/FlowDesk.Infrastructure/
COPY src/FlowDesk.Api/FlowDesk.Api.csproj src/FlowDesk.Api/

RUN dotnet restore src/FlowDesk.Api/FlowDesk.Api.csproj

COPY src/ src/
RUN dotnet publish src/FlowDesk.Api/FlowDesk.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FlowDesk.Api.dll"]
