using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Models.DTOs;

namespace RaceDay.API.Controllers
{
    [Route("api")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        private async Task<int?> GetCurrentOrganiserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return null;
            var userId = int.Parse(userIdClaim);
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
            return organiser?.OrganiserID;
        }

        // GET: api/categories
        [HttpGet("categories")]
        [Authorize]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _context.Categories
                .Include(c => c.Event)
                .Select(c => new
                {
                    c.CategoryID,
                    c.CategoryName,
                    c.DistanceKm,
                    c.MaxParticipants,
                    c.EntryFee,
                    c.EventID,
                    EventName = c.Event.EventName
                })
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/events/{eventId}/categories
        [HttpGet("events/{eventId}/categories")]
        [Authorize]
        public async Task<IActionResult> GetCategoriesForEvent(int eventId)
        {
            var eventExists = await _context.Events.AnyAsync(e => e.EventID == eventId);
            if (!eventExists)
                return NotFound(new { message = "Event not found." });

            var categories = await _context.Categories
                .Where(c => c.EventID == eventId)
                .Select(c => new
                {
                    c.CategoryID,
                    c.CategoryName,
                    c.DistanceKm,
                    c.MaxParticipants,
                    c.EntryFee,
                    c.EventID
                })
                .ToListAsync();

            return Ok(categories);
        }

        // POST: api/events/{eventId}/categories
        [HttpPost("events/{eventId}/categories")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var organiserId = await GetCurrentOrganiserId();
            if (organiserId == null)
                return Forbid();

            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganiserID != organiserId.Value)
                return Forbid();

            var duplicate = await _context.Categories
                .AnyAsync(c => c.EventID == eventId && c.CategoryName == dto.CategoryName);

            if (duplicate)
                return Conflict(new { message = "A category with this name already exists for this event." });

            var category = new Category
            {
                EventID = eventId,
                CategoryName = dto.CategoryName,
                DistanceKm = dto.DistanceKm,
                MaxParticipants = dto.MaxParticipants,
                EntryFee = dto.EntryFee
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Category created successfully.",
                categoryId = category.CategoryID
            });
        }

        // PUT: api/categories/{id}
        [HttpPut("categories/{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var organiserId = await GetCurrentOrganiserId();
            if (organiserId == null)
                return Forbid();

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.Event.OrganiserID != organiserId.Value)
                return Forbid();

            category.CategoryName = dto.CategoryName;
            category.DistanceKm = dto.DistanceKm;
            category.MaxParticipants = dto.MaxParticipants;
            category.EntryFee = dto.EntryFee;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Category updated successfully." });
        }

        // DELETE: api/categories/{id}
        [HttpDelete("categories/{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var organiserId = await GetCurrentOrganiserId();
            if (organiserId == null)
                return Forbid();

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.Event.OrganiserID != organiserId.Value)
                return Forbid();

            var hasEnrolments = await _context.Enrolments.AnyAsync(en => en.CategoryID == id);
            if (hasEnrolments)
                return Conflict(new { message = "Cannot delete a category that has enrolments." });

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}