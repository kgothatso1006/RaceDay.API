using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models.DTOs
{
    public class UpdateProfileDto
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Phone { get; set; }

        // Participant-specific (optional)
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(100)]
        public string? EmergencyContact { get; set; }

        [MaxLength(30)]
        public string? EmergencyPhone { get; set; }

        // Organiser-specific (optional)
        [MaxLength(150)]
        public string? OrganisationName { get; set; }
    }
}