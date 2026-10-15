using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models.DTOs
{
    public class RegisterDto
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Phone { get; set; }

        [Required]
        [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be either 'Organiser' or 'Participant'.")]
        public string Role { get; set; } = string.Empty;

        // Optional fields for Organiser
        [MaxLength(150)]
        public string? OrganisationName { get; set; }

        // Optional fields for Participant
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(100)]
        public string? EmergencyContact { get; set; }

        [MaxLength(30)]
        public string? EmergencyPhone { get; set; }
    }
}