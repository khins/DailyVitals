using DailyVitals.Data.Configuration;
using DailyVitals.Domain.Models;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DailyVitals.Data.Services;

public class SavedMealService
{
    public List<SavedMeal> GetMeals(long personId)
    {
        using var conn = DbConnectionFactory.Create();
        conn.Open();
        using var cmd = new NpgsqlCommand("SELECT saved_meal_id, name, meal_type, foods_json::text FROM public.saved_meal WHERE person_id = @person ORDER BY name", conn);
        cmd.Parameters.AddWithValue("person", personId);
        using var reader = cmd.ExecuteReader();
        var meals = new List<SavedMeal>();
        while (reader.Read()) meals.Add(new SavedMeal { SavedMealId = reader.GetInt64(0), Name = reader.GetString(1), MealType = reader.GetString(2), FoodsJson = reader.GetString(3) });
        return meals;
    }

    public void Save(long personId, string name, string mealType, long[] entryIds)
    {
        name = name.Trim();
        if (name.Length is < 1 or > 100) throw new ArgumentException("Enter a meal name of 1–100 characters.");
        if (mealType is not ("Breakfast" or "Lunch" or "Dinner" or "Snack")) throw new ArgumentException("Choose a meal type.");
        var ids = entryIds.Distinct().ToArray();
        if (ids.Length is < 1 or > 100) throw new ArgumentException("Select between 1 and 100 foods.");
        using var conn = DbConnectionFactory.Create();
        conn.Open();
        // Snapshot only food values; never retain medication counts or diary identity.
        using var cmd = new NpgsqlCommand("""
            INSERT INTO public.saved_meal (person_id, name, meal_type, foods_json)
            SELECT @person, @name, @type, jsonb_agg(jsonb_build_object(
                'food_name', food_name, 'serving_description', serving_description,
                'phosphorus_mg', phosphorus_mg, 'calories', calories, 'sodium_mg', sodium_mg,
                'protein_g', protein_g, 'potassium_mg', potassium_mg, 'fluid_ml', fluid_ml,
                'estimated_by_ai', estimated_by_ai, 'ai_provider', ai_provider,
                'ai_confidence', ai_confidence, 'source_notes', source_notes)
                ORDER BY consumed_at, food_phosphorus_intake_id)
            FROM public.food_phosphorus_intake
            WHERE person_id = @person AND food_phosphorus_intake_id = ANY(@ids)
            HAVING count(*) = @count
            """, conn);
        cmd.Parameters.AddWithValue("person", personId);
        cmd.Parameters.AddWithValue("name", name);
        cmd.Parameters.AddWithValue("type", mealType);
        cmd.Parameters.AddWithValue("ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint, ids);
        cmd.Parameters.AddWithValue("count", ids.Length);
        if (cmd.ExecuteNonQuery() != 1) throw new ArgumentException("Some selected foods are no longer available. Refresh and select them again.");
    }
}
