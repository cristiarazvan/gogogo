# Docker Project Setup

## Prerequisites
- Docker & Docker Compose installed
- .NET 9.0 SDK installed (for running migrations from host machine)
- Git

## Setup Instructions (Fresh Clone)

### Mac / Linux

**Step 1: Clone the repository**
```bash
git clone <repository-url>
cd <repository-directory>
```

**Step 2: Build and start Docker containers**
```bash
docker-compose up -d --build
```

**Step 3: Wait for MySQL to be ready (about 10 seconds), then run migrations**
```bash
sleep 10
cd DockerProject && dotnet ef database update --connection "Database=dockerproject;Host=localhost;Port=3306;User=dockeruser;Password=dockerpass;"
cd ..
```

**All-in-one command:**
```bash
docker-compose up -d --build && sleep 10 && cd DockerProject && dotnet ef database update --connection "Database=dockerproject;Host=localhost;Port=3306;User=dockeruser;Password=dockerpass;" && cd ..
```

---

### Windows (PowerShell)

**Step 1: Clone the repository**
```powershell
git clone <repository-url>
cd <repository-directory>
```

**Step 2: Build and start Docker containers**
```powershell
docker-compose up -d --build
```

**Step 3: Wait for MySQL to be ready (about 10 seconds), then run migrations**
```powershell
Start-Sleep -Seconds 10
cd DockerProject
dotnet ef database update --connection "Database=dockerproject;Host=localhost;Port=3306;User=dockeruser;Password=dockerpass;"
cd ..
```

**All-in-one command:**
```powershell
docker-compose up -d --build; Start-Sleep -Seconds 10; cd DockerProject; dotnet ef database update --connection "Database=dockerproject;Host=localhost;Port=3306;User=dockeruser;Password=dockerpass;"; cd ..
```

---

### Windows (Command Prompt)

**Step 1: Clone the repository**
```cmd
git clone <repository-url>
cd <repository-directory>
```

**Step 2: Build and start Docker containers**
```cmd
docker-compose up -d --build
```

**Step 3: Wait for MySQL to be ready (about 10 seconds), then run migrations**
```cmd
timeout /t 10 /nobreak
cd DockerProject
dotnet ef database update --connection "Database=dockerproject;Host=localhost;Port=3306;User=dockeruser;Password=dockerpass;"
cd ..
```

---

## Access the Application

Once setup is complete, your application will be running at:
- **Application**: http://localhost:8080
- **MySQL Database**: localhost:3306

## Useful Commands

**Stop all containers:**
```bash
docker-compose down
```

**View logs:**
```bash
docker-compose logs -f app
```

**Rebuild and restart after code changes:**
```bash
docker-compose restart app
```

**Reset database (drop and recreate):**
```bash
# Mac/Linux
docker-compose exec db mysql -uroot -prootpass -e "DROP DATABASE IF EXISTS dockerproject; CREATE DATABASE dockerproject;"
cd DockerProject && dotnet ef database update --connection "Database=dockerproject;Host=localhost;Port=3306;User=dockeruser;Password=dockerpass;" && cd ..

# Windows PowerShell
docker-compose exec db mysql -uroot -prootpass -e "DROP DATABASE IF EXISTS dockerproject; CREATE DATABASE dockerproject;"
cd DockerProject; dotnet ef database update --connection "Database=dockerproject;Host=localhost;Port=3306;User=dockeruser;Password=dockerpass;"; cd ..
```


