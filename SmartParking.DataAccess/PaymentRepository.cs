using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient; 
using SmartParking.DataAccess.Repositories;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;

namespace SmartParking.DataAccess
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly string _connectionString;

        public PaymentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Guid Create(Payment payment)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_CreatePayment", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@ReservationId", payment.ReservationId);
                cmd.Parameters.AddWithValue("@Amount", payment.Amount);

                var outputId = new SqlParameter("@PaymentId", SqlDbType.UniqueIdentifier)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(outputId);

                conn.Open();
                cmd.ExecuteNonQuery();

                return (Guid)outputId.Value;
            }
        }

        public Payment GetById(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetPaymentById", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PaymentId", id);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapReaderToPayment(reader);
                    }
                }
            }
            return null;
        }

        public Payment GetByReservationId(Guid reservationId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetPaymentByReservationId", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ReservationId", reservationId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapReaderToPayment(reader);
                    }
                }
            }
            return null;
        }

        public List<Payment> GetByUserId(Guid userId)
        {
            var payments = new List<Payment>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetPaymentsByUserId", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        payments.Add(MapReaderToPayment(reader));
                    }
                }
            }

            return payments;
        }

        public void UpdateStatus(Guid id, PaymentStatus status)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_UpdatePaymentStatus", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PaymentId", id);
                cmd.Parameters.AddWithValue("@PaymentStatus", status.ToString());

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private Payment MapReaderToPayment(SqlDataReader reader)
        {
            return new Payment
            {
                Id = (Guid)reader["Id"],
                ReservationId = (Guid)reader["ReservationId"],
                Amount = (decimal)reader["Amount"],
                PaymentStatus = (PaymentStatus)Enum.Parse(typeof(PaymentStatus), reader["PaymentStatus"].ToString()),
                CreatedAt = (DateTime)reader["CreatedAt"]
            };
        }
    }
}