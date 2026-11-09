
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models
{
    public class CaptureResultViewModel
    {
        public int EventId { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a participant.")]
        [Display(Name = "Participant")]
        public int EnrolmentId { get; set; }

        [Required(ErrorMessage = "Finish time is required.")]
        [RegularExpression(
            @"^\d{2,}:[0-5]\d:[0-5]\d$",
            ErrorMessage = "Use HH:mm:ss format, e.g. 01:25:30.")]
        [Display(Name = "Finish Time")]
        public string FinishTime { get; set; }
            = string.Empty;

        [Range(1, int.MaxValue,
            ErrorMessage = "Finishing position must be greater than zero.")]
        [Display(Name = "Finishing Position")]
        public int FinishPosition { get; set; }

        // Used to populate the participant dropdown
        public List<EnrolmentViewModel> AvailableEnrolments
        { get; set; } = new();
    }
}
