using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    public class Result
    {
        [Key]
        public int ResultID { get; set; }

        [Required]
        public int EnrolmentID { get; set; }

        public TimeSpan? FinishTime { get; set; }

        public int? Position { get; set; }

        [Required]
        [MaxLength(20)]
        public string ResultStatus { get; set; } = "Finished"; // Finished, DidNotFinish, Disqualified

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("EnrolmentID")]
        public Enrolment Enrolment { get; set; } = null!;
    }
}