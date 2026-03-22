using DuoCare.Dtos;
using DuoCare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DuoCare.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly JwtService _jwtService;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            JwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
        }

        // Standard registration: all users get the "User" role by default
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest("Passwords do not match.");

            var user = new User
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName
            };
            // -------IMPORTANT-------IMPORTANTE-----------
            // ASP.NET Identity automatically hashes the password before storing it.
            // The password is NEVER saved in plain text.
            // Identity generates a unique salt, applies the PBKDF2 hashing algorithm,
            // and stores only the resulting hash in the database.
            // This ensures strong security and prevents password recovery even by administrators.
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Assign default role
            await _userManager.AddToRoleAsync(user, "User");

            return Ok("User registered successfully.");
        }

        // Login: generates JWT including roles
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized();

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded) return Unauthorized();

            var token = await _jwtService.GenerateToken(user);

            return Ok(new
            {
                token,
                userId = user.Id,
                email = user.Email,
                expires = DateTime.Now.AddHours(2)
            });
        }

        // ------------IMPORTANT---------IMPORTANTE-------------------
        // MAKE ADMIN: Only an Administrator can use this endpoint
        // This allows having a reasonable number of admins
        // ---------------------------------------------------------
        [Authorize(Roles = "Administrator")]
        [HttpPost("make-admin/{id}")]
        public async Task<IActionResult> MakeAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound("User not found.");

            await _userManager.AddToRoleAsync(user, "Administrator");

            return Ok("User is now an administrator.");
        }
    }
}
