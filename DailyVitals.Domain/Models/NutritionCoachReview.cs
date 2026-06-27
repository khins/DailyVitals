using System.Collections.Generic;

namespace DailyVitals.Domain.Models
{
    public class NutritionCoachReview
    {
        public string Headline { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<string> Wins { get; set; } = [];
        public List<string> FocusAreas { get; set; } = [];
        public List<string> SuggestedActions { get; set; } = [];
        public string CareTeamNote { get; set; } = string.Empty;
    }
}
