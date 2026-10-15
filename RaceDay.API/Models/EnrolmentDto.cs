namespace RaceDay.API.Models.DTOs
{
    public class EnrolmentDto
    {
        // Optional: user can suggest a race number, otherwise it's auto-generated
        public string? RaceNumber { get; set; }
    }
}