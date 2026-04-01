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
        private readonly EmailService _emailService;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            JwtService jwtService,
            EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest("Las contraseñas no coinciden.");

            var user = new User
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, "User");

            // Generate email confirmation token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var confirmationUrl =
                $"https://tuservidor.com/api/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

            // Send confirmation email
            await _emailService.SendEmailAsync(
                user.Email,
                "Confirma tu cuenta",
                $"Hola {user.FullName},<br><br>" +
                $"Haz clic en el siguiente enlace para confirmar tu cuenta:<br>" +
                $"<a href='{confirmationUrl}'>Confirmar cuenta</a><br><br>" +
                $"Si no solicitaste esta cuenta, puedes ignorar este mensaje."
            );

            return Ok("Usuario registrado correctamente. Revisa tu correo para confirmar la cuenta.");
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return BadRequest("Usuario no valido.");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
                return BadRequest("Token de confirmación no válido o expirado.");

            return Ok("Correo confirmado correctamente.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized("Credenciales incorrectas.");

            if (!user.EmailConfirmed)
                return Unauthorized("Debes confirmar tu correo antes de iniciar sesión.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded)
                return Unauthorized("Credenciales incorrectas.");

            var token = await _jwtService.GenerateToken(user);

            return Ok(new
            {
                token,
                userId = user.Id,
                email = user.Email,
                expires = DateTime.Now.AddHours(2)
            });
        }


        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            // No revelar si el usuario existe o no
            if (user == null)
                return Ok("Si el correo existe, se enviará un enlace para restablecer la contraseña.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetUrl =
                $"https://tuservidor.com/reset-password?userId={user.Id}&token={Uri.EscapeDataString(token)}";

            // Send reset email
            await _emailService.SendEmailAsync(
                user.Email,
                "Restablecer contraseña",
                $"Hola {user.FullName},<br><br>" +
                $"Haz clic en el siguiente enlace para restablecer tu contraseña:<br>" +
                $"<a href='{resetUrl}'>Restablecer contraseña</a><br><br>" +
                $"Si no solicitaste este cambio, puedes ignorar este mensaje."
            );

            return Ok("Si el correo existe, se enviará un enlace para restablecer la contraseña.");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
                return BadRequest("Usuario no valido.");

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest("El token no es válido o ha expirado.");

            return Ok("Contraseña restablecida correctamente.");
        }


        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok("Sesión cerrada correctamente.");
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost("make-admin/{id}")]
        public async Task<IActionResult> MakeAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound("Usuario no encontrado.");

            await _userManager.AddToRoleAsync(user, "Administrator");

            return Ok("El usuario ahora es administrador.");
        }
    }
}
