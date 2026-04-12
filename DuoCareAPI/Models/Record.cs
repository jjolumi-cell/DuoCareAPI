using System;

namespace DuoCareAPI.Models
{
    public class Record
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }
        // Will be "child" or "pet"

        public string Medication { get; set; }

        public string MedicalData { get; set; }

        public string Notes { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
