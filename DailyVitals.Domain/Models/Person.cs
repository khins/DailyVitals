namespace DailyVitals.Domain.Models
{
    public class Person
    {
        public long PersonId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}
