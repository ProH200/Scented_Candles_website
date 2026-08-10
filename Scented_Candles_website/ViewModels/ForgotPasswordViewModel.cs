using System.ComponentModel.DataAnnotations;

namespace ScentedCandleWebsite.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }
    }
}