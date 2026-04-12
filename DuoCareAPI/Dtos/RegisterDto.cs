namespace DuoCareAPI.Dtos
{
    public class RegisterDto
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public string ConfirmPassword { get; set; }
        // Added to confirm password and avoid issues if typed incorrectly

        public string FullName { get; set; }
    }
}
