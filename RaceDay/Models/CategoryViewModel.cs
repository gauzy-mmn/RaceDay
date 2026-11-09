using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models
{
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }

        public int EventId { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }
    }
}
