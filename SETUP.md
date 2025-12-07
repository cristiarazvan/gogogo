# Project Setup & Sync Guide

This guide contains all the commands you need to run after doing a `git pull` to ensure your local environment is in sync with the repository.

## Quick Sync (After Git Pull)

Run these commands in order after pulling changes:

```bash
# 1. Stop any running containers
docker-compose down

# 2. Rebuild Docker images (only if Dockerfile changed)
docker-compose build

# 3. Restore NuGet packages
dotnet restore

# 4. Clean previous builds
dotnet clean

# 5. Build the project
dotnet build

# 6. Update database to latest migration
dotnet ef database update --project DockerProject

# 7. Start Docker containers
docker-compose up -d

# 8. (Optional) View logs
docker-compose logs -f
```

## One-Line Sync Command

For convenience, here's a one-liner to run all sync steps:

```bash
docker-compose down && dotnet restore && dotnet clean && dotnet build && dotnet ef database update --project DockerProject && docker-compose up -d
```

## First-Time Setup

If this is your first time setting up the project:

```bash
# 1. Install EF Core tools globally (one-time only)
dotnet tool install --global dotnet-ef

# 2. Restore packages
dotnet restore

# 3. Build the project
dotnet build

# 4. Create/Update the database
dotnet ef database update --project DockerProject

# 5. Start Docker containers
docker-compose up -d
```

## Common Commands

### Database Operations

```bash
# Create a new migration
dotnet ef migrations add MigrationName --project DockerProject

# Update database to latest migration
dotnet ef database update --project DockerProject

# Rollback to a specific migration
dotnet ef database update PreviousMigrationName --project DockerProject

# Remove last migration (if not applied to database)
dotnet ef migrations remove --project DockerProject

# Drop the entire database
dotnet ef database drop --project DockerProject
```

### Docker Operations

```bash
# Start containers in background
docker-compose up -d

# Stop containers
docker-compose down

# Stop and remove volumes (CAUTION: Deletes data)
docker-compose down -v

# Rebuild images
docker-compose build

# View running containers
docker ps

# View logs
docker-compose logs -f

# Restart a specific service
docker-compose restart <service-name>
```

### Build & Run

```bash
# Clean build artifacts
dotnet clean

# Restore packages
dotnet restore

# Build the project
dotnet build

# Run the project (without Docker)
dotnet run --project DockerProject

# Build in Release mode
dotnet build -c Release
```

## Troubleshooting

### Database Connection Issues

1. Ensure Docker containers are running: `docker ps`
2. Check connection string in `appsettings.json`
3. Verify database exists: `dotnet ef database update --project DockerProject`

### Migration Conflicts

If you pulled migrations that conflict with your local database:

```bash
# Option 1: Reset database to match migrations
dotnet ef database drop --project DockerProject
dotnet ef database update --project DockerProject

# Option 2: Generate SQL script to review changes
dotnet ef migrations script --project DockerProject
```

### Docker Issues

```bash
# Clean up all Docker resources (CAUTION: Affects all Docker projects)
docker system prune -a

# Remove specific containers and volumes
docker-compose down -v
docker-compose up -d --force-recreate
```

### Build Errors After Pull

```bash
# Clean everything and rebuild
dotnet clean
rm -rf DockerProject/bin DockerProject/obj
dotnet restore
dotnet build
```

## Environment Variables

Create a `.env` file in the root directory for environment-specific settings:

```env
# Database
DB_CONNECTION_STRING=your_connection_string_here

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:5000;https://+:5001

# Docker
COMPOSE_PROJECT_NAME=dockerproject
```

## Pre-Pull Checklist

Before running `git pull`, make sure:

- [ ] All local changes are committed or stashed
- [ ] Docker containers are stopped: `docker-compose down`
- [ ] No pending database changes that need migration

## Post-Pull Checklist

After running `git pull`:

- [ ] Run `dotnet restore` to get new packages
- [ ] Run `dotnet build` to compile
- [ ] Run `dotnet ef database update --project DockerProject` to apply migrations
- [ ] Run `docker-compose up -d` to start services
- [ ] Verify application is running correctly

## Notes

- Always run `dotnet ef database update` after pulling new migrations
- The `.gitignore` file ensures build artifacts and sensitive data aren't committed
- Keep `appsettings.json` in version control, but use `appsettings.Development.json` for local overrides
- Database files (*.db, *.mdf, *.ldf) are ignored and won't be committed
