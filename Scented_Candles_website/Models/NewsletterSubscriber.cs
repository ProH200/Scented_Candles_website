using System.ComponentModel.DataAnnotations;

namespace ScentedCandleWebsite.Models
{
    public class NewsletterSubscriber
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; }

        public DateTime SubscribedDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? UnsubscribedDate { get; set; }
    }
}