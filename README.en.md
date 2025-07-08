# 🛒 E-Commerce Backend - Microservice Architecture (.NET 8)

This project demonstrates a microservice-based e-commerce backend built with .NET 8. It includes:

- **API**: Handles order creation and listing
- **Worker**: Consumes messages from RabbitMQ and logs to Redis
- **PostgreSQL**: Database
- **Redis**: Caching and logging
- **RabbitMQ**: Messaging system
- **JWT**: Authentication
- **Serilog**: Structured logging

---

## 🚀 Setup

### 0. Environment Variables (.env)

An `.env.example` file is provided. To use it:

```bash
cp .env.example .env
```

> Configuration primarily uses `appsettings.json`. `.env` is included for best practice and secret management.

### 1. Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/)
- [RabbitMQ](https://www.rabbitmq.com/download.html)
- [Redis (via Memurai or Docker)](https://www.memurai.com/download)

### 2. PostgreSQL Setup

- Create a database named `ECommerceDb`
- Connection string in `appsettings.json`:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=ECommerceDb;Username=postgres;Password=postgres"
```

### 3. Redis

- Should be running at `localhost:6379`
- Test with: `redis-cli → PING → PONG`

### 4. RabbitMQ

- After install, access it at:
  - [http://localhost:15672](http://localhost:15672)
  - Username: `guest`, Password: `guest`

---

## 🧪 Database Migration

```bash
cd ECommerce.Api
Update-Database
```

---

## 🧱 Running the Project

### Visual Studio Users

- Multi-startup configuration: `ECommerce.Api` + `ECommerce.Worker`

### CLI Users

```bash
cd ECommerce.Api && dotnet run
cd ECommerce.Worker && dotnet run
```

---

## 🧰 API Endpoints

### 🔐 Login - Get Token

```
POST /api/auth/login
```

```json
{
  "email": "test@example.com",
  "password": "123"
}
```

### 🛒 Create Order

```
POST /api/orders
```

```json
{
  "userId": "<guid>",
  "productId": "<guid>",
  "quantity": 2,
  "paymentMethod": 1
}
```

### 📦 Get User Orders

```
GET /api/orders/{userId}
```

---

## 🐳 Docker Support

Docker Compose sets up the following services:

- Redis
- PostgreSQL
- RabbitMQ

```bash
docker-compose up -d
```

---

## 📦 Postman Collection

- Import `postman-collection.json`
- Set Authorization → Bearer Token (from login response)

---

## 🧪 Tests

The solution includes both unit and integration tests:

### ✅ Unit Tests:

- `JwtServiceTests.cs`: Verifies token generation

### ✅ Integration Tests:

- `OrdersControllerTests.cs`: Ensures 401 is returned without token
- `OrdersControllerAuthorizedTests.cs`: Creates order using a valid JWT

### 🔧 Run Tests:

```bash
cd ECommerce.Tests
dotnet test
```

> Tests use `WebApplicationFactory` to launch the real API.

### ⚠️ Notes:

- `PreserveCompilationContext = true` must be set in `ECommerce.Api.csproj`
- `Program.cs` must include `public partial class Program {}`
- Dummy users and product IDs must exist or be seeded before running

---

## 🔍 End-to-End Flow

1. Login → receive token  
2. Create order  
3. Order is stored in DB and sent to RabbitMQ  
4. Worker consumes the message and updates Redis + DB  
5. Redis logs the order as `order_processed:{id}`  
6. Orders can be queried via `GET /api/orders/{userId}`

---

## 📂 Project Structure

```
ECommerce.Api         → Web API
ECommerce.Worker      → RabbitMQ Background Service
ECommerce.Infrastructure → DbContext, Repository, Caching
ECommerce.Shared      → DTOs, Events, Constants
```

---

## 📝 License

MIT
