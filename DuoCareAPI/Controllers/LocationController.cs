using DuoCare.Data;
using DuoCare.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DuoCareAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LocationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST api/location
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SaveLocation(UserLocation model)
        {
            // El usuario autenticado
            model.UserId = User.FindFirst("uid")?.Value;
            model.Timestamp = DateTime.Now;

            _context.UserLocations.Add(model);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
