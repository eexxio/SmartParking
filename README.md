# SmartParking
🚗 Smart Parking System — Project Documentation (C#)
1. Project Overview
The Smart Parking System is an application designed to help users view available parking spots, make reservations, manage payments, and track usage history.
The system follows Object-Oriented Architecture, a layered structure, uses SQL stored procedures, implements logging, exception handling, data validation, unit testing (including mocking), and integrates an external API for geolocation.

The application is implemented in C# (.NET) with a SQL relational database.

2. Team Members & Responsibilities
👤 Tudor — Backend Core Developer
Implements domain classes (ParkingLot, ParkingSpot, User, Reservation, Payment).
Writes business logic for reservations and pricing.
Implements validation and exception handling.

👤 Ioana — Database & Repository Developer
Designs the SQL database schema and relations.
Creates stored procedures (insert reservation, update payment, get available spots, etc.).
Implements Repository Layer + ADO.NET/Dapper/EF Core integration.

👤 Stefania — Testing & Logging Engineer
Implements more than 100 unit tests using MSTest/NUnit/xUnit.
Uses mocking frameworks (Moq / NSubstitute).
Implements logging (Serilog / NLog).

👤 Alexandru — API Integration & Service Layer Developer
Integrates external API (Google Maps Geocoding or OpenStreetMap Nominatim).
Builds the Service Layer for communication between controllers and repositories.
Implements the Reservation and Payment API endpoints.

3. System Features
 View free parking spots
 Reserve a spot (based on type: Normal / EV / Handicap)
 Automatic pricing calculation (price/hour * duration)
 Payment handling
 Penalty for expired reservations
 Cancellation flow
 Logging of all operations
 Validation of business rules
 Error handling with custom exceptions
