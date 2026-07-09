# Use the .NET SDK image for build and publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["Finefolio.ValuationApi.csproj", "./"]
RUN dotnet restore "Finefolio.ValuationApi.csproj"

# Copy the remaining source files and publish
COPY . .
RUN dotnet publish "Finefolio.ValuationApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy application files from the build stage
COPY --from=build /app/publish .

# Expose the default ASP.NET Core port and use a non-root user if desired
EXPOSE 80

RUN apt-get update && apt-get install -y libgssapi-krb5-2
# Setting the db connection string
ENV ConnectionStrings__DefaultConnection="Host=ffpostgres;Port=5432;Database=postgres;Username=ffdb;Password="

# Set the entry point
ENTRYPOINT ["dotnet", "Finefolio.ValuationApi.dll"]
