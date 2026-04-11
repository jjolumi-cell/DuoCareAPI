using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DuoCareAPI.Data;
using DuoCareAPI.Dtos;
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

            _logger.LogInformation("Usuario {UserId} accede a FindByEmail", requesterId);

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Búsqueda de usuario con email vacío por {UserId}", requesterId);
                return BadRequest("El email es requerido");
            }

            email = email.Trim().ToLower();
            _logger.LogInformation("Buscando usuario con email normalizado: {Email}", email);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                _logger.LogWarning("Usuario no encontrado con email: {Email}", email);
                return NotFound("Usuario no encontrado");
            }
            _logger.LogInformation("Usuario encontrado con email: {Email}", email);

            var userDto = new UserListDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email
            };
            return Ok(userDto);
        }
    }
}
