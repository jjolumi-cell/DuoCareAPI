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
    public class RecordsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RecordsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Creates a medical record for a user
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(RecordDto dto)
        {
            var userId = User.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

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

            return Ok(record);
        }


        // IMPORTANT:
        // With [Authorize]
        // Only the owner of the record OR an Administrator
        // can access this endpoint.
        //
        // This protects sensitive medical information and ensures
        // compliance with privacy and legal requirements.
        [Authorize]
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetByUser(string id)
        {
            // Extract the authenticated user ID from the JWT
            var loggedUserId = User.FindFirst("uid")?.Value;

            // If the user is NOT admin and NOT the owner → deny access
            if (!User.IsInRole("Administrator") && loggedUserId != id)
                return Forbid("No tienes permiso para ver este perfil.");

            var record = await _context.Records
                .FirstOrDefaultAsync(r => r.UserId == id);

            return Ok(record);
        }
    }
}

