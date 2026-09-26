# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore from the project files alone, so this layer stays cached until a dependency changes.
COPY src/PaymentGateway.Api/PaymentGateway.Api.csproj src/PaymentGateway.Api/
COPY src/PaymentGateway.Application/PaymentGateway.Application.csproj src/PaymentGateway.Application/
COPY src/PaymentGateway.Domain/PaymentGateway.Domain.csproj src/PaymentGateway.Domain/
COPY src/PaymentGateway.Infrastructure/PaymentGateway.Infrastructure.csproj src/PaymentGateway.Infrastructure/
RUN dotnet restore src/PaymentGateway.Api/PaymentGateway.Api.csproj

COPY .editorconfig ./
COPY src/ src/
RUN dotnet publish src/PaymentGateway.Api/PaymentGateway.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app \
    /p:UseAppHost=false

# Chiseled Ubuntu: only the .NET runtime and its dependencies. No shell or package manager,
# so a smaller attack surface, and it runs as a non-root user.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "PaymentGateway.Api.dll"]
