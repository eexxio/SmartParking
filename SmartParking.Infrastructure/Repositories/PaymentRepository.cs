using System;
using System.Collections.Generic;
using Npgsql;
using Microsoft.Extensions.Configuration;
using SmartParking.Infrastructure.Interfaces;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;
using SmartParking.Domain.Exceptions;

namespace SmartParking.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly string _connectionString;

        public PaymentRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration), "DefaultConnection not found in configuration");
        }

        public Guid Create(Payment payment)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                using var command = new NpgsqlCommand(
                    "SELECT * FROM sp_create_payment(@p_reservation_id, @p_amount)",
                    connection);

                command.Parameters.AddWithValue("p_reservation_id", payment.ReservationId);
                command.Parameters.AddWithValue("p_amount", payment.Amount);

                connection.Open();
                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    return reader.GetGuid(reader.GetOrdinal("payment_id"));
                }

                throw new InvalidOperationException("Failed to create payment");
            }
            catch (PostgresException ex) when (ex.SqlState == "45301")
            {
                throw new InvalidPaymentException("Payment amount must be greater than 0");
            }
            catch (PostgresException ex) when (ex.SqlState == "45302")
            {
                throw new PaymentNotFoundException("Reservation not found");
            }
            catch (PostgresException ex) when (ex.SqlState == "45303")
            {
                throw new InvalidPaymentException("Payment already completed for this reservation");
            }

            catch (PostgresException ex)
            {
                throw new PaymentProcessingException("Error creating payment", ex);
            }
        }

        public Payment GetById(Guid id)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                using var command = new NpgsqlCommand("SELECT * FROM sp_get_payment_by_id(@p_payment_id)", connection);

                command.Parameters.AddWithValue("p_payment_id", id);

                connection.Open();
                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    return MapPaymentFromReader(reader);
                }

                return null;
            }
            catch (PostgresException ex)
            {
                throw new PaymentProcessingException($"Error retrieving payment with ID {id}", ex);
            }
        }

        public Payment GetByReservationId(Guid reservationId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                using var command = new NpgsqlCommand("SELECT * FROM sp_get_payment_by_reservation_id(@p_reservation_id)", connection);

                command.Parameters.AddWithValue("p_reservation_id", reservationId);

                connection.Open();
                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    return MapPaymentFromReader(reader);
                }

                return null;
            }
            catch (PostgresException ex)
            {
                throw new PaymentProcessingException($"Error retrieving payment for reservation {reservationId}", ex);
            }
        }

        public List<Payment> GetByUserId(Guid userId)
        {
            var payments = new List<Payment>();

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                using var command = new NpgsqlCommand("SELECT * FROM sp_get_payments_by_user_id(@p_user_id)", connection);

                command.Parameters.AddWithValue("p_user_id", userId);

                connection.Open();
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    payments.Add(MapPaymentFromReader(reader));
                }

                return payments;
            }
            catch (PostgresException ex)
            {
                throw new PaymentProcessingException($"Error retrieving payments for user {userId}", ex);
            }
        }

        public void UpdateStatus(Guid id, PaymentStatus status)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                using var command = new NpgsqlCommand(
                    "SELECT sp_update_payment_status(@p_payment_id, @p_payment_status)",
                    connection);

                command.Parameters.AddWithValue("p_payment_id", id);
                command.Parameters.AddWithValue("p_payment_status", status.ToString());

                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (PostgresException ex) when (ex.SqlState == "45304")
            {
                throw new InvalidPaymentException("Invalid payment status");
            }
            catch (PostgresException ex) when (ex.SqlState == "45305")
            {
                throw new PaymentNotFoundException(id);
            }

            catch (PostgresException ex)
            {
                throw new PaymentProcessingException($"Error updating payment status for ID {id}", ex);
            }
        }

        private Payment MapPaymentFromReader(NpgsqlDataReader reader)
        {
            var payment = new Payment
            {
                Id = reader.GetGuid(reader.GetOrdinal("id")),
                ReservationId = reader.GetGuid(reader.GetOrdinal("reservation_id")),
                Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                PaymentStatus = Enum.Parse<PaymentStatus>(reader.GetString(reader.GetOrdinal("payment_status"))),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            };

            return payment;
        }
    }
}
