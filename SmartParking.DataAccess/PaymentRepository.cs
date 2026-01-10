using System;
using System.Data;
using System.Data.SqlClient;
using SmartParking.Domain.Entities;
using SmartParking.Domain.Enums;
using SmartParking.DataAccess.Repositories;

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

                // Input Parameters
                cmd.Parameters.AddWithValue("@ReservationId", payment.ReservationId);
                cmd.Parameters.AddWithValue("@Amount", payment.Amount);

                // Output Parameter
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
            return null;
        }

        public Payment GetByReservationId(Guid reservationId)
        {
            throw new NotImplementedException();
        }
    }
}