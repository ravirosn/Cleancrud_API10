CleanCrud API
🚀 Overview

CleanCrud API is a production-ready ASP.NET Core 10 Web API built using Clean Architecture principles. The project demonstrates secure authentication, scalable architecture, containerized deployment, structured logging, and modern backend development practices.

✨ Features

✔ Clean Architecture

✔ Repository Pattern

✔ Entity Framework Core

✔ SQL Server

✔ JWT Authentication

✔ Role-Based Authorization

✔ Global Exception Handling

✔ Serilog Logging

✔ Swagger/OpenAPI

✔ Docker Support

✔ File Upload

🔜 Refresh Token

🔜 Docker Compose

🔜 Redis Cache

🔜 AWS Deployment

🏗 Architecture
CleanCrud

├── CleanCrud.API

├── CleanCrud.Application

├── CleanCrud.Domain

└── CleanCrud.Infrastructure
🛠 Tech Stack
Technology	Usage
ASP.NET Core 10	Web API
Entity Framework Core	ORM
SQL Server	Database
JWT	Authentication
Docker	Containerization
Serilog	Logging
Swagger	API Documentation
🔐 Authentication

JWT Bearer Authentication

Role-Based Authorization

Secure Password Hashing

🐳 Docker
docker build -t cleancrud-api .

docker run -d -p 8080:8080 cleancrud-api
📷 Screenshots

Yahan baad me:

Swagger
Login API
Docker Desktop
SQL Server

ke screenshots add karenge.

⭐ Future Enhancements
Refresh Token
Forgot Password
Email Verification
Redis Cache
Docker Compose
AWS EC2 Deployment
GitHub Actions CI/CD
