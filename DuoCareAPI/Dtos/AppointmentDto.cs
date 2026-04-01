using System;

namespace DuoCare.Dtos
{
    public class AppointmentDto
    {
        public DateTime Date { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string ReceiverId { get; set; }
    }
}
