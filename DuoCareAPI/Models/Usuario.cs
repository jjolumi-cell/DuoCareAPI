using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace DuoCare.Models
{
    public class Usuario : IdentityUser
    {
        public string NombreCompleto { get; set; }
    }

}
