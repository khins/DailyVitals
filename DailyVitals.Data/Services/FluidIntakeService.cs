using DailyVitals.Data.Configuration;
using DailyVitals.Domain.Models;
using Npgsql;
using System;
using System.Collections.Generic;

namespace DailyVitals.Data.Services
{
    public class FluidIntakeService
    {
        public long Insert(
            long personId,
            DateTime consumedAt,
            int fluidMl,
            decimal enteredAmount,
            string enteredUnit,
            string beverageName,
            string? notes,
            string enteredBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            const string insertSql = @"
                INSERT INTO public.fluid_intake (
                    person_id,
                    consumed_at,
                    fluid_ml,
                    entered_amount,
                    entered_unit,
                    beverage_name,
                    notes
                )
                VALUES (
                    @person_id,
                    @consumed_at,
                    @fluid_ml,
                    @entered_amount,
                    @entered_unit,
                    TRIM(@beverage_name),
                    @notes
                )
                RETURNING fluid_intake_id;";

            using var insertCommand = new NpgsqlCommand(insertSql, conn, transaction);
            AddCommonParameters(insertCommand, personId, consumedAt, fluidMl, enteredAmount, enteredUnit, beverageName, notes);

            var result = insertCommand.ExecuteScalar();
            if (result is null or DBNull)
                throw new InvalidOperationException("Fluid intake insert failed. No ID returned.");

            var fluidIntakeId = Convert.ToInt64(result);

            const string logSql = @"
                INSERT INTO public.data_entry_log (
                    table_name,
                    record_id,
                    action_type,
                    entered_by,
                    change_details
                )
                VALUES (
                    'fluid_intake',
                    @fluid_intake_id,
                    'INSERT',
                    @entered_by,
                    jsonb_build_object(
                        'consumed_at', @consumed_at,
                        'fluid_ml', @fluid_ml,
                        'entered_amount', @entered_amount,
                        'entered_unit', @entered_unit,
                        'beverage_name', @beverage_name,
                        'notes', @notes
                    )
                );";

            using var logCommand = new NpgsqlCommand(logSql, conn, transaction);
            AddCommonParameters(logCommand, personId, consumedAt, fluidMl, enteredAmount, enteredUnit, beverageName, notes);
            logCommand.Parameters.AddWithValue("fluid_intake_id", fluidIntakeId);
            logCommand.Parameters.AddWithValue("entered_by", enteredBy);
            logCommand.ExecuteNonQuery();

            transaction.Commit();
            return fluidIntakeId;
        }

        public bool UpdateForPerson(
            long fluidIntakeId,
            long personId,
            DateTime consumedAt,
            int fluidMl,
            decimal enteredAmount,
            string enteredUnit,
            string beverageName,
            string? notes,
            string updatedBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                WITH updated AS (
                    UPDATE public.fluid_intake
                    SET
                        consumed_at = @consumed_at,
                        fluid_ml = @fluid_ml,
                        entered_amount = @entered_amount,
                        entered_unit = @entered_unit,
                        beverage_name = TRIM(@beverage_name),
                        notes = @notes,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE fluid_intake_id = @fluid_intake_id
                      AND person_id = @person_id
                    RETURNING fluid_intake_id
                ),
                logged AS (
                    INSERT INTO public.data_entry_log (
                        table_name,
                        record_id,
                        action_type,
                        entered_by,
                        change_details
                    )
                    SELECT
                        'fluid_intake',
                        fluid_intake_id,
                        'UPDATE',
                        @updated_by,
                        jsonb_build_object(
                            'consumed_at', @consumed_at,
                            'fluid_ml', @fluid_ml,
                            'entered_amount', @entered_amount,
                            'entered_unit', @entered_unit,
                            'beverage_name', @beverage_name,
                            'notes', @notes
                        )
                    FROM updated
                    RETURNING 1
                )
                SELECT fluid_intake_id
                FROM updated;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("fluid_intake_id", fluidIntakeId);
            AddCommonParameters(cmd, personId, consumedAt, fluidMl, enteredAmount, enteredUnit, beverageName, notes);
            cmd.Parameters.AddWithValue("updated_by", updatedBy);

            return cmd.ExecuteScalar() is not null and not DBNull;
        }

        public List<FluidIntake> GetHistory(long personId)
        {
            var list = new List<FluidIntake>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                SELECT
                    fluid_intake_id,
                    person_id,
                    consumed_at::timestamp,
                    fluid_ml,
                    entered_amount,
                    entered_unit,
                    beverage_name,
                    notes,
                    created_at,
                    updated_at
                FROM public.fluid_intake
                WHERE person_id = @person_id
                ORDER BY consumed_at DESC, fluid_intake_id DESC;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new FluidIntake
                {
                    FluidIntakeId = reader.GetInt64(0),
                    PersonId = reader.GetInt64(1),
                    ConsumedAt = reader.GetDateTime(2),
                    FluidMl = reader.GetInt32(3),
                    EnteredAmount = reader.GetDecimal(4),
                    EnteredUnit = reader.GetString(5),
                    BeverageName = reader.GetString(6),
                    Notes = reader.IsDBNull(7) ? null : reader.GetString(7),
                    CreatedAt = reader.GetDateTime(8),
                    UpdatedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9)
                });
            }

            return list;
        }

        public bool DeleteForPerson(long personId, long fluidIntakeId)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                DELETE FROM public.fluid_intake
                WHERE person_id = @person_id
                  AND fluid_intake_id = @fluid_intake_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("fluid_intake_id", fluidIntakeId);

            return cmd.ExecuteNonQuery() == 1;
        }

        private static void AddCommonParameters(
            NpgsqlCommand cmd,
            long personId,
            DateTime consumedAt,
            int fluidMl,
            decimal enteredAmount,
            string enteredUnit,
            string beverageName,
            string? notes)
        {
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("consumed_at", consumedAt);
            cmd.Parameters.AddWithValue("fluid_ml", fluidMl);
            cmd.Parameters.AddWithValue("entered_amount", enteredAmount);
            cmd.Parameters.AddWithValue("entered_unit", enteredUnit);
            cmd.Parameters.AddWithValue("beverage_name", beverageName);
            cmd.Parameters.AddWithValue("notes", (object?)notes ?? DBNull.Value);
        }

    }
}
