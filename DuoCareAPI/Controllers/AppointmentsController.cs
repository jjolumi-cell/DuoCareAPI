using DuoCare.Data;
using DuoCare.Dtos;
using DuoCare.Models;
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

        // Only authenticated users can create appointments.
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(AppointmentDto dto)
        {
            if (dto.SenderId == dto.ReceiverId)
                return BadRequest("Sender and receiver cannot be the same user.");

            if (dto.Date < DateTime.Now)
                return BadRequest("The appointment date cannot be in the past.");

            if (dto.Latitude == 0 || dto.Longitude == 0)
                return BadRequest("Invalid location.");

            var appointment = new Appointment
            {
                Date = dto.Date,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                SenderId = dto.SenderId,
                ReceiverId = dto.ReceiverId
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return Ok(appointment);
        }

        // Only the owner of the appointments OR an Administrator
        [Authorize]
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetByUser(string id)
        {
            // Extract authenticated user ID from JWT
            var loggedUserId = User.FindFirst("uid")?.Value;

            // If not admin AND not the owner → deny access
            if (!User.IsInRole("Administrator") && loggedUserId != id)
                return Forbid("You are not allowed to access these appointments.");

            var appointments = await _context.Appointments
                .Where(a => a.SenderId == id || a.ReceiverId == id)
                .Select(a => new AppointmentResponseDto
                {
                    Id = a.Id,
                    Date = a.Date,
                    Status = a.Status,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    SenderId = a.SenderId,
                    ReceiverId = a.ReceiverId
                })
                .ToListAsync();

            return Ok(appointments);
        }
    }
}
