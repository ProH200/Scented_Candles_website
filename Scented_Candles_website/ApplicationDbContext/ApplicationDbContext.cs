using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScentedCandleWebsite.Models;

namespace ScentedCandleWebsite.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<NewsletterSubscriber> NewsletterSubscribers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure decimal properties for SQL Server
            // This prevents the "No store type was specified for decimal property" warning

            // Product configuration
            builder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Price)
                    .HasPrecision(18, 2); // 18 total digits, 2 decimal places

                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Order configuration
            builder.Entity<Order>(entity =>
            {
                entity.Property(o => o.TotalAmount)
                    .HasPrecision(18, 2); // 18 total digits, 2 decimal places

                entity.HasOne(o => o.User)
                    .WithMany(u => u.Orders)
                    .HasForeignKey(o => o.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // OrderItem configuration
            builder.Entity<OrderItem>(entity =>
            {
                entity.Property(oi => oi.Price)
                    .HasPrecision(18, 2); // 18 total digits, 2 decimal places

                entity.HasOne(oi => oi.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.Product)
                    .WithMany()
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Category configuration
            builder.Entity<Category>(entity =>
            {
                entity.HasIndex(c => c.Name)
                    .IsUnique(); // Prevent duplicate category names
            });

            // CartItem configuration
            builder.Entity<CartItem>(entity =>
            {
                entity.HasOne(ci => ci.User)
                    .WithMany()
                    .HasForeignKey(ci => ci.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.Product)
                    .WithMany()
                    .HasForeignKey(ci => ci.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(ci => ci.UserId);
                entity.HasIndex(ci => ci.ProductId);
            });
        }
    }
    
}