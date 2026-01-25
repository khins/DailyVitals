using DailyVitals.Data.Configuration;
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

    }

}
