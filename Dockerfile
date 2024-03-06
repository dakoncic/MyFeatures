# Stage 1: Build the Angular app (AzureCICDUI)
FROM node:18 AS angular-build
WORKDIR /app
COPY AzureCICDUI/package*.json ./
RUN npm install
COPY AzureCICDUI/ .

# Introduce ARG for build environment
ARG BUILD_ENV=production
# Use the BUILD_ENV argument to specify the Angular build configuration
RUN if [ "$BUILD_ENV" = "production" ]; then \
  npm run build:prod; \
  else \
  npm run build:staging; \
  fi

# Stage 2: Build the ASP.NET app (AzureCICD)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS aspnet-build
WORKDIR /src

# Copy over the project files first and restore them
COPY Core/Core.csproj Core/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY AzureCICD/AzureCICD.csproj AzureCICD/

# Restore the Infrastructure project first since it has no dependencies
RUN dotnet restore Infrastructure/Infrastructure.csproj
# Restore the Core project which depends on Infrastructure
RUN dotnet restore Core/Core.csproj
# Finally, restore the main AzureCICD project
RUN dotnet restore AzureCICD/AzureCICD.csproj

# Now copy the rest of the files for the projects
COPY Infrastructure/ Infrastructure/
COPY Core/ Core/
COPY AzureCICD/ AzureCICD/

# Set the working directory to the main project's directory
WORKDIR /src/AzureCICD
# Publish the main project
RUN dotnet publish -c Release -o /app/publish

# Stage 3: Build the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Introduce ARG for the .NET environment within the runtime stage
ARG DOTNET_ENV=Production
# Set the ASP.NET Core environment variable based on the DOTNET_ENV argument
ENV ASPNETCORE_ENVIRONMENT=${DOTNET_ENV}

# Add a command to echo the ASPNETCORE_ENVIRONMENT to confirm it's set correctly
RUN echo "ASPNETCORE_ENVIRONMENT is set to ${ASPNETCORE_ENVIRONMENT}"

COPY --from=aspnet-build /app/publish .
# Copy the Angular build output from the 'dist/azure-cicdui' directory to 'wwwroot'
COPY --from=angular-build /app/dist/azure-cicdui ./wwwroot

RUN ls -la # This will list the files in the current directory

ENTRYPOINT ["dotnet", "AzureCICD.dll"]
