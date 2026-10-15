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
    [Authorize]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EnrolmentsController(RaceDayDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }

        private async Task<int?> GetCurrentParticipantId()
        {
            var userId = GetCurrentUserId();
            var participant = await _context.Participants.FirstOrDefaultAsync(p => p.UserID == userId);
            return participant?.ParticipantID;
        }

        private async Task<int?> GetCurrentOrganiserId()
        {
            var userId = GetCurrentUserId();
            var organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
            return organiser?.OrganiserID;
        }

        // POST: api/categories/{id}/enrol  --  Participant only
        [HttpPost("categories/{id}/enrol")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> EnrolInCategory(int id, [FromBody] EnrolmentDto? dto)
        {
            var participantId = await GetCurrentParticipantId();
            if (participantId == null)
                return Forbid();

            var category = await _context.Categories
                .Include(c => c.Event)
                .Include(c => c.Enrolments)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.Event.Status == "Cancelled" || category.Event.Status == "Completed")
                return Conflict(new { message = "Cannot enrol in a cancelled or completed event." });

            var alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e => e.ParticipantID == participantId.Value
                            && e.CategoryID == id
                            && e.EnrolmentStatus != "Cancelled");

            if (alreadyEnrolled)
                return Conflict(new { message = "You are already enrolled in this category." });

            var activeEnrolmentsCount = category.Enrolments.Count(e => e.EnrolmentStatus != "Cancelled");
            if (activeEnrolmentsCount >= category.MaxParticipants)
                return Conflict(new { message = "This category is full." });

            var raceNumber = !string.IsNullOrEmpty(dto?.RaceNumber)
                ? dto.RaceNumber
                : await GenerateRaceNumber(category.EventID);

            if (await _context.Enrolments.AnyAsync(e => e.RaceNumber == raceNumber))
            {
                raceNumber = await GenerateRaceNumber(category.EventID);
            }

            var enrolment = new Enrolment
            {
                ParticipantID = participantId.Value,
                CategoryID = id,
                EnrolmentDate = DateTime.UtcNow,
                EnrolmentStatus = "Registered",
                RaceNumber = raceNumber
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Enrolment successful.",
                enrolmentId = enrolment.EnrolmentID,
                raceNumber = enrolment.RaceNumber
            });
        }

        private async Task<string> GenerateRaceNumber(int eventId)
        {
            var prefix = $"EVT{eventId}-";
            var existing = await _context.Enrolments
                .Where(e => e.RaceNumber != null && e.RaceNumber.StartsWith(prefix))
                .Select(e => e.RaceNumber)
                .ToListAsync();

            int nextNum = existing.Count + 1;
            string candidate;
            do
            {
                candidate = $"{prefix}{nextNum:D3}";
                nextNum++;
            }
            while (existing.Contains(candidate));

            return candidate;
        }

        // GET: api/enrolments
        [HttpGet("enrolments")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = GetCurrentUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role == "Participant")
            {
                var participantId = await GetCurrentParticipantId();
                if (participantId == null) return Forbid();

                var enrolments = await _context.Enrolments
                    .Where(e => e.ParticipantID == participantId.Value)
                    .Include(e => e.Category)
                        .ThenInclude(c => c.Event)
                    .Include(e => e.Result)
                    .Select(e => new
                    {
                        e.EnrolmentID,
                        e.EnrolmentDate,
                        e.EnrolmentStatus,
                        e.RaceNumber,
                        CategoryName = e.Category.CategoryName,
                        DistanceKm = e.Category.DistanceKm,
                        EntryFee = e.Category.EntryFee,
                        EventName = e.Category.Event.EventName,
                        EventDate = e.Category.Event.EventDate,
                        EventVenue = e.Category.Event.Venue,
                        HasResult = e.Result != null
                    })
                    .ToListAsync();

                return Ok(enrolments);
            }
            else if (role == "Organiser")
            {
                var organiserId = await GetCurrentOrganiserId();
                if (organiserId == null) return Forbid();

                var enrolments = await _context.Enrolments
                    .Where(e => e.Category.Event.OrganiserID == organiserId.Value)
                    .Include(e => e.Category)
                        .ThenInclude(c => c.Event)
                    .Include(e => e.Participant)
                        .ThenInclude(p => p.User)
                    .Select(e => new
                    {
                        e.EnrolmentID,
                        e.EnrolmentDate,
                        e.EnrolmentStatus,
                        e.RaceNumber,
                        ParticipantName = e.Participant.User.FirstName + " " + e.Participant.User.LastName,
                        ParticipantEmail = e.Participant.User.Email,
                        CategoryName = e.Category.CategoryName,
                        EventName = e.Category.Event.EventName,
                        EventDate = e.Category.Event.EventDate
                    })
                    .ToListAsync();

                return Ok(enrolments);
            }

            return Forbid();
        }

        // GET: api/enrolments/{id}
        [HttpGet("enrolments/{id}")]
        public async Task<IActionResult> GetEnrolmentById(int id)
        {
            var userId = GetCurrentUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var enrolment = await _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c.Event)
                .Include(e => e.Participant)
                    .ThenInclude(p => p.User)
                .Include(e => e.Result)
                .FirstOrDefaultAsync(e => e.EnrolmentID == id);

            if (enrolment == null)
                return NotFound(new { message = "Enrolment not found." });

            if (role == "Participant")
            {
                var participantId = await GetCurrentParticipantId();
                if (participantId == null || enrolment.ParticipantID != participantId.Value)
                    return Forbid();
            }
            else if (role == "Organiser")
            {
                var organiserId = await GetCurrentOrganiserId();
                if (organiserId == null || enrolment.Category.Event.OrganiserID != organiserId.Value)
                    return Forbid();
            }

            return Ok(new
            {
                enrolment.EnrolmentID,
                enrolment.EnrolmentDate,
                enrolment.EnrolmentStatus,
                enrolment.RaceNumber,
                Participant = new
                {
                    enrolment.Participant.User.FirstName,
                    enrolment.Participant.User.LastName,
                    enrolment.Participant.User.Email,
                    enrolment.Participant.DateOfBirth,
                    enrolment.Participant.EmergencyContact,
                    enrolment.Participant.EmergencyPhone
                },
                Category = new
                {
                    enrolment.Category.CategoryName,
                    enrolment.Category.DistanceKm,
                    enrolment.Category.EntryFee
                },
                Event = new
                {
                    enrolment.Category.Event.EventName,
                    enrolment.Category.Event.EventDate,
                    enrolment.Category.Event.Venue,
                    enrolment.Category.Event.Location,
                    enrolment.Category.Event.Status
                },
                Result = enrolment.Result == null ? null : new
                {
                    enrolment.Result.FinishTime,
                    enrolment.Result.Position,
                    enrolment.Result.ResultStatus
                }
            });
        }

        // DELETE: api/enrolments/{id}  --  Participant only
        [HttpDelete("enrolments/{id}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var participantId = await GetCurrentParticipantId();
            if (participantId == null)
                return Forbid();

            var enrolment = await _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == id);

            if (enrolment == null)
                return NotFound(new { message = "Enrolment not found." });

            if (enrolment.ParticipantID != participantId.Value)
                return Forbid();

            if (enrolment.EnrolmentStatus == "Cancelled")
                return Conflict(new { message = "This enrolment is already cancelled." });

            if (enrolment.Category.Event.Status == "Completed")
                return Conflict(new { message = "Cannot cancel an enrolment in a completed event." });

            enrolment.EnrolmentStatus = "Cancelled";
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/events/{eventId}/enrolments  --  Organiser only
        [HttpGet("events/{eventId}/enrolments")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEnrolmentsForEvent(int eventId)
        {
            var organiserId = await GetCurrentOrganiserId();
            if (organiserId == null)
                return Forbid();

            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
            if (ev == null)
                return NotFound(new { message = "Event not found." });

            if (ev.OrganiserID != organiserId.Value)
                return Forbid();

            var enrolments = await _context.Enrolments
                .Where(e => e.Category.EventID == eventId)
                .Include(e => e.Category)
                .Include(e => e.Participant)
                    .ThenInclude(p => p.User)
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.RaceNumber,
                    e.EnrolmentDate,
                    e.EnrolmentStatus,
                    ParticipantName = e.Participant.User.FirstName + " " + e.Participant.User.LastName,
                    ParticipantEmail = e.Participant.User.Email,
                    CategoryName = e.Category.CategoryName,
                    DistanceKm = e.Category.DistanceKm
                })
                .ToListAsync();

            return Ok(enrolments);
        }
    }
}