Mac:
```bash
docker-compose down && dotnet restore && dotnet clean && dotnet build && dotnet ef database update --project DockerProject && docker-compose up -d
```

Windows:
```bash
docker-compose down; dotnet restore; dotnet clean; dotnet build; dotnet ef database update --project DockerProject; docker-compose up -d
```


