using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models.DTOs
{
    public class EventDto
    {
        [Required]
        [MaxLength(150)]
        public string EventName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        [MaxLength(200)]
        public string Venue { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Upcoming|Open|Completed|Cancelled)$",
            ErrorMessage = "Status must be one of: Upcoming, Open, Completed, Cancelled")]
        public string Status { get; set; } = "Upcoming";
    }
}