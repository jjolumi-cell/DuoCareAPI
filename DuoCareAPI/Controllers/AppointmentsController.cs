using DuoCareAPI.Data;
using DuoCareAPI.Dtos;
using DuoCareAPI.Models;
using DuoCareAPI.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DuoCareAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(ApplicationDbContext context, ILogger<AppointmentsController> logger)
        {
            _context = context;
            _logger = logger;
        }
        //Creamos cita entre usuarios
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AppointmentDto dto)
        {
            var userIdFromToken = User.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(userIdFromToken))
            {
                _logger.LogWarning("Acceso no autorizado al crear cita");
                return Unauthorized();
            }

            if (dto == null)
                return BadRequest("Datos de la cita inválidos.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrEmpty(dto.ReceiverId))
                return BadRequest("Receptor inválido.");

            if (userIdFromToken == dto.ReceiverId)
                return BadRequest("El que envia y el receptor no pueden ser el mismo.");

            if (dto.Date <= DateTime.Now)
                return BadRequest("La cita no puede estar en el pasado.");

            if (dto.Latitude < -90 || dto.Latitude > 90 || dto.Longitude < -180 || dto.Longitude > 180)
                return BadRequest("Coordenadas inválidas.");

            try
            {
                var receiver = await _context.Users.FindAsync(dto.ReceiverId);
                if (receiver == null)
                    return BadRequest("El usuario receptor no existe.");

                var sender = await _context.Users.FindAsync(userIdFromToken);
                if (sender == null)
                    return BadRequest("Usuario autenticado no válido.");

                var conflict = await _context.Appointments.AnyAsync(a =>
                    (a.SenderId == userIdFromToken || a.ReceiverId == userIdFromToken ||
                     a.SenderId == dto.ReceiverId || a.ReceiverId == dto.ReceiverId)
                    && a.Date == dto.Date
                    && a.Status != AppointmentStatus.Rechazada);

                if (conflict)
                    return BadRequest("Ya existe una cita en esa fecha para alguno de los participantes.");

                var appointment = new Appointment
                {
                    Date = dto.Date,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    SenderId = userIdFromToken,
                    ReceiverId = dto.ReceiverId,
                    Status = AppointmentStatus.Pendiente,
                    CreatedBy = userIdFromToken
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cita creada: {AppointmentId} por usuario {SenderId}", appointment.Id, userIdFromToken);

                return Ok(appointment);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al crear cita para usuario {UserId}", userIdFromToken);
                return StatusCode(500, "Error al crear la cita. Intenta más tarde.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear cita para usuario {UserId}", userIdFromToken);
                return StatusCode(500, "Error inesperado. Intenta más tarde.");
            }
        }
        //Obtengo cita por ID
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = User.FindFirst("uid")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Acceso no autorizado al obtener cita {AppointmentId}", id);
                return Unauthorized();
            }

            try
            {
                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (appointment == null)
                    return NotFound();

                if (appointment.SenderId != userId && appointment.ReceiverId != userId &&
                    !User.IsInRole("Administrator"))
                {
                    _logger.LogWarning("Acceso denegado a cita {AppointmentId} por usuario {UserId}", id, userId);
                    return Forbid();
                }

                if (appointment.ReceiverId == userId &&
                    appointment.Status == AppointmentStatus.Pendiente)
                {
                    appointment.Status = AppointmentStatus.Leido;
                    await _context.SaveChangesAsync();
                }

                return Ok(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cita {AppointmentId}", id);
                return StatusCode(500, "Error al obtener la cita. Intenta más tarde.");
            }
        }
        //Obtiene todas las citas por paginas
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyAppointments(int page = 1, int pageSize = 10)
        {
            var userId = User.FindFirst("uid")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Acceso no autorizado al obtener mis citas");
                return Unauthorized();
            }

            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 50) pageSize = 50;

                var query = _context.Appointments
                    .Where(a => a.SenderId == userId || a.ReceiverId == userId)
                    .OrderByDescending(a => a.Date);

                var total = await query.CountAsync();

                var appointments = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                bool changes = false;

                foreach (var appointment in appointments)
                {
                    if (appointment.ReceiverId == userId &&
                        appointment.Status == AppointmentStatus.Pendiente)
                    {
                        appointment.Status = AppointmentStatus.Leido;
                        changes = true;
                    }
                }

                if (changes)
                    await _context.SaveChangesAsync();

                return Ok(new
                {
                    total,
                    page,
                    pageSize,
                    data = appointments
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener citas del usuario {UserId}", userId);
                return StatusCode(500, "Error al obtener tus citas. Intenta más tarde.");
            }
        }
        //Obtiene las citas de un usuario, SOLO admin (A usar por motivos de requirimiento judicial)
        [Authorize(Roles = "Administrator")]
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetByUser(string id, int page = 1, int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 50) pageSize = 50;

                var query = _context.Appointments
                    .Where(a => a.SenderId == id || a.ReceiverId == id)
                    .OrderByDescending(a => a.Date);

                var total = await query.CountAsync();

                var appointments = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    total,
                    page,
                    pageSize,
                    data = appointments
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener citas del usuario {UserId}", id);
                return StatusCode(500, "Error al obtener las citas. Intenta más tarde.");
            }
        }
        //Aceptar cita
        [Authorize]
        [HttpPut("{id}/accept")]
        public async Task<IActionResult> Accept(int id)
        {
            var userId = User.FindFirst("uid")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Acceso no autorizado al aceptar cita {AppointmentId}", id);
                return Unauthorized();
            }

            try
            {
                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment == null)
                    return NotFound();

                if (appointment.ReceiverId != userId)
                {
                    _logger.LogWarning("Acceso denegado al aceptar cita {AppointmentId} por usuario {UserId}", id, userId);
                    return Forbid();
                }

                if (appointment.Status == AppointmentStatus.Aceptado)
                    return BadRequest("La cita ya fue aceptada.");

                if (appointment.Status == AppointmentStatus.Rechazada ||
                    appointment.Status == AppointmentStatus.Completado ||
                    appointment.Status == AppointmentStatus.Cancelado)
                {
                    _logger.LogWarning("Intento de aceptar cita con estado inválido {AppointmentId}: {Status}", id, appointment.Status);
                    return BadRequest("La cita no puede ser aceptada en su estado actual.");
                }

                appointment.Status = AppointmentStatus.Aceptado;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cita aceptada: {AppointmentId} por usuario {UserId}", id, userId);

                return Ok(appointment);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al aceptar cita {AppointmentId}", id);
                return StatusCode(500, "Error al aceptar la cita. Intenta más tarde.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al aceptar cita {AppointmentId}", id);
                return StatusCode(500, "Error inesperado. Intenta más tarde.");
            }
        }
        //Rehazar cita
        [Authorize]
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            var userId = User.FindFirst("uid")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Acceso no autorizado al rechazar cita {AppointmentId}", id);
                return Unauthorized();
            }

            try
            {
                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment == null)
                    return NotFound();

                if (appointment.ReceiverId != userId)
                {
                    _logger.LogWarning("Acceso denegado al rechazar cita {AppointmentId} por usuario {UserId}", id, userId);
                    return Forbid();
                }

                if (appointment.Status == AppointmentStatus.Aceptado ||
                    appointment.Status == AppointmentStatus.Completado ||
                    appointment.Status == AppointmentStatus.Cancelado)
                {
                    _logger.LogWarning("Intento de rechazar cita con estado inválido {AppointmentId}: {Status}", id, appointment.Status);
                    return BadRequest("La cita ya no puede ser rechazada.");
                }

                appointment.Status = AppointmentStatus.Rechazada;
                appointment.RejectedByUserId = userId;
                appointment.RejectedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Cita rechazada: {AppointmentId} por usuario {UserId}", id, userId);

                return Ok(appointment);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al rechazar cita {AppointmentId}", id);
                return StatusCode(500, "Error al rechazar la cita. Intenta más tarde.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al rechazar cita {AppointmentId}", id);
                return StatusCode(500, "Error inesperado. Intenta más tarde.");
            }
        }
        //Completar y finalizar cita
        [Authorize]
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            var userId = User.FindFirst("uid")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Acceso no autorizado al completar cita {AppointmentId}", id);
                return Unauthorized();
            }

            try
            {
                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment == null)
                    return NotFound();

                if (appointment.SenderId != userId && appointment.ReceiverId != userId)
                {
                    _logger.LogWarning("Acceso denegado al completar cita {AppointmentId} por usuario {UserId}", id, userId);
                    return Forbid();
                }

                if (appointment.Status != AppointmentStatus.Aceptado)
                {
                    _logger.LogWarning("Intento de completar cita con estado inválido {AppointmentId}: {Status}", id, appointment.Status);
                    return BadRequest("Sólo una cita aceptada puede marcarse como completada.");
                }

                appointment.Status = AppointmentStatus.Completado;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cita completada: {AppointmentId} por usuario {UserId}", id, userId);

                return Ok(appointment);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al completar cita {AppointmentId}", id);
                return StatusCode(500, "Error al completar la cita. Intenta más tarde.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al completar cita {AppointmentId}", id);
                return StatusCode(500, "Error inesperado. Intenta más tarde.");
            }
        }
    }
}