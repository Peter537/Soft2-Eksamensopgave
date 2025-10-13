# Legacy MToGo System

Legacy customer management system for the MTOGO food delivery platform.

## Project Structure

```
LegacyMToGoSystem/
├── Controllers/        # HTTP endpoints
├── Services/          # Business logic
├── Repositories/      # Data access
├── Models/            # Domain entities
├── DTOs/              # Data transfer objects
└── Infrastructure/    # Database abstraction
```

## Running the Application

```bash
dotnet run
```

API available at: `https://localhost:5001`  
Swagger UI: `https://localhost:5001`

## Test Credentials

- **Peter Andersen** (ID: 1): `pean@outlook.dk` / `Peter123!`
- **Yusef Khafaji** (ID: 3): `Joe@gmail.com` / `Yusef789!`
- **Oskar Olsen** (ID: 2): `odo@yahoo.dk` / `Oskar456!` (soft-deleted)

## API Endpoints

- `POST /api/customers/register` - Create new customer (auto-increment ID)
- `POST /api/customers/login` - Authenticate customer
- `GET /api/customers` - Get all customers
- `GET /api/customers/{id}` - Get customer by ID (e.g., /api/customers/1)
- `DELETE /api/customers/{id}` - Soft delete customer

## Database

JSON file storage in `Data/` directory with SQL-style auto-incrementing integer IDs. Interface-based design allows easy migration to PostgreSQL.
