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
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
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

        // GET: api/results
        [HttpGet("results")]
        public async Task<IActionResult> GetMyResults()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role == "Participant")
            {
                var participantId = await GetCurrentParticipantId();
                if (participantId == null) return Forbid();

                var results = await _context.Results
                    .Where(r => r.Enrolment.ParticipantID == participantId.Value)
                    .Include(r => r.Enrolment)
                        .ThenInclude(e => e.Category)
                            .ThenInclude(c => c.Event)
                    .Select(r => new
                    {
                        r.ResultID,
                        r.FinishTime,
                        r.Position,
                        r.ResultStatus,
                        r.RecordedAt,
                        RaceNumber = r.Enrolment.RaceNumber,
                        CategoryName = r.Enrolment.Category.CategoryName,
                        DistanceKm = r.Enrolment.Category.DistanceKm,
                        EventName = r.Enrolment.Category.Event.EventName,
                        EventDate = r.Enrolment.Category.Event.EventDate
                    })
                    .ToListAsync();

                return Ok(results);
            }
            else if (role == "Organiser")
            {
                var organiserId = await GetCurrentOrganiserId();
                if (organiserId == null) return Forbid();

                var results = await _context.Results
                    .Where(r => r.Enrolment.Category.Event.OrganiserID == organiserId.Value)
                    .Include(r => r.Enrolment)
                        .ThenInclude(e => e.Category)
                            .ThenInclude(c => c.Event)
                    .Include(r => r.Enrolment)
                        .ThenInclude(e => e.Participant)
                            .ThenInclude(p => p.User)
                    .Select(r => new
                    {
                        r.ResultID,
                        r.FinishTime,
                        r.Position,
                        r.ResultStatus,
                        r.RecordedAt,
                        RaceNumber = r.Enrolment.RaceNumber,
                        ParticipantName = r.Enrolment.Participant.User.FirstName + " " + r.Enrolment.Participant.User.LastName,
                        CategoryName = r.Enrolment.Category.CategoryName,
                        EventName = r.Enrolment.Category.Event.EventName,
                        EventDate = r.Enrolment.Category.Event.EventDate
                    })
                    .ToListAsync();

                return Ok(results);
            }

            return Forbid();
        }

        // GET: api/events/{eventId}/results
        [HttpGet("events/{eventId}/results")]
        public async Task<IActionResult> GetResultsForEvent(int eventId)
        {
            var eventExists = await _context.Events.AnyAsync(e => e.EventID == eventId);
            if (!eventExists)
                return NotFound(new { message = "Event not found." });

            var results = await _context.Results
                .Where(r => r.Enrolment.Category.EventID == eventId)
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e.Category)
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e.Participant)
                        .ThenInclude(p => p.User)
                .OrderBy(r => r.Position)
                .Select(r => new
                {
                    r.ResultID,
                    r.FinishTime,
                    r.Position,
                    r.ResultStatus,
                    RaceNumber = r.Enrolment.RaceNumber,
                    ParticipantName = r.Enrolment.Participant.User.FirstName + " " + r.Enrolment.Participant.User.LastName,
                    CategoryName = r.Enrolment.Category.CategoryName,
                    DistanceKm = r.Enrolment.Category.DistanceKm
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/enrolments/{enrolmentId}/result
        [HttpGet("enrolments/{enrolmentId}/result")]
        public async Task<IActionResult> GetResultForEnrolment(int enrolmentId)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var enrolment = await _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);

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

            var result = await _context.Results.FirstOrDefaultAsync(r => r.EnrolmentID == enrolmentId);
            if (result == null)
                return NotFound(new { message = "Result not found for this enrolment." });

            return Ok(new
            {
                result.ResultID,
                result.FinishTime,
                result.Position,
                result.ResultStatus,
                result.RecordedAt
            });
        }

        // POST: api/enrolments/{enrolmentId}/result
        [HttpPost("enrolments/{enrolmentId}/result")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> RecordResult(int enrolmentId, [FromBody] ResultDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var organiserId = await GetCurrentOrganiserId();
            if (organiserId == null)
                return Forbid();

            var enrolment = await _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);

            if (enrolment == null)
                return NotFound(new { message = "Enrolment not found." });

            if (enrolment.Category.Event.OrganiserID != organiserId.Value)
                return Forbid();

            if (enrolment.EnrolmentStatus == "Cancelled")
                return Conflict(new { message = "Cannot record a result for a cancelled enrolment." });

            var existingResult = await _context.Results.AnyAsync(r => r.EnrolmentID == enrolmentId);
            if (existingResult)
                return Conflict(new { message = "A result already exists for this enrolment." });

            var result = new Result
            {
                EnrolmentID = enrolmentId,
                FinishTime = dto.FinishTime,
                Position = dto.Position,
                ResultStatus = dto.ResultStatus,
                RecordedAt = DateTime.UtcNow
            };

            _context.Results.Add(result);

            if (dto.ResultStatus == "Finished")
            {
                enrolment.EnrolmentStatus = "Completed";
            }

            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Result recorded successfully.",
                resultId = result.ResultID
            });
        }

        // PUT: api/results/{id}
        [HttpPut("results/{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> UpdateResult(int id, [FromBody] ResultDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var organiserId = await GetCurrentOrganiserId();
            if (organiserId == null)
                return Forbid();

            var result = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e.Category)
                        .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null)
                return NotFound(new { message = "Result not found." });

            if (result.Enrolment.Category.Event.OrganiserID != organiserId.Value)
                return Forbid();

            result.FinishTime = dto.FinishTime;
            result.Position = dto.Position;
            result.ResultStatus = dto.ResultStatus;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Result updated successfully." });
        }

        // DELETE: api/results/{id}
        [HttpDelete("results/{id}")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> DeleteResult(int id)
        {
            var organiserId = await GetCurrentOrganiserId();
            if (organiserId == null)
                return Forbid();

            var result = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e.Category)
                        .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null)
                return NotFound(new { message = "Result not found." });

            if (result.Enrolment.Category.Event.OrganiserID != organiserId.Value)
                return Forbid();

            _context.Results.Remove(result);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}