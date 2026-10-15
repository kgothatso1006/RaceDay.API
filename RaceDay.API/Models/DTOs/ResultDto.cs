using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Models.DTOs
{
    public class ResultDto
    {
        public TimeSpan? FinishTime { get; set; }

        [Range(1, 100000, ErrorMessage = "Position must be a positive number.")]
        public int? Position { get; set; }

        [Required]
        [RegularExpression("^(Finished|DidNotFinish|Disqualified)$",
            ErrorMessage = "ResultStatus must be one of: Finished, DidNotFinish, Disqualified")]
        public string ResultStatus { get; set; } = "Finished";
    }
}