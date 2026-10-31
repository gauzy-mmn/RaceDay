
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models
{
    public class UpdateProfileViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
            = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
            = string.Empty;

        [Display(Name = "Phone Number")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        public string? PhoneNumber { get; set; }
    }
}
