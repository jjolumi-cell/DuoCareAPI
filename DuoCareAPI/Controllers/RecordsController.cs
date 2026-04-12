using DuoCareAPI.Data;
using DuoCareAPI.Dtos;
using DuoCareAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DuoCareAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecordsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecordsController> _logger;

        public RecordsController(ApplicationDbContext context, ILogger<RecordsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Crea un regisrtro medico del usuario(niño o animal)
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(RecordDto dto)
        {
            var userId = User.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Acceso no autorizado al crear registro");
                return Unauthorized();
            }

            try
            {
                var record = new Record
                {
                    Name = dto.Name,
                    Type = dto.Type,
                    Medication = dto.Medication,
                    MedicalData = dto.MedicalData,
                    Notes = dto.Notes,
                    UserId = userId 
                };

                _context.Records.Add(record);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Registro médico creado: {RecordId} para usuario {UserId}", record.Id, userId);

                return Ok(record);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al crear registro para usuario {UserId}", userId);
                return StatusCode(500, "Error al crear el registro. Intenta más tarde.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear registro para usuario {UserId}", userId);
                return StatusCode(500, "Error inesperado. Intenta más tarde.");
            }
        }

        //Obtiene los registros de un usuario, SOLO usuario o admin por motivos importantes
        [Authorize]
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetByUser(string id)
        {
            // Extract the authenticated user ID from the JWT
            var loggedUserId = User.FindFirst("uid")?.Value;

            // If the user is NOT admin and NOT the owner → deny access
            if (!User.IsInRole("Administrator") && loggedUserId != id)
            {
                _logger.LogWarning("Acceso denegado a registros del usuario {UserId} por usuario {RequestedBy}", id, loggedUserId);
                return Forbid("No tienes permiso para ver este perfil.");
            }

            try
            {
                var record = await _context.Records
                    .FirstOrDefaultAsync(r => r.UserId == id);

                _logger.LogInformation("Registros obtenidos para usuario {UserId} por {RequestedBy}", id, loggedUserId);

                return Ok(record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener registros del usuario {UserId}", id);
                return StatusCode(500, "Error al obtener los registros. Intenta más tarde.");
            }
        }
    }
}

