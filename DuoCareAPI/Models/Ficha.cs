using System;
using System.Collections.Generic;
using System.Text;

namespace DuoCare.Models
{
    public class Ficha
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; } // Que sera hijo o mascota
        public string Medicacion { get; set; }
        public string DatosMedicos { get; set; }
        public string Notas { get; set; }

        public string UsuarioId { get; set; }
        public Usuario Usuario { get; set; }
    }


}
