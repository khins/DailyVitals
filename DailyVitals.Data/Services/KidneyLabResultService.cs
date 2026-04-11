using DailyVitals.Data.Configuration;
using DailyVitals.Domain.Models;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;

namespace DailyVitals.Data.Services
{
    public class KidneyLabResultService
    {
        public long Insert(KidneyLabResult result, string enteredBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT sp_insert_kidney_lab_result(" +
                "@p_person_id, @p_result_month, @p_albumin, @p_npcr, @p_potassium, @p_wktv, " +
                "@p_calcium, @p_phosphorus, @p_ipth, @p_hemoglobin, @p_glucose, @p_cholesterol, " +
                "@p_triglycerides, @p_bun, @p_creatinine, @p_notes, @p_entered_by)",
                conn);

            AddParameters(cmd, result, enteredBy);

            var id = cmd.ExecuteScalar();
            if (id is null or DBNull)
                throw new Exception("Kidney lab result insert failed. No ID returned.");

            return Convert.ToInt64(id);
        }

        public void Update(KidneyLabResult result, string enteredBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT sp_update_kidney_lab_result(" +
                "@p_kidney_lab_result_id, @p_result_month, @p_albumin, @p_npcr, @p_potassium, @p_wktv, " +
                "@p_calcium, @p_phosphorus, @p_ipth, @p_hemoglobin, @p_glucose, @p_cholesterol, " +
                "@p_triglycerides, @p_bun, @p_creatinine, @p_notes, @p_entered_by)",
                conn);

            cmd.Parameters.AddWithValue("p_kidney_lab_result_id", result.KidneyLabResultId);
            AddValueParameters(cmd, result);
            cmd.Parameters.AddWithValue("p_entered_by", enteredBy);
            cmd.ExecuteNonQuery();
        }

        public void Delete(long kidneyLabResultId, string enteredBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT sp_delete_kidney_lab_result(@p_kidney_lab_result_id, @p_entered_by)",
                conn);

            cmd.Parameters.AddWithValue("p_kidney_lab_result_id", kidneyLabResultId);
            cmd.Parameters.AddWithValue("p_entered_by", enteredBy);
            cmd.ExecuteNonQuery();
        }

        public List<KidneyLabResult> GetHistory(long personId)
        {
            var list = new List<KidneyLabResult>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                SELECT kidney_lab_result_id,
                       person_id,
                       result_month,
                       albumin,
                       npcr,
                       potassium,
                       wktv,
                       calcium,
                       phosphorus,
                       ipth,
                       hemoglobin,
                       glucose,
                       cholesterol,
                       triglycerides,
                       bun,
                       creatinine,
                       notes
                FROM kidney_lab_result
                WHERE person_id = @person_id
                ORDER BY result_month DESC;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new KidneyLabResult
                {
                    KidneyLabResultId = reader.GetInt64(0),
                    PersonId = reader.GetInt64(1),
                    ResultMonth = reader.GetDateTime(2),
                    Albumin = reader.GetDecimal(3),
                    NPCR = reader.GetDecimal(4),
                    Potassium = reader.GetDecimal(5),
                    WKtV = reader.GetDecimal(6),
                    Calcium = reader.GetDecimal(7),
                    Phosphorus = reader.GetDecimal(8),
                    IPTH = reader.GetDecimal(9),
                    Hemoglobin = reader.GetDecimal(10),
                    Glucose = reader.GetDecimal(11),
                    Cholesterol = reader.GetDecimal(12),
                    Triglycerides = reader.GetDecimal(13),
                    BUN = reader.GetDecimal(14),
                    Creatinine = reader.GetDecimal(15),
                    Notes = reader.IsDBNull(16) ? null : reader.GetString(16)
                });
            }

            return list;
        }

        private static void AddParameters(NpgsqlCommand cmd, KidneyLabResult result, string enteredBy)
        {
            cmd.Parameters.AddWithValue("p_person_id", result.PersonId);
            AddValueParameters(cmd, result);
            cmd.Parameters.AddWithValue("p_entered_by", enteredBy);
        }

        private static void AddValueParameters(NpgsqlCommand cmd, KidneyLabResult result)
        {
            cmd.Parameters.Add(
                new NpgsqlParameter("p_result_month", NpgsqlDbType.Date)
                {
                    Value = NormalizeMonth(result.ResultMonth).Date
                });
            cmd.Parameters.AddWithValue("p_albumin", result.Albumin);
            cmd.Parameters.AddWithValue("p_npcr", result.NPCR);
            cmd.Parameters.AddWithValue("p_potassium", result.Potassium);
            cmd.Parameters.AddWithValue("p_wktv", result.WKtV);
            cmd.Parameters.AddWithValue("p_calcium", result.Calcium);
            cmd.Parameters.AddWithValue("p_phosphorus", result.Phosphorus);
            cmd.Parameters.AddWithValue("p_ipth", result.IPTH);
            cmd.Parameters.AddWithValue("p_hemoglobin", result.Hemoglobin);
            cmd.Parameters.AddWithValue("p_glucose", result.Glucose);
            cmd.Parameters.AddWithValue("p_cholesterol", result.Cholesterol);
            cmd.Parameters.AddWithValue("p_triglycerides", result.Triglycerides);
            cmd.Parameters.AddWithValue("p_bun", result.BUN);
            cmd.Parameters.AddWithValue("p_creatinine", result.Creatinine);
            cmd.Parameters.AddWithValue("p_notes", (object?)result.Notes ?? DBNull.Value);
        }

        private static DateTime NormalizeMonth(DateTime value) =>
            new(value.Year, value.Month, 1);
    }
}
