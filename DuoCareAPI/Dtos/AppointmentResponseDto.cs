using System;

namespace DuoCareAPI.Dtos
{
    public class AppointmentResponseDto
    {
        public int Id { get; set; }

        public DateTime Date { get; set; }

        public string Status { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string SenderId { get; set; }

        public string ReceiverId { get; set; }
    }
}
