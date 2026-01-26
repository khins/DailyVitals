using DailyVitals.Data.Configuration;
using DailyVitals.Domain.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DailyVitals.Data.Services
{
    public class MedicationService
    {
        public void InsertMedication(
            long personId,
            string medicationName,
            decimal dosageMg,
            string dosageForm,
            bool takeMorning,
            bool takeNoon,
            bool takeEvening,
            bool takeBedtime,
            string instructions,
            string prescribedBy,
            DateTime? startDate,
            DateTime? endDate)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "CALL sp_insert_medication(" +
                "@p_person_id, " +
                "@p_medication_name, " +
                "@p_dosage_mg, " +
                "@p_dosage_form, " +
                "@p_take_morning, " +
                "@p_take_noon, " +
                "@p_take_evening, " +
                "@p_take_bedtime, " +
                "@p_instructions, " +
                "@p_prescribed_by, " +
                "@p_start_date, " +
                "@p_end_date)", conn);

            cmd.Parameters.AddWithValue("p_person_id", personId);
            cmd.Parameters.AddWithValue("p_medication_name", medicationName);
            cmd.Parameters.AddWithValue("p_dosage_mg", dosageMg);
            cmd.Parameters.AddWithValue("p_dosage_form", (object?)dosageForm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_take_morning", takeMorning);
            cmd.Parameters.AddWithValue("p_take_noon", takeNoon);
            cmd.Parameters.AddWithValue("p_take_evening", takeEvening);
            cmd.Parameters.AddWithValue("p_take_bedtime", takeBedtime);
            cmd.Parameters.AddWithValue("p_instructions", (object?)instructions ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_prescribed_by", (object?)prescribedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_start_date", (object?)startDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_end_date", (object?)endDate ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public List<Medication> GetMedications(long personId)
        {
            var list = new List<Medication>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                    SELECT medication_id,
                           medication_name,
                           dosage_mg,
                           dosage_form,
                           take_morning,
                           take_noon,
                           take_evening,
                           take_bedtime,
                           instructions,
                           prescribed_by,
                           is_active
                    FROM medication
                    WHERE person_id = @person_id
                    ORDER BY medication_name;
                ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Medication
                {
                    MedicationId = reader.GetInt64(0),
                    MedicationName = reader.GetString(1),
                    DosageMg = reader.GetDecimal(2),
                    DosageForm = reader.GetString(3),
                    TakeMorning = reader.GetBoolean(4),
                    TakeNoon = reader.GetBoolean(5),
                    TakeEvening = reader.GetBoolean(6),
                    TakeBedtime = reader.GetBoolean(7),
                    Instructions = reader.IsDBNull(8) ? null : reader.GetString(8),
                    PrescribedBy = reader.IsDBNull(9) ? null : reader.GetString(9),
                    IsActive = reader.GetBoolean(10)
                });
            }

            return list;
        }

        public void DeactivateMedication(long medicationId, string enteredBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "CALL sp_deactivate_medication(@p_medication_id, @p_entered_by)", conn);

            cmd.Parameters.AddWithValue("p_medication_id", medicationId);
            cmd.Parameters.AddWithValue("p_entered_by", enteredBy);

            cmd.ExecuteNonQuery();
        }

        public void UpdateMedication(
                    long medicationId,
                    decimal dosageMg,
                    string dosageForm,
                    bool takeMorning,
                    bool takeNoon,
                    bool takeEvening,
                    bool takeBedtime,
                    string instructions,
                    string prescribedBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "CALL sp_update_medication(" +
                "@p_medication_id, " +
                "@p_dosage_mg, " +
                "@p_dosage_form, " +
                "@p_take_morning, " +
                "@p_take_noon, " +
                "@p_take_evening, " +
                "@p_take_bedtime, " +
                "@p_instructions, " +
                "@p_prescribed_by)", conn);

            cmd.Parameters.AddWithValue("p_medication_id", medicationId);
            cmd.Parameters.AddWithValue("p_dosage_mg", dosageMg);
            cmd.Parameters.AddWithValue("p_dosage_form", (object?)dosageForm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_take_morning", takeMorning);
            cmd.Parameters.AddWithValue("p_take_noon", takeNoon);
            cmd.Parameters.AddWithValue("p_take_evening", takeEvening);
            cmd.Parameters.AddWithValue("p_take_bedtime", takeBedtime);
            cmd.Parameters.AddWithValue("p_instructions", (object?)instructions ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_prescribed_by", (object?)prescribedBy ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }


    }

}
