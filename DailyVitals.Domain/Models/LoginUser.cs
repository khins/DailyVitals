using System;

namespace DailyVitals.Domain.Models
{
    public class LoginUser
    {
        public long LoginUserId { get; set; }
        public long? PersonId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
