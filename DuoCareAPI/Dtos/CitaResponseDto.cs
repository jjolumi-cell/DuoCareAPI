using System;
using System.Collections.Generic;
using System.Text;

namespace DuoCare.Dtos
{
    public class CitaResponseDto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public string EmisorId { get; set; }
        public string ReceptorId { get; set; }
    }

}
