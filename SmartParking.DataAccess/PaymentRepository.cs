using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;
using SmartParking.Domain.Exceptions;

namespace SmartParking.DataAccess.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly string _connectionString;

        public PaymentRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public Guid Create(Payment payment)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_CreatePayment", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@ReservationId", payment.ReservationId);
                        command.Parameters.AddWithValue("@Amount", payment.Amount);

                        var paymentIdParam = new SqlParameter("@PaymentId", SqlDbType.UniqueIdentifier)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(paymentIdParam);

                        connection.Open();
                        command.ExecuteNonQuery();

                        return (Guid)paymentIdParam.Value;
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50301)
                    throw new InvalidPaymentException("Payment amount must be greater than 0");
                if (ex.Number == 50302)
                    throw new PaymentNotFoundException("Reservation not found");
                if (ex.Number == 50303)
                    throw new InvalidPaymentException("Payment already completed for this reservation");

                throw new PaymentProcessingException("Error creating payment", ex);
            }
        }

        public Payment GetById(Guid id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetPaymentById", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PaymentId", id);

                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapPaymentFromReader(reader);
                            }
                        }
                    }
                }
                return null;
            }
            catch (SqlException ex)
            {
                throw new PaymentProcessingException($"Error retrieving payment with ID {id}", ex);
            }
        }

        public Payment GetByReservationId(Guid reservationId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetPaymentByReservationId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@ReservationId", reservationId);

                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapPaymentFromReader(reader);
                            }
                        }
                    }
                }
                return null;
            }
            catch (SqlException ex)
            {
                throw new PaymentProcessingException($"Error retrieving payment for reservation {reservationId}", ex);
            }
        }

        public List<Payment> GetByUserId(Guid userId)
        {
            var payments = new List<Payment>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GetPaymentsByUserId", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@UserId", userId);

                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                payments.Add(MapPaymentFromReader(reader));
                            }
                        }
                    }
                }
                return payments;
            }
            catch (SqlException ex)
            {
                throw new PaymentProcessingException($"Error retrieving payments for user {userId}", ex);
            }
        }

        public void UpdateStatus(Guid id, PaymentStatus status)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_UpdatePaymentStatus", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PaymentId", id);
                        command.Parameters.AddWithValue("@PaymentStatus", status.ToString());

                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50304)
                    throw new InvalidPaymentException("Invalid payment status");
                if (ex.Number == 50305)
                    throw new PaymentNotFoundException(id);

                throw new PaymentProcessingException($"Error updating payment status for ID {id}", ex);
            }
        }

        private Payment MapPaymentFromReader(SqlDataReader reader)
        {
            var payment = new Payment
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                ReservationId = reader.GetGuid(reader.GetOrdinal("ReservationId")),
                Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                PaymentStatus = Enum.Parse<PaymentStatus>(reader.GetString(reader.GetOrdinal("PaymentStatus"))),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };

            return payment;
        }
    }
}