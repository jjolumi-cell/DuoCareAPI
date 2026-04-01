using DuoCare.Data;
using DuoCare.Dtos;
using DuoCare.Models;
using DuoCare.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DuoCare.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Create appointment
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(AppointmentDto dto)
        {
            var userIdFromToken = User.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(userIdFromToken))
                return Unauthorized();

            if (userIdFromToken == dto.ReceiverId)
                return BadRequest("El que envia y el receptor no pueden ser el mismo.");

            if (dto.Date < DateTime.Now)
                return BadRequest("La cita no puede estar en el pasado.");

            if (dto.Latitude == 0 || dto.Longitude == 0)
                return BadRequest("No existe tal lugar.");

            var appointment = new Appointment
            {
                Date = dto.Date,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                SenderId = userIdFromToken,
                ReceiverId = dto.ReceiverId,
                Status = AppointmentStatus.Pendiente
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return Ok(appointment);
        }

        // Get appointment by ID (auto-mark as read)
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = User.FindFirst("uid")?.Value;

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound();

            // Auto-mark as read when opened individually
            if (appointment.ReceiverId == userId &&
                appointment.Status == AppointmentStatus.Pendiente)
            {
                appointment.Status = AppointmentStatus.Leido;
                await _context.SaveChangesAsync();
            }

            return Ok(appointment);
        }

        // Get my appointments (paginated + auto-mark as read)
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyAppointments(int page = 1, int pageSize = 10)
        {
            var userId = User.FindFirst("uid")?.Value;

            var query = _context.Appointments
                .Where(a => a.SenderId == userId || a.ReceiverId == userId)
                .OrderByDescending(a => a.Date);

            var total = await query.CountAsync();

            var appointments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ⭐ Auto-mark as read when listing
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

        // Admin: get appointments of a user
        [Authorize(Roles = "Administrator")]
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetByUser(string id, int page = 1, int pageSize = 10)
        {
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

        // Accept appointment
        [Authorize]
        [HttpPut("{id}/accept")]
        public async Task<IActionResult> Accept(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound();

            appointment.Status = AppointmentStatus.Aceptado;
            await _context.SaveChangesAsync();

            return Ok(appointment);
        }

        // Complete appointment
        [Authorize]
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return NotFound();

            appointment.Status = AppointmentStatus.Completado;
            await _context.SaveChangesAsync();

            return Ok(appointment);
        }
    }
}
