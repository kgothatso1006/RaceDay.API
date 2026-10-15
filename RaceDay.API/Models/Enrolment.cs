using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Enrolment
    {
        [Key]
        public int EnrolmentID { get; set; }

        [Required]
        public int ParticipantID { get; set; }

        [Required]
        public int CategoryID { get; set; }

        public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(20)]
        public string EnrolmentStatus { get; set; } = "Registered"; // Registered, Cancelled, Completed

        [MaxLength(20)]
        public string? RaceNumber { get; set; }

        // Navigation Properties
        [ForeignKey("ParticipantID")]
        public Participant Participant { get; set; } = null!;

        [ForeignKey("CategoryID")]
        public Category Category { get; set; } = null!;

        public Result? Result { get; set; }
    }
}