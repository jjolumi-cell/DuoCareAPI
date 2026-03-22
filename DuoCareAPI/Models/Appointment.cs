using System;

namespace DuoCare.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public string Status { get; set; } = "Pending";
        // initialized like this

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string SenderId { get; set; }
        public User Sender { get; set; }

        public string ReceiverId { get; set; }
        public User Receiver { get; set; }
    }
}
