# Use the official ASP.NET Core runtime as a base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the .NET SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file and project files with correct paths
COPY PaymentGateway.sln ./
COPY src/PaymentGateway.Api/*.csproj src/PaymentGateway.Api/
COPY src/PaymentGateway.Domain/*.csproj src/PaymentGateway.Domain/
COPY src/PaymentGateway.Infrastructure/*.csproj src/PaymentGateway.Infrastructure/
COPY src/PaymentGateway.Services/*.csproj src/PaymentGateway.Services/
COPY test/PaymentGateway.Api.Tests/*.csproj test/PaymentGateway.Api.Tests/
COPY test/PaymentGateway.Api.IntegrationTests/*.csproj test/PaymentGateway.Api.IntegrationTests/
COPY test/PaymentGateway.Api.EndToEndTests/*.csproj test/PaymentGateway.Api.EndToEndTests/

# Restore dependencies for all projects in the solution
RUN dotnet restore

# Copy the entire source code into the Docker image
COPY . .

# Set the working directory to the API project and build the app
WORKDIR /src/src/PaymentGateway.Api
RUN dotnet publish -c Release -o /app/publish

# Use the runtime image to run the app
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Set the environment variable for Heroku's dynamic port
ENV ASPNETCORE_URLS=http://*:$PORT

# Set the entry point
ENTRYPOINT ["dotnet", "PaymentGateway.Api.dll"]
