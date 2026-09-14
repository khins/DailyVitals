namespace DailyVitals.Domain.Models;

public class SavedMeal
{
    public long SavedMealId { get; set; }
    public string Name { get; set; } = "";
    public string MealType { get; set; } = "Breakfast";
    public string FoodsJson { get; set; } = "[]";
}
