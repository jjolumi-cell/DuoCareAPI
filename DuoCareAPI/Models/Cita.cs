using System;
using System.Collections.Generic;
using System.Text;

namespace DuoCare.Models
{
    public class Cita
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = "Pendiente"; //inicializamos asi

        public double Latitud { get; set; }
        public double Longitud { get; set; }

        public string EmisorId { get; set; }
        public Usuario Emisor { get; set; }

        public string ReceptorId { get; set; }
        public Usuario Receptor { get; set; }
    }

}
