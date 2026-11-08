using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Models.DTOs;

namespace RaceDay.API.Controllers
{
    [Route("api/events")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventsController(RaceDayDbContext context)
        {
            _context = context;
        }

        private async Task<int?> GetOrganiserId() => (await _context.Organisers.FirstOrDefaultAsync(o => o.UserID == int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)))?.OrganiserID;

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllEvents()
        {
            return Ok(await _context.Events.Include(e => e.Categories).OrderBy(e => e.EventDate).ToListAsync());
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetEventById(int id)
        {
            var ev = await _context.Events.Include(e => e.Categories).Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventID == id);
            return ev == null ? NotFound() : Ok(ev);
        }

        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateEvent([FromBody] EventDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var orgId = await GetOrganiserId();
            if (orgId == null) return Forbid();

            var newEvent = new Event
            {
                OrganiserID = orgId.Value,
                EventName = dto.EventName,
                Description = dto.Description,
                EventDate = dto.EventDate,
                StartTime = dto.StartTime,
                Venue = dto.Venue,
                Location = dto.Location,
                Status = dto.Status
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "Event created successfully.", eventId = newEvent.EventID });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventDto dto)
        {
            var orgId = await GetOrganiserId();
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventID == id);
            if (ev == null) return NotFound();
            if (ev.OrganiserID != orgId) return Forbid();

            ev.EventName = dto.EventName;
            ev.Description = dto.Description;
            ev.EventDate = dto.EventDate;
            ev.StartTime = dto.StartTime;
            ev.Venue = dto.Venue;
            ev.Location = dto.Location;
            ev.Status = dto.Status;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Event updated successfully." });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var orgId = await GetOrganiserId();
            var ev = await _context.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventID == id);
            if (ev == null) return NotFound();
            if (ev.OrganiserID != orgId) return Forbid();

            if (await _context.Enrolments.AnyAsync(en => ev.Categories.Select(c => c.CategoryID).Contains(en.CategoryID)))
                return Conflict(new { message = "Cannot delete event with enrolments." });

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- NEW METHOD ADDED HERE ---
        // POST: api/events/{id}/banner
        [HttpPost("{id}/banner")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UploadBanner(int id, IFormFile file, [FromServices] RaceDay.API.Services.BlobService blobService)
        {
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventID == id);
            if (ev == null) return NotFound();
            if (ev.OrganiserID != await GetOrganiserId()) return Forbid();

            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            var imageUrl = await blobService.UploadFileAsync(file);

            ev.BannerUrl = imageUrl;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Banner uploaded successfully.", url = imageUrl });
        }
    }
}