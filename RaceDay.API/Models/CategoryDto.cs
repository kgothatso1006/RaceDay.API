using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models.DTOs
{
    public class CategoryDto
    {
        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 500.0, ErrorMessage = "Distance must be between 0.01 and 500 km.")]
        public decimal DistanceKm { get; set; }

        [Required]
        [Range(1, 10000, ErrorMessage = "Max participants must be between 1 and 10,000.")]
        public int MaxParticipants { get; set; }

        [Required]
        [Range(0, 100000, ErrorMessage = "Entry fee cannot be negative.")]
        public decimal EntryFee { get; set; }
    }
}