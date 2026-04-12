using DuoCareAPI.Data;
using DuoCareAPI.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DuoCareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Busca un usuario por su email y devuelve sus datos básicos
        [HttpGet("find")]
        public async Task<ActionResult<UserListDto>> FindByEmail([FromQuery] string email)
        {
            var requesterId = User.FindFirst("uid")?.Value;

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Búsqueda de usuario con email vacío por usuario {UserId}", requesterId);
                return BadRequest("El email es requerido");
            }

            if (!email.Contains("@"))
            {
                _logger.LogWarning("Formato de email inválido por usuario {UserId}", requesterId);
                return BadRequest("El formato del email es inválido");
            }

            email = email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null || user.Id == requesterId)
            {
                _logger.LogWarning("Usuario no encontrado o intento de buscarse a sí mismo por usuario {UserId}", requesterId);
                return NotFound("Usuario no encontrado");
            }

            var userDto = new UserListDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email
            };

            _logger.LogInformation("Usuario encontrado por usuario {UserId}", requesterId);

            return Ok(userDto);
        }
    }
}