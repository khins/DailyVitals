using System;

namespace DailyVitals.Domain.Models
{
    public class Person
    {
        public long PersonId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public decimal? HeightFt { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}
