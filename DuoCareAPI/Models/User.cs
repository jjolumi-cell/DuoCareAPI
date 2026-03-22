using Microsoft.AspNetCore.Identity;

namespace DuoCare.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }
    }
}
