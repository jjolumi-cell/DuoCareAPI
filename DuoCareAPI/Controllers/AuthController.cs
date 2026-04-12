using DuoCareAPI.Dtos;
using DuoCareAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.RateLimiting;

namespace DuoCareAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly JwtService _jwtService;
        private readonly EmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _config;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            JwtService jwtService,
            EmailService emailService,
            ILogger<AuthController> logger,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _emailService = emailService;
            _logger = logger;
            _config = config;
        }

        //Registra nuevo usuario
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            try
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

                _logger.LogInformation("Usuario registrado: {Email}", user.Email);

                await _userManager.AddToRoleAsync(user, "User");

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                var confirmationUrl = $"{_config["App:BaseUrl"]}/api/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar usuario");
                return StatusCode(500, "Error al registrar el usuario. Intenta más tarde.");
            }
        }

        //Confirmacion del correo para nuevos usuarios
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return BadRequest("Usuario no valido.");

                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (!result.Succeeded)
                    return BadRequest("Token de confirmación no válido o expirado.");

                _logger.LogInformation("Correo confirmado: {Email}", user.Email);

                return Ok("Correo confirmado correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al confirmar email para usuario {UserId}", userId);
                return StatusCode(500, "Error al confirmar el email. Intenta más tarde.");
            }
        }

        //Endpoint para loguear
        [HttpPost("login")]
        [EnableRateLimiting("LoginRateLimit")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                {
                    _logger.LogWarning("Intento de login fallido: usuario no encontrado");
                    return Unauthorized("Credenciales incorrectas.");
                }

                if (!user.EmailConfirmed)
                    return Unauthorized("Debes confirmar tu correo antes de iniciar sesión.");

                var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Intento de login fallido: contraseña incorrecta");
                    return Unauthorized("Credenciales incorrectas.");
                }

                var token = await _jwtService.GenerateToken(user);

                _logger.LogInformation("Login exitoso para usuario {UserId}", user.Id);

                return Ok(new
                {
                    token,
                    userId = user.Id,
                    email = user.Email,
                    expires = DateTime.Now.AddHours(2)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al hacer login");
                return StatusCode(500, "Error al iniciar sesión. Intenta más tarde.");
            }
        }

        //Solicitamos enlace que permit cambiar el pasword
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(dto.Email);

                if (user == null)
                    return Ok("Si el correo existe, se enviará un enlace para restablecer la contraseña.");

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var resetUrl =
                    $"https://tuservidor.com/reset-password?userId={user.Id}&token={Uri.EscapeDataString(token)}";

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Restablecer contraseña",
                    $"Hola {user.FullName},<br><br>" +
                    $"Haz clic en el siguiente enlace para restablecer tu contraseña:<br>" +
                    $"<a href='{resetUrl}'>Restablecer contraseña</a><br><br>" +
                    $"Si no solicitaste este cambio, puedes ignorar este mensaje."
                );

                _logger.LogInformation("Reset de contraseña solicitado: {Email}", dto.Email);

                return Ok("Si el correo existe, se enviará un enlace para restablecer la contraseña.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar forgot-password para {Email}", dto.Email);
                return StatusCode(500, "Error al procesar la solicitud. Intenta más tarde.");
            }
        }

        //reseteamos password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                    return BadRequest("Usuario no valido.");

                var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
                if (!result.Succeeded)
                    return BadRequest("El token no es válido o ha expirado.");

                _logger.LogInformation("Contraseña restablecida: {UserId}", dto.UserId);

                return Ok("Contraseña restablecida correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resetear contraseña para usuario {UserId}", dto.UserId);
                return StatusCode(500, "Error al restablecer la contraseña. Intenta más tarde.");
            }
        }

        //Logout
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                return Ok("Sesión cerrada correctamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar sesión");
                return StatusCode(500, "Error al cerrar sesión. Intenta más tarde.");
            }
        }

        //Cambiamos el rol de un usuario a admin, SOLO admin puede hacerlo
        [Authorize(Roles = "Administrator")]
        [HttpPost("make-admin/{id}")]
        public async Task<IActionResult> MakeAdmin(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound("Usuario no encontrado.");

                await _userManager.AddToRoleAsync(user, "Administrator");

                _logger.LogInformation("Usuario promovido a administrador: {UserId}", id);

                return Ok("El usuario ahora es administrador.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al promover usuario {UserId} a administrador", id);
                return StatusCode(500, "Error al promover el usuario. Intenta más tarde.");
            }
        }

        //Obtenemos informacion del usuario
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            try
            {
                var userId = User.FindFirst("uid")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Acceso no autorizado a /me");
                    return Unauthorized();
                }

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                    return NotFound("Usuario no encontrado.");

                return Ok(new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    roles = await _userManager.GetRolesAsync(user)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario autenticado");
                return StatusCode(500, "Error al obtener tu información. Intenta más tarde.");
            }
        }
    }
}
