using Microsoft.AspNetCore.Identity;

namespace ScentedCandleWebsite.Models
{
    // This class is your custom user account - it extends ASP.NET Core Identity's default user system with extra properties you need for your candle website.
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ProfilePictureUrl { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}