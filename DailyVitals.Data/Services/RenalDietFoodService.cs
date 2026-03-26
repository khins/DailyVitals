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
        public List<RenalMealCombo> GetWeightLossMealCombos(long personId, int maxItems = 2)
        {
            var foods = GetRankedFoods(personId, 12);
            var combos = BuildMealCombos(foods);

            return combos
                .Take(maxItems)
                .ToList();
        }

        private List<RenalDietFood> GetRankedFoods(long personId, int maxItems)
        {
            var list = new List<RenalDietFood>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                SELECT
                    rdf.renal_food_id,
                    rdf.person_id,
                    rdf.food_name,
                    rdf.serving_size,
                    rdf.calories,
                    rdf.sodium_mg,
                    rdf.potassium_mg,
                    rdf.phosphorus_mg,
                    rdf.protein_g,
                    rdf.allowed,
                    rdf.restriction_notes,
                    rfc.category_name
                FROM renal_diet_food rdf
                LEFT JOIN renal_food_category rfc
                    ON rfc.category_id = rdf.category_id
                WHERE rdf.person_id = @person_id
                  AND rdf.allowed = TRUE
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
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("limit", maxItems);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new RenalDietFood
                {
                    RenalFoodId = reader.GetInt64(0),
                    PersonId = reader.GetInt64(1),
                    FoodName = reader.GetString(2),
                    ServingSize = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Calories = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    SodiumMg = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    PotassiumMg = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    PhosphorusMg = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    ProteinG = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
                    Allowed = reader.GetBoolean(9),
                    RestrictionNotes = reader.IsDBNull(10) ? null : reader.GetString(10),
                    CategoryName = reader.IsDBNull(11) ? null : reader.GetString(11)
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
