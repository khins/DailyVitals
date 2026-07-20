using DailyVitals.Data.Configuration;
using DailyVitals.Domain.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DailyVitals.Data.Services
{
    public class RenalDietFoodService
    {
        public List<RenalDietFood> GetFoodCatalog()
        {
            var list = new List<RenalDietFood>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                SELECT
                    rdf.renal_friendly_food_id,
                    rdf.food_name,
                    rdf.serving_description,
                    ROUND(rdf.calories)::int,
                    ROUND(rdf.sodium_mg)::int,
                    ROUND(rdf.potassium_mg)::int,
                    ROUND(rdf.phosphorus_mg)::int,
                    rdf.protein_g,
                    rdf.category,
                    rdf.renal_rating,
                    rdf.guidance_notes,
                    rdf.source_notes,
                    rdf.is_active
                FROM public.renal_friendly_food rdf
                WHERE rdf.is_active = TRUE
                ORDER BY
                    CASE lower(COALESCE(rdf.renal_rating, ''))
                        WHEN 'preferred' THEN 0
                        WHEN 'friendly' THEN 0
                        WHEN 'good' THEN 0
                        WHEN 'limit' THEN 1
                        WHEN 'avoid' THEN 2
                        ELSE 1
                    END,
                    rdf.category,
                    rdf.food_name;";

            using var cmd = new NpgsqlCommand(sql, conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new RenalDietFood
                {
                    RenalFoodId = reader.GetInt64(0),
                    FoodName = reader.GetString(1),
                    ServingSize = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Calories = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    SodiumMg = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    PotassiumMg = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    PhosphorusMg = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    ProteinG = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                    CategoryName = reader.IsDBNull(8) ? null : reader.GetString(8),
                    RenalRating = reader.IsDBNull(9) ? null : reader.GetString(9),
                    GuidanceNotes = reader.IsDBNull(10) ? null : reader.GetString(10),
                    SourceNotes = reader.IsDBNull(11) ? null : reader.GetString(11),
                    IsActive = reader.GetBoolean(12)
                });
            }

            return list;
        }

        public List<RenalMealCombo> GetWeightLossMealCombos(int maxItems = 2)
        {
            var foods = GetRankedFoods(12);
            var combos = BuildMealCombos(foods);

            return combos
                .Take(maxItems)
                .ToList();
        }

        private List<RenalDietFood> GetRankedFoods(int maxItems)
        {
            var list = new List<RenalDietFood>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                SELECT
                    rdf.renal_friendly_food_id,
                    rdf.food_name,
                    rdf.serving_description,
                    ROUND(rdf.calories)::int,
                    ROUND(rdf.sodium_mg)::int,
                    ROUND(rdf.potassium_mg)::int,
                    ROUND(rdf.phosphorus_mg)::int,
                    rdf.protein_g,
                    rdf.category,
                    rdf.renal_rating,
                    rdf.guidance_notes,
                    rdf.source_notes,
                    rdf.is_active
                FROM public.renal_friendly_food rdf
                WHERE rdf.is_active = TRUE
                  AND lower(COALESCE(rdf.renal_rating, '')) NOT IN ('avoid', 'restricted')
                ORDER BY
                    CASE
                        WHEN COALESCE(rdf.protein_g, 0) > 0
                        THEN COALESCE(rdf.protein_g, 0) / GREATEST(COALESCE(rdf.calories, 999999), 1)
                        ELSE 0
                    END DESC,
                    COALESCE(rdf.protein_g, 0) DESC,
                    COALESCE(rdf.calories, 999999),
                    COALESCE(rdf.sodium_mg, 999999),
                    COALESCE(rdf.phosphorus_mg, 999999),
                    COALESCE(rdf.potassium_mg, 999999),
                    rdf.food_name
                LIMIT @limit;
            ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("limit", maxItems);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new RenalDietFood
                {
                    RenalFoodId = reader.GetInt64(0),
                    FoodName = reader.GetString(1),
                    ServingSize = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Calories = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    SodiumMg = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    PotassiumMg = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    PhosphorusMg = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    ProteinG = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                    CategoryName = reader.IsDBNull(8) ? null : reader.GetString(8),
                    RenalRating = reader.IsDBNull(9) ? null : reader.GetString(9),
                    GuidanceNotes = reader.IsDBNull(10) ? null : reader.GetString(10),
                    SourceNotes = reader.IsDBNull(11) ? null : reader.GetString(11),
                    IsActive = reader.GetBoolean(12)
                });
            }

            return list;
        }

        private static List<RenalMealCombo> BuildMealCombos(IReadOnlyList<RenalDietFood> foods)
        {
            var combos = new List<RenalMealCombo>();

            for (int i = 0; i < foods.Count; i++)
            {
                for (int j = i + 1; j < foods.Count; j++)
                {
                    var first = foods[i];
                    var second = foods[j];

                    if (first.RenalFoodId == second.RenalFoodId)
                        continue;

                    var totalCalories = (first.Calories ?? 0) + (second.Calories ?? 0);
                    var totalProtein = (first.ProteinG ?? 0) + (second.ProteinG ?? 0);
                    var totalSodium = (first.SodiumMg ?? 0) + (second.SodiumMg ?? 0);
                    var totalPotassium = (first.PotassiumMg ?? 0) + (second.PotassiumMg ?? 0);
                    var totalPhosphorus = (first.PhosphorusMg ?? 0) + (second.PhosphorusMg ?? 0);
                    var mealStyle = ClassifyMealStyle(first, second);

                    // Keep combos relatively light for a post-exercise weight-loss suggestion.
                    if (totalCalories > 550)
                        continue;

                    combos.Add(new RenalMealCombo
                    {
                        Foods = new[] { first, second },
                        MealStyle = mealStyle,
                        TotalCalories = totalCalories,
                        TotalProteinG = totalProtein,
                        TotalSodiumMg = totalSodium,
                        TotalPotassiumMg = totalPotassium,
                        TotalPhosphorusMg = totalPhosphorus
                    });
                }
            }

            return combos
                .OrderByDescending(combo => GetMealStyleScore(combo.MealStyle))
                .ThenByDescending(combo => combo.TotalCalories > 0
                    ? combo.TotalProteinG / combo.TotalCalories
                    : combo.TotalProteinG)
                .ThenByDescending(combo => combo.TotalProteinG)
                .ThenBy(combo => combo.TotalCalories)
                .ThenBy(combo => combo.TotalSodiumMg)
                .ThenBy(combo => combo.TotalPhosphorusMg)
                .ThenBy(combo => combo.TotalPotassiumMg)
                .ThenByDescending(combo => combo.Foods.Select(food => food.CategoryName).Distinct(StringComparer.OrdinalIgnoreCase).Count())
                .ToList();
        }

        private static int GetMealStyleScore(string mealStyle)
        {
            return mealStyle switch
            {
                "Protein + Vegetable" => 5,
                "Protein + Fruit" => 4,
                "Protein + Whole Grain" => 4,
                "Protein + Dairy Alternative" => 4,
                "Grain + Fruit" => 3,
                "Protein + Snack" => 3,
                "Snack + Fruit" => 3,
                "Protein + Healthy Fat" => 2,
                "Balanced Pair" => 2,
                _ => 1
            };
        }

        private static string ClassifyMealStyle(RenalDietFood first, RenalDietFood second)
        {
            var firstGroup = GetFoodGroup(first);
            var secondGroup = GetFoodGroup(second);

            if (IsPair(firstGroup, secondGroup, "protein", "vegetable"))
                return "Protein + Vegetable";

            if (IsPair(firstGroup, secondGroup, "protein", "fruit"))
                return "Protein + Fruit";

            if (IsPair(firstGroup, secondGroup, "protein", "grain"))
                return "Protein + Whole Grain";

            if (IsPair(firstGroup, secondGroup, "protein", "dairy-alternative"))
                return "Protein + Dairy Alternative";

            if (IsPair(firstGroup, secondGroup, "protein", "snack"))
                return "Protein + Snack";

            if (IsPair(firstGroup, secondGroup, "grain", "fruit"))
                return "Grain + Fruit";

            if (IsPair(firstGroup, secondGroup, "snack", "fruit"))
                return "Snack + Fruit";

            if (IsPair(firstGroup, secondGroup, "protein", "fat-oil"))
                return "Protein + Healthy Fat";

            if (!string.IsNullOrWhiteSpace(firstGroup) &&
                !string.IsNullOrWhiteSpace(secondGroup) &&
                !string.Equals(firstGroup, secondGroup, StringComparison.OrdinalIgnoreCase))
                return "Balanced Pair";

            return "Light Pair";
        }

        private static bool IsPair(string first, string second, string expectedA, string expectedB)
        {
            return (string.Equals(first, expectedA, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(second, expectedB, StringComparison.OrdinalIgnoreCase)) ||
                   (string.Equals(first, expectedB, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(second, expectedA, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetFoodGroup(RenalDietFood food)
        {
            var category = food.CategoryName?.Trim().ToLowerInvariant() ?? string.Empty;
            var searchText = $"{category} {food.FoodName}".Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(searchText))
                return string.Empty;

            switch (category)
            {
                case "fruit":
                    return "fruit";
                case "vegetable":
                    return "vegetable";
                case "protein":
                    return "protein";
                case "grain":
                    return "grain";
                case "snack":
                    return "snack";
                case "dairy alternative":
                    return "dairy-alternative";
                case "fat / oil":
                case "fat/oil":
                case "fat":
                case "oil":
                    return "fat-oil";
            }

            if (ContainsAny(searchText, "protein", "meat", "egg", "fish", "seafood", "chicken",
                "turkey", "tuna", "salmon", "beef", "pork", "tofu", "tempeh"))
                return "protein";

            if (ContainsAny(searchText, "vegetable", "veggie", "salad", "broccoli", "cabbage",
                "cauliflower", "cucumber", "green bean", "lettuce", "pepper", "zucchini", "carrot"))
                return "vegetable";

            if (ContainsAny(searchText, "fruit", "berry", "apple", "grape", "pear", "peach",
                "pineapple", "mandarin", "blueberry", "strawberry"))
                return "fruit";

            if (ContainsAny(searchText, "grain", "bread", "rice", "pasta", "cereal", "oat",
                "cracker", "toast", "tortilla", "quinoa", "barley"))
                return "grain";

            if (ContainsAny(searchText, "dairy alternative", "almond milk", "rice milk",
                "oat milk", "non-dairy", "nondairy"))
                return "dairy-alternative";

            if (ContainsAny(searchText, "fat / oil", "fat/oil", "olive oil", "oil", "butter",
                "margarine", "avocado oil"))
                return "fat-oil";

            if (ContainsAny(searchText, "snack", "side", "bite", "mix"))
                return "snack";

            if (ContainsAny(searchText, "breakfast", "morning"))
                return "breakfast";

            if (ContainsAny(searchText, "lunch", "dinner", "entree", "main"))
                return "meal";

            if (ContainsAny(searchText, "drink", "beverage", "smoothie", "shake"))
                return "drink";

            return food.CategoryName?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        private static bool ContainsAny(string input, params string[] values)
        {
            return values.Any(input.Contains);
        }
    }
}
