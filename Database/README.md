# Smart Parking System - Database Setup

This folder contains all database scripts for the Smart Parking System project.

## 📁 Files Overview

| File | Description | Owner |
|------|-------------|-------|
| `000_MASTER_SETUP.sql` | Master script that runs all other scripts in order | All |
| `001_CreateTables.sql` | Creates database and all 6 tables | Member 1 |
| `002_UserWallet_StoredProcedures.sql` | User & Wallet stored procedures | Member 1 |
| `003_ParkingSpot_StoredProcedures.sql` | ParkingSpot stored procedures | Member 2 |
| `004_Reservation_Penalty_StoredProcedures.sql` | Reservation & Penalty stored procedures | Member 3 |
| `005_Payment_StoredProcedures.sql` | Payment stored procedures | Member 4 |
| `006_SeedData.sql` | Test data for development | All |

## 🚀 How to Set Up the Database

### Option 1: Run Master Script (Recommended)

1. Open **SQL Server Management Studio (SSMS)**
2. Connect to your SQL Server instance
3. Click **File → Open → File**
4. Navigate to `Database/000_MASTER_SETUP.sql`
5. Click **Execute** (or press F5)

This will automatically run all scripts in the correct order.

### Option 2: Run Scripts Individually

If you prefer to run scripts one by one:

1. Open SSMS
2. Execute scripts in this order:
   - `001_CreateTables.sql`
   - `002_UserWallet_StoredProcedures.sql`
   - `003_ParkingSpot_StoredProcedures.sql`
   - `004_Reservation_Penalty_StoredProcedures.sql`
   - `005_Payment_StoredProcedures.sql`
   - `006_SeedData.sql` (optional, for test data)

### Option 3: Command Line (sqlcmd)

```bash
sqlcmd -S localhost -i Database\000_MASTER_SETUP.sql
```

## 📊 Database Schema

### Tables Created

1. **Users** - User accounts
2. **UserWallets** - User balance (app coins)
3. **ParkingSpots** - Parking spot information
4. **Reservations** - Parking reservations
5. **Payments** - Payment transactions
6. **Penalties** - Penalty records

### Stored Procedures by Member

#### Member 1 - User & Wallet (9 procedures)
- `sp_CreateUser`
- `sp_GetUserById`
- `sp_GetUserByEmail`
- `sp_UpdateUser`
- `sp_GetAllUsers`
- `sp_GetWalletByUserId`
- `sp_AddToWallet`
- `sp_DeductFromWallet`
- `sp_GetWalletBalance`

#### Member 2 - ParkingSpot (8 procedures)
- `sp_CreateParkingSpot`
- `sp_GetParkingSpotById`
- `sp_GetAllParkingSpots`
- `sp_GetAvailableParkingSpots`
- `sp_GetAvailableSpotsByType`
- `sp_UpdateSpotOccupancy`
- `sp_ValidateSpotForUser`
- `sp_GetSpotStatistics`

#### Member 3 - Reservation & Penalty (10 procedures)
- `sp_CreateReservation`
- `sp_GetReservationById`
- `sp_GetReservationsByUserId`
- `sp_ConfirmReservation`
- `sp_CancelReservation`
- `sp_CompleteReservation`
- `sp_GetExpiredPendingReservations`
- `sp_CreatePenalty`
- `sp_GetPenaltiesByReservationId`
- `sp_GetPenaltiesByUserId`

#### Member 4 - Payment (8 procedures)
- `sp_CreatePayment`
- `sp_GetPaymentById`
- `sp_GetPaymentByReservationId`
- `sp_GetPaymentsByUserId`
- `sp_UpdatePaymentStatus`
- `sp_CalculatePaymentAmount`
- `sp_ProcessPayment`
- `sp_GetPaymentStatistics`

## 🧪 Test Data

After running `006_SeedData.sql`, you will have:

- **5 test users:**
  - john.doe@email.com (Regular user, Balance: 150 coins)
  - jane.smith@email.com (EV user, Balance: 200 coins)
  - michael.johnson@email.com (EV user, Balance: 100 coins)
  - sarah.williams@email.com (Regular user, Balance: 175 coins)
  - david.brown@email.com (Regular user, Balance: 125 coins)

- **10 parking spots:**
  - 7 Regular spots (A-001-REG to C-001-REG)
  - 3 EV spots (EV-01-CHG to EV-03-CHG)

- **Sample reservations and payments** for testing

## 🔌 Connection String for C#

Use this connection string in your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "SmartParkingDB": "Server=localhost;Database=SmartParkingDB;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

Or with SQL Server Authentication:

```json
{
  "ConnectionStrings": {
    "SmartParkingDB": "Server=localhost;Database=SmartParkingDB;User Id=your_username;Password=your_password;TrustServerCertificate=true;"
  }
}
```

## ✅ Verification Queries

After setup, run these queries to verify everything is working:

```sql
USE SmartParkingDB;

-- Check all tables exist
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';

-- Check stored procedures
SELECT ROUTINE_NAME FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE';

-- Check test data
SELECT COUNT(*) AS TotalUsers FROM Users;
SELECT COUNT(*) AS TotalSpots FROM ParkingSpots;
SELECT COUNT(*) AS AvailableSpots FROM ParkingSpots WHERE IsOccupied = 0;
```

## 🔄 Reset Database

To completely reset the database and start fresh:

1. Run `000_MASTER_SETUP.sql` again (it will drop and recreate everything)

Or manually:

```sql
USE master;
DROP DATABASE SmartParkingDB;
-- Then run 000_MASTER_SETUP.sql
```

## 📝 Notes

- All validation is built into the stored procedures
- Error codes start at 50000 and are grouped by member (50001-50099 for Member 1, etc.)
- All monetary values use `DECIMAL(10, 2)` for precision
- All IDs use `UNIQUEIDENTIFIER` (GUID)
- Transactions are used for operations that modify multiple tables

## 🆘 Troubleshooting

**Problem:** "Database already exists" error
**Solution:** The master script automatically drops and recreates the database

**Problem:** "Cannot drop database because it is currently in use"
**Solution:** Close all connections to SmartParkingDB and run again

**Problem:** Stored procedures not found
**Solution:** Make sure you ran all scripts in order (or use the master script)

**Problem:** Foreign key constraint errors
**Solution:** Make sure you're using the provided stored procedures, not direct INSERT statements

---

**Ready to code!** The database is now set up and ready for C# development. Each team member can start working on their respective modules.
