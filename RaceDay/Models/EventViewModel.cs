using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models
{
    public class EventViewModel
    {
        public int EventId { get; set; }

        public int OrganiserId { get; set; }

        [Required(ErrorMessage = "Event name is required.")]
        [Display(Name = "Event Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event date is required.")]
        [Display(Name = "Event Date")]
        [DataType(DataType.DateTime)]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        public string Location { get; set; } = string.Empty;

        [Range(0.01, 10000,
            ErrorMessage = "Distance must be greater than zero.")]
        [Display(Name = "Distance (km)")]
        public decimal Distance { get; set; }

        [Required(ErrorMessage = "Event type is required.")]
        [Display(Name = "Event Type")]
        public string EventType { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
