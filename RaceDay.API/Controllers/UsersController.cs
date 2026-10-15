using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models.DTOs;

namespace RaceDay.API.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize] // <-- ALL endpoints in this controller require a valid JWT token
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public UsersController(RaceDayDbContext context)
        {
            _context = context;
        }

        // Helper: get the logged-in user's ID from the JWT token
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }

        // GET: api/users/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();

            var user = await _context.Users
                .Include(u => u.Organiser)
                .Include(u => u.Participant)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            // Build a clean response (never expose PasswordHash!)
            var response = new
            {
                user.UserID,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Phone,
                user.Role,
                user.CreatedAt,
                OrganisationName = user.Organiser?.OrganisationName,
                DateOfBirth = user.Participant?.DateOfBirth,
                EmergencyContact = user.Participant?.EmergencyContact,
                EmergencyPhone = user.Participant?.EmergencyPhone
            };

            return Ok(response);
        }

        // PUT: api/users/me
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();

            var user = await _context.Users
                .Include(u => u.Organiser)
                .Include(u => u.Participant)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            // Update common fields
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Phone = dto.Phone;

            // Update role-specific fields
            if (user.Role == "Organiser" && user.Organiser != null)
            {
                if (!string.IsNullOrEmpty(dto.OrganisationName))
                    user.Organiser.OrganisationName = dto.OrganisationName;
            }
            else if (user.Role == "Participant" && user.Participant != null)
            {
                user.Participant.DateOfBirth = dto.DateOfBirth;
                user.Participant.EmergencyContact = dto.EmergencyContact;
                user.Participant.EmergencyPhone = dto.EmergencyPhone;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully." });
        }
    }
}