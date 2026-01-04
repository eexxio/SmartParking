# Smart Parking System - Team Task Distribution

**Project:** Smart Parking System (C#)
**Team Size:** 4 members
**Total Unit Tests Required:** 100+ (25+ per member)

---

## 🎯 MEMBER 1: Database Setup + User & Wallet Management

### Module Ownership
- **Database Infrastructure** (Foundation for entire team)
- **User Management**
- **Wallet/Balance System**

### Tasks

#### 1. DATABASE SETUP (PRIORITY - Week 1)
**DO THIS FIRST - Everyone depends on this!**

- [ ] Create SQL Server database: `SmartParkingDB`
- [ ] Create migration script: `001_CreateTables.sql`
  - Table: `Users` (Id, Email, FullName, IsEVUser, CreatedAt, IsActive)
  - Table: `UserWallets` (Id, UserId, Balance, UpdatedAt)
  - Table: `ParkingSpots` (Id, SpotNumber, SpotType, IsOccupied, HourlyRate, CreatedAt)
  - Table: `Reservations` (Id, UserId, SpotId, StartTime, EndTime, Status, CancellationDeadline, CreatedAt)
  - Table: `Payments` (Id, ReservationId, Amount, PaymentStatus, CreatedAt)
  - Table: `Penalties` (Id, ReservationId, Amount, Reason, CreatedAt)
  - Add all PRIMARY KEYS, FOREIGN KEYS, and UNIQUE constraints
  - Add indexes on frequently queried columns
- [ ] Create stored procedures script: `002_UserWalletProcedures.sql`
  - `sp_CreateUser` (email, fullName, isEVUser)
  - `sp_GetUserById` (userId)
  - `sp_GetUserByEmail` (email)
  - `sp_UpdateUser` (userId, fullName, isEVUser)
  - `sp_CreateWallet` (userId, initialBalance)
  - `sp_GetWalletByUserId` (userId)
  - `sp_UpdateWalletBalance` (walletId, newBalance)
  - `sp_DeductFromWallet` (userId, amount)
  - `sp_AddToWallet` (userId, amount)
- [ ] Create seed data script: `003_SeedData.sql`
  - Insert 5 test users (2 EV users, 3 regular users)
  - Insert wallets with initial balances (e.g., 100 coins)
  - Insert 10 parking spots (3 EV, 7 regular)

#### 2. DOMAIN LAYER
- [ ] Create `SmartParking.Domain` project
- [ ] Create entity: `User.cs`
  - Properties: Id, Email, FullName, IsEVUser, CreatedAt, IsActive
  - Validation: Email required + valid format, FullName min 5 chars
  - Constructor with validation
- [ ] Create entity: `UserWallet.cs`
  - Properties: Id, UserId, Balance, UpdatedAt
  - Validation: Balance >= 0
  - Methods: `CanWithdraw(amount)`, `Deposit(amount)`, `Withdraw(amount)`
- [ ] Create custom exceptions:
  - `InvalidUserDataException`
  - `InsufficientBalanceException`
  - `UserNotFoundException`

#### 3. DATA ACCESS LAYER
- [ ] Create `SmartParking.DataAccess` project
- [ ] Create interface: `IUserRepository.cs`
  - Methods: `Create(User user)`, `GetById(Guid id)`, `GetByEmail(string email)`, `Update(User user)`, `Delete(Guid id)`
- [ ] Implement `UserRepository.cs`
  - Use ADO.NET or Dapper to call stored procedures
  - Implement all CRUD operations
  - Add proper exception handling and logging
- [ ] Create interface: `IWalletRepository.cs`
  - Methods: `Create(UserWallet wallet)`, `GetByUserId(Guid userId)`, `UpdateBalance(Guid walletId, decimal newBalance)`
- [ ] Implement `WalletRepository.cs`
  - Call stored procedures for wallet operations
  - Add transaction support for balance updates

#### 4. BUSINESS LOGIC LAYER
- [ ] Create `SmartParking.Business` project
- [ ] Create interface: `IUserService.cs`
  - Methods: `RegisterUser(string email, string fullName, bool isEVUser)`, `GetUser(Guid userId)`, `UpdateUser(Guid userId, string fullName, bool isEVUser)`
- [ ] Implement `UserService.cs`
  - Validate user data (email format, name length >= 5)
  - Check for duplicate emails
  - Create wallet automatically when user registers
  - Add logging for all operations
  - Implement exception handling
- [ ] Create interface: `IWalletService.cs`
  - Methods: `GetBalance(Guid userId)`, `Deposit(Guid userId, decimal amount)`, `Withdraw(Guid userId, decimal amount)`, `CanAfford(Guid userId, decimal amount)`
- [ ] Implement `WalletService.cs`
  - Validate amounts (must be > 0)
  - Check sufficient balance before withdrawal
  - Log all transactions
  - Throw `InsufficientBalanceException` when needed

#### 5. LOGGING SETUP
- [ ] Install Serilog or NLog package
- [ ] Configure logging to file and console
- [ ] Set up log levels (Debug, Info, Warning, Error)
- [ ] Create `LoggingConfiguration.cs`
- [ ] Add logging to all service methods

#### 6. UNIT TESTING (Minimum 25 tests)
- [ ] Create `SmartParking.Tests` project (xUnit or NUnit)
- [ ] Install Moq package for mocking
- [ ] Create `UserServiceTests.cs`
  - Test: `RegisterUser_ValidData_Success`
  - Test: `RegisterUser_InvalidEmail_ThrowsException`
  - Test: `RegisterUser_ShortName_ThrowsException` (name < 5 chars)
  - Test: `RegisterUser_DuplicateEmail_ThrowsException`
  - Test: `GetUser_ExistingId_ReturnsUser`
  - Test: `GetUser_NonExistingId_ThrowsException`
  - Mock `IUserRepository` for all tests
- [ ] Create `WalletServiceTests.cs`
  - Test: `Deposit_ValidAmount_Success`
  - Test: `Deposit_NegativeAmount_ThrowsException`
  - Test: `Withdraw_SufficientBalance_Success`
  - Test: `Withdraw_InsufficientBalance_ThrowsException`
  - Test: `Withdraw_NegativeAmount_ThrowsException`
  - Test: `GetBalance_ExistingWallet_ReturnsBalance`
  - Test: `CanAfford_SufficientBalance_ReturnsTrue`
  - Test: `CanAfford_InsufficientBalance_ReturnsFalse`
  - Mock `IWalletRepository` for all tests
- [ ] Add at least 10 more tests covering edge cases

#### Deliverables
- Database scripts (migrations + stored procedures)
- User and Wallet entities with validation
- Repositories with stored procedure calls
- Services with business logic
- Logging infrastructure
- 25+ unit tests with mocking

---

## 🎯 MEMBER 2: Parking Spot Management

### Module Ownership
- **Parking Spot System**
- **Spot Availability Logic**
- **EV vs Regular Spot Validation**

### Tasks

#### 1. DOMAIN LAYER
- [ ] Create enum: `SpotType.cs`
  - Values: `Regular`, `EV`
- [ ] Create entity: `ParkingSpot.cs`
  - Properties: Id, SpotNumber, SpotType, IsOccupied, HourlyRate, CreatedAt
  - Validation: SpotNumber min 5 chars, HourlyRate > 0
  - Constructor with validation
- [ ] Create custom exceptions:
  - `InvalidSpotDataException`
  - `SpotNotAvailableException`
  - `InvalidSpotTypeException`

#### 2. DATA ACCESS LAYER
- [ ] Create interface: `IParkingSpotRepository.cs`
  - Methods: `Create(ParkingSpot spot)`, `GetById(Guid id)`, `GetAll()`, `GetAvailableSpots()`, `GetAvailableSpotsByType(SpotType type)`, `UpdateOccupancy(Guid spotId, bool isOccupied)`
- [ ] Implement `ParkingSpotRepository.cs`
  - Use stored procedures for all operations
  - Add proper exception handling
- [ ] Create stored procedures: `003_ParkingSpotProcedures.sql`
  - `sp_CreateSpot` (spotNumber, spotType, hourlyRate)
  - `sp_GetSpotById` (spotId)
  - `sp_GetAllSpots`
  - `sp_GetAvailableSpots` (returns all spots where IsOccupied = 0)
  - `sp_GetAvailableSpotsByType` (spotType)
  - `sp_UpdateSpotOccupancy` (spotId, isOccupied)

#### 3. BUSINESS LOGIC LAYER
- [ ] Create interface: `IParkingSpotService.cs`
  - Methods: `CreateSpot(string spotNumber, SpotType type, decimal hourlyRate)`, `GetAllSpots()`, `GetAvailableSpots()`, `GetAvailableSpotsByType(SpotType type)`, `ValidateSpotForUser(Guid spotId, bool isEVUser)`, `MarkAsOccupied(Guid spotId)`, `MarkAsAvailable(Guid spotId)`
- [ ] Implement `ParkingSpotService.cs`
  - Validate spot data (spotNumber >= 5 chars, hourlyRate > 0)
  - Implement `ValidateSpotForUser` logic:
    - If spot is EV type and user is NOT EV → throw exception
    - If spot is Regular and user is EV → allow (EV users can use regular spots)
  - Check if spot is already occupied before marking as occupied
  - Add logging for all operations
  - Implement exception handling

#### 4. EXTERNAL API INTEGRATION (Optional but Recommended)
- [ ] Choose ONE of these APIs:
  - **Option A:** OpenWeatherMap API (show weather for parking location)
  - **Option B:** SendGrid/Twilio (send notifications about spot availability)
- [ ] Create interface: `IExternalApiService.cs` (if doing this)
  - Method: `SendNotification(string email, string message)` OR `GetWeather(string location)`
- [ ] Implement API client with HttpClient
- [ ] Add error handling for API failures
- [ ] Add configuration for API keys (appsettings.json)

#### 5. UNIT TESTING (Minimum 25 tests)
- [ ] Create `ParkingSpotServiceTests.cs`
  - Test: `CreateSpot_ValidData_Success`
  - Test: `CreateSpot_ShortSpotNumber_ThrowsException` (< 5 chars)
  - Test: `CreateSpot_NegativeRate_ThrowsException`
  - Test: `GetAvailableSpots_ReturnsOnlyUnoccupied`
  - Test: `GetAvailableSpotsByType_EV_ReturnsOnlyEVSpots`
  - Test: `GetAvailableSpotsByType_Regular_ReturnsOnlyRegularSpots`
  - Test: `ValidateSpotForUser_EVSpot_NonEVUser_ThrowsException`
  - Test: `ValidateSpotForUser_EVSpot_EVUser_Success`
  - Test: `ValidateSpotForUser_RegularSpot_EVUser_Success`
  - Test: `ValidateSpotForUser_RegularSpot_NonEVUser_Success`
  - Test: `MarkAsOccupied_AvailableSpot_Success`
  - Test: `MarkAsOccupied_AlreadyOccupied_ThrowsException`
  - Test: `MarkAsAvailable_OccupiedSpot_Success`
  - Mock `IParkingSpotRepository` for all tests
- [ ] Add at least 12 more tests covering edge cases
- [ ] If using external API, create mock tests for API calls

#### Deliverables
- ParkingSpot entity with validation
- Repository with stored procedures
- Service with spot availability logic
- EV vs Regular validation
- External API integration (optional)
- 25+ unit tests with mocking

---

## 🎯 MEMBER 3: Reservation & Penalty Management

### Module Ownership
- **Reservation System**
- **Cancellation Logic**
- **Penalty System**
- **Timeout Handling**

### Tasks

#### 1. DOMAIN LAYER
- [ ] Create enum: `ReservationStatus.cs`
  - Values: `Pending`, `Confirmed`, `Cancelled`, `Completed`
- [ ] Create entity: `Reservation.cs`
  - Properties: Id, UserId, SpotId, StartTime, EndTime, Status, CancellationDeadline, CreatedAt
  - Validation: StartTime < EndTime (if EndTime is set)
  - Methods: `CanCancel()` (check if before deadline), `CalculateDuration()` (hours)
- [ ] Create entity: `Penalty.cs`
  - Properties: Id, ReservationId, Amount, Reason, CreatedAt
  - Validation: Reason min 5 chars, Amount > 0
- [ ] Create custom exceptions:
  - `InvalidReservationException`
  - `ReservationNotFoundException`
  - `CancellationDeadlineExceededException`
  - `InvalidPenaltyException`

#### 2. DATA ACCESS LAYER
- [ ] Create interface: `IReservationRepository.cs`
  - Methods: `Create(Reservation reservation)`, `GetById(Guid id)`, `GetByUserId(Guid userId)`, `Update(Reservation reservation)`, `GetPendingReservations()`, `GetExpiredPendingReservations()`
- [ ] Implement `ReservationRepository.cs`
  - Use stored procedures
  - Add exception handling
- [ ] Create interface: `IPenaltyRepository.cs`
  - Methods: `Create(Penalty penalty)`, `GetByReservationId(Guid reservationId)`, `GetByUserId(Guid userId)`
- [ ] Implement `PenaltyRepository.cs`
- [ ] Create stored procedures: `004_ReservationPenaltyProcedures.sql`
  - `sp_CreateReservation` (userId, spotId, startTime, status, cancellationDeadline)
  - `sp_GetReservationById` (reservationId)
  - `sp_GetReservationsByUserId` (userId)
  - `sp_UpdateReservationStatus` (reservationId, status, endTime)
  - `sp_GetPendingReservations`
  - `sp_GetExpiredPendingReservations` (where Status = 'Pending' AND CancellationDeadline < NOW)
  - `sp_CreatePenalty` (reservationId, amount, reason)
  - `sp_GetPenaltiesByReservationId` (reservationId)

#### 3. BUSINESS LOGIC LAYER
- [ ] Create interface: `IReservationService.cs`
  - Methods: `CreateReservation(Guid userId, Guid spotId)`, `ConfirmReservation(Guid reservationId)`, `CancelReservation(Guid reservationId)`, `CompleteReservation(Guid reservationId)`, `GetUserReservations(Guid userId)`, `CheckAndApplyTimeoutPenalties()`
- [ ] Implement `ReservationService.cs`
  - **CreateReservation logic:**
    - Check if spot is available (call `IParkingSpotService.GetAvailableSpots`)
    - Get user info to check IsEVUser
    - Validate spot type for user
    - Set Status = Pending
    - Set StartTime = Now
    - Set CancellationDeadline = Now + 15 minutes
    - Mark spot as occupied
    - Log the operation
  - **ConfirmReservation logic:**
    - Check if reservation exists and is Pending
    - Change status to Confirmed
    - Log the operation
  - **CancelReservation logic:**
    - Check if reservation exists
    - Check if before CancellationDeadline
    - If AFTER deadline → call `ApplyPenalty(reservationId, 10, "Late cancellation")`
    - Change status to Cancelled
    - Mark spot as available
    - Log the operation
  - **CompleteReservation logic:**
    - Set EndTime = Now
    - Calculate duration (EndTime - StartTime)
    - Change status to Completed
    - Mark spot as available
    - Log the operation
  - **CheckAndApplyTimeoutPenalties logic:**
    - Get all expired pending reservations
    - For each: apply penalty, cancel reservation, free spot
- [ ] Create interface: `IPenaltyService.cs`
  - Methods: `ApplyPenalty(Guid reservationId, decimal amount, string reason)`, `GetUserPenalties(Guid userId)`
- [ ] Implement `PenaltyService.cs`
  - Validate penalty data (reason >= 5 chars, amount > 0)
  - Create penalty record
  - Optionally deduct from user wallet
  - Add logging

#### 4. UNIT TESTING (Minimum 25 tests)
- [ ] Create `ReservationServiceTests.cs`
  - Test: `CreateReservation_AvailableSpot_Success`
  - Test: `CreateReservation_OccupiedSpot_ThrowsException`
  - Test: `CreateReservation_EVSpot_NonEVUser_ThrowsException`
  - Test: `CreateReservation_SetsCancellationDeadline15Minutes`
  - Test: `ConfirmReservation_PendingReservation_Success`
  - Test: `ConfirmReservation_NonPendingReservation_ThrowsException`
  - Test: `CancelReservation_BeforeDeadline_NoPenalty`
  - Test: `CancelReservation_AfterDeadline_AppliesPenalty`
  - Test: `CancelReservation_FreesSpot`
  - Test: `CompleteReservation_SetsEndTime`
  - Test: `CompleteReservation_CalculatesDuration`
  - Test: `CompleteReservation_FreesSpot`
  - Test: `CheckAndApplyTimeoutPenalties_ExpiredReservations`
  - Mock `IReservationRepository`, `IParkingSpotService`, `IUserService`
- [ ] Create `PenaltyServiceTests.cs`
  - Test: `ApplyPenalty_ValidData_Success`
  - Test: `ApplyPenalty_ShortReason_ThrowsException` (< 5 chars)
  - Test: `ApplyPenalty_NegativeAmount_ThrowsException`
  - Test: `GetUserPenalties_ReturnsAllPenalties`
  - Mock `IPenaltyRepository`
- [ ] Add at least 8 more tests for edge cases

#### Deliverables
- Reservation and Penalty entities
- Repositories with stored procedures
- Services with cancellation and timeout logic
- Penalty application system
- 25+ unit tests with mocking

---

## 🎯 MEMBER 4: Payment Management + External API

### Module Ownership
- **Payment System**
- **Cost Calculation**
- **External API Integration** (Required)

### Tasks

#### 1. DOMAIN LAYER
- [ ] Create enum: `PaymentStatus.cs`
  - Values: `Pending`, `Completed`, `Failed`
- [ ] Create entity: `Payment.cs`
  - Properties: Id, ReservationId, Amount, PaymentStatus, CreatedAt
  - Validation: Amount > 0
- [ ] Create custom exceptions:
  - `InvalidPaymentException`
  - `PaymentProcessingException`
  - `PaymentNotFoundException`

#### 2. DATA ACCESS LAYER
- [ ] Create interface: `IPaymentRepository.cs`
  - Methods: `Create(Payment payment)`, `GetById(Guid id)`, `GetByReservationId(Guid reservationId)`, `Update(Payment payment)`, `GetByUserId(Guid userId)`
- [ ] Implement `PaymentRepository.cs`
  - Use stored procedures
  - Add exception handling
- [ ] Create stored procedures: `005_PaymentProcedures.sql`
  - `sp_CreatePayment` (reservationId, amount, status)
  - `sp_GetPaymentById` (paymentId)
  - `sp_GetPaymentByReservationId` (reservationId)
  - `sp_UpdatePaymentStatus` (paymentId, status)
  - `sp_GetPaymentsByUserId` (userId)

#### 3. BUSINESS LOGIC LAYER
- [ ] Create interface: `IPaymentService.cs`
  - Methods: `CalculatePaymentAmount(Guid reservationId)`, `ProcessPayment(Guid reservationId)`, `GetPaymentByReservation(Guid reservationId)`, `GetUserPayments(Guid userId)`
- [ ] Implement `PaymentService.cs`
  - **CalculatePaymentAmount logic:**
    - Get reservation details (StartTime, EndTime, SpotId)
    - If EndTime is null → throw exception
    - Calculate duration in hours: `(EndTime - StartTime).TotalHours`
    - Get spot's HourlyRate
    - Formula: `amount = hourlyRate * durationInHours`
    - Round to 2 decimal places
    - Return amount
  - **ProcessPayment logic:**
    - Calculate amount
    - Check if user has sufficient balance (call `IWalletService.CanAfford`)
    - Create payment record with Status = Pending
    - Deduct from wallet (call `IWalletService.Withdraw`)
    - If successful → update payment Status = Completed
    - If insufficient funds → Status = Failed, throw exception
    - Send payment confirmation via external API
    - Log the operation
  - Add exception handling for all payment failures

#### 4. EXTERNAL API INTEGRATION (REQUIRED)
- [ ] Choose ONE of these APIs:
  - **Option A:** SendGrid API (send email receipt)
  - **Option B:** Twilio API (send SMS confirmation)
  - **Option C:** Mock notification service
- [ ] Create interface: `INotificationService.cs`
  - Methods: `SendPaymentConfirmation(string email, decimal amount)`, `SendReservationConfirmation(string email, string spotNumber)`
- [ ] Implement `NotificationService.cs`
  - Set up HttpClient for API calls
  - Implement email/SMS sending
  - Add error handling for API failures (retry logic, fallback)
  - Add logging for all API calls
- [ ] Store API keys in `appsettings.json`
- [ ] Create configuration class for API settings

#### 5. UNIT TESTING (Minimum 25 tests)
- [ ] Create `PaymentServiceTests.cs`
  - Test: `CalculatePaymentAmount_ValidReservation_ReturnsCorrectAmount`
  - Test: `CalculatePaymentAmount_1Hour_CalculatesCorrectly`
  - Test: `CalculatePaymentAmount_3Point5Hours_CalculatesCorrectly`
  - Test: `CalculatePaymentAmount_NoEndTime_ThrowsException`
  - Test: `ProcessPayment_SufficientBalance_Success`
  - Test: `ProcessPayment_InsufficientBalance_ThrowsException`
  - Test: `ProcessPayment_DeductsFromWallet`
  - Test: `ProcessPayment_CreatesPaymentRecord`
  - Test: `ProcessPayment_UpdatesStatusToCompleted`
  - Test: `ProcessPayment_SendsNotification`
  - Test: `GetPaymentByReservation_ExistingPayment_ReturnsPayment`
  - Test: `GetPaymentByReservation_NoPayment_ReturnsNull`
  - Mock `IPaymentRepository`, `IReservationService`, `IWalletService`, `IParkingSpotService`, `INotificationService`
- [ ] Create `NotificationServiceTests.cs`
  - Test: `SendPaymentConfirmation_ValidData_Success`
  - Test: `SendPaymentConfirmation_ApiFailure_HandlesGracefully`
  - Test: `SendReservationConfirmation_ValidData_Success`
  - Mock HttpClient for API calls
- [ ] Add at least 10 more tests for edge cases

#### Deliverables
- Payment entity with validation
- Repository with stored procedures
- Service with cost calculation logic
- Wallet integration
- External API integration (notification service)
- 25+ unit tests with mocking

---

## 📊 Testing Summary

Each member must write **minimum 25 unit tests** covering:
1. Happy path scenarios
2. Validation failures (min 5 chars, required fields, etc.)
3. Business logic edge cases
4. Exception handling
5. Mock testing for dependencies

**Total: 100+ tests across the team**

---

## 🔄 Integration Plan

**Week 10: Integration Week**
- All members push their completed modules to the repository
- Create `SmartParking.Presentation` console application
- Register all services with Dependency Injection
- Create demo flow:
  1. Register user
  2. Add balance to wallet
  3. View available spots
  4. Create reservation
  5. Confirm reservation
  6. Complete reservation
  7. Process payment
  8. View payment history

---

## ✅ Checklist for Each Member

Before marking your module as "done":
- [ ] All entity classes have validation
- [ ] All repositories call stored procedures
- [ ] All services have logging
- [ ] All services have exception handling
- [ ] All methods have XML documentation comments
- [ ] All unit tests pass (25+ tests)
- [ ] Code follows C# naming conventions
- [ ] Git commits are regular with clear messages
- [ ] No hardcoded values (use configuration)

---

## 🚀 Git Workflow

1. Member 1 creates repository and pushes database scripts first
2. All members clone repository
3. Each member creates their own branch: `feature/user-management`, `feature/parking-spots`, etc.
4. Regular commits with messages like:
   - "Add User entity with validation"
   - "Implement UserRepository with stored procedures"
   - "Add unit tests for WalletService"
5. Code reviews before merging to main
6. Weekly sync to ensure compatibility

---

## 📞 Questions?

If you have questions about your tasks or need clarification on how modules interact, discuss as a team or ask your professor during lab sessions.

**Remember:** Each module is independent. You should be able to complete your tasks without waiting for others, using mocking for dependencies in your tests.
