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

        // Helper: Get the logged-in user's ID
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }

        // Helper: Get the logged-in user's OrganiserID (or null if not an Organiser)
        private async Task<int?> GetCurrentOrganiserId()
        {
            var userId = GetCurrentUserId();
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
            return organiser?.OrganiserID;
        }

        // GET: api/events  --  Any logged-in user
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _context.Events
                .Include(e => e.Categories)
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventID,
                    e.EventName,
                    e.Description,
                    e.EventDate,
                    e.StartTime,
                    e.Venue,
                    e.Location,
                    e.Status,
                    CategoryCount = e.Categories.Count
                })
                .ToListAsync();

            return Ok(events);
        }

        // GET: api/events/{id}  --  Any logged-in user
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetEventById(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Categories)
                .Include(e => e.Organiser)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            var response = new
            {
                ev.EventID,
                ev.EventName,
                ev.Description,
                ev.EventDate,
                ev.StartTime,
                ev.Venue,
                ev.Location,
                ev.Status,
                ev.CreatedAt,
                OrganiserName = ev.Organiser.OrganisationName,
                Categories = ev.Categories.Select(c => new
                {
                    c.CategoryID,
                    c.CategoryName,
                    c.DistanceKm,
                    c.MaxParticipants,
                    c.EntryFee
                })
            };

            return Ok(response);
        }

        // POST: api/events  --  Organiser only
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateEvent([FromBody] EventDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var organiserId = await GetCurrentOrganiserId();
            if (organiserId == null)
                return Forbid(); // user claims Organiser role but has no Organiser profile

            var newEvent = new Event
            {
                OrganiserID = organiserId.Value,
                EventName = dto.EventName,
                Description = dto.Description,
                EventDate = dto.EventDate,
                StartTime = dto.StartTime,
                Venue = dto.Venue,
                Location = dto.Location,
                Status = dto.Status,
                CreatedAt = DateTime.UtcNow
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Event created successfully.",
                eventId = newEvent.EventID
            });
        }

        // PUT: api/events/{id}  --  Organiser only, only their own event
        [HttpPut("{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var organiserId = await GetCurrentOrganiserId();
            if (organiserId == null)
                return Forbid();

            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventID == id);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganiserID != organiserId.Value)
                return Forbid(); // Not their event

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

        // DELETE: api/events/{id}  --  Organiser only, only their own event
        [HttpDelete("{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var organiserId = await GetCurrentOrganiserId();
            if (organiserId == null)
                return Forbid();

            var ev = await _context.Events
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganiserID != organiserId.Value)
                return Forbid();

            // Check if any categories have enrolments (business rule: cannot delete if enrolments exist)
            var hasEnrolments = await _context.Enrolments
                .AnyAsync(en => ev.Categories.Select(c => c.CategoryID).Contains(en.CategoryID));

            if (hasEnrolments)
                return Conflict(new { message = "Cannot delete this event because it has enrolments. Cancel it instead." });

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return NoContent(); // 204
        }
    }
}