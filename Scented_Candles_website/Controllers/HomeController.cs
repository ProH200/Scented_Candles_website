using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScentedCandleWebsite.Data;
using ScentedCandleWebsite.Models;
using ScentedCandleWebsite.ViewModels;
using System.Diagnostics;

namespace ScentedCandleWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: Home page with products
        public async Task<IActionResult> Index(
            string category = null,
            string search = null,
            string sort = "name")
        {
            // Start with base query - only active products
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity > 0)
                .AsQueryable();

            // Apply category filter
            if (!string.IsNullOrEmpty(category))
            {
                productsQuery = productsQuery.Where(p =>
                    p.Category != null && p.Category.Name == category);
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                productsQuery = productsQuery.Where(p =>
                    p.Name.Contains(search) ||
                    p.Description.Contains(search) ||
                    p.Scent.Contains(search));
            }

            // Apply sorting
            switch (sort.ToLower())
            {
                case "price_asc":
                    productsQuery = productsQuery.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Price);
                    break;
                case "newest":
                    productsQuery = productsQuery.OrderByDescending(p => p.CreatedAt);
                    break;
                case "featured":
                    productsQuery = productsQuery.OrderByDescending(p => p.IsFeatured);
                    break;
                default:
                    productsQuery = productsQuery.OrderBy(p => p.Name);
                    break;
            }

            // Get featured products (for hero section)
            var featuredProducts = await _context.Products
                .Where(p => p.IsFeatured && p.IsActive && p.StockQuantity > 0)
                .Take(6)
                .ToListAsync();

            // Get all products
            var allProducts = await productsQuery.ToListAsync();

            // Get categories
            var categories = await _context.Categories.ToListAsync();

            // Build ViewModel
            var viewModel = new HomePageViewModel
            {
                HeroTitle = "Illuminate Your World",
                HeroSubtitle = "Handcrafted soy candles for every moment",
                Categories = categories,
                FeaturedProducts = featuredProducts,
                Products = allProducts,
                TotalProducts = allProducts.Count,
                ProductsOnSale = allProducts.Count(p => p.IsOnSale),
                NewArrivals = allProducts.Count(p => p.CreatedAt > DateTime.UtcNow.AddDays(-30)),
                SelectedCategory = category,
                SearchTerm = search,
                SortBy = sort
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        // GET: /Home/SearchProducts - AJAX endpoint for live search
        [HttpGet]
        public async Task<IActionResult> SearchProducts(string search, string category, string sort)
        {
            // Start with base query - only active products
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity > 0)
                .AsQueryable();

            // Apply category filter
            if (!string.IsNullOrEmpty(category))
            {
                productsQuery = productsQuery.Where(p =>
                    p.Category != null && p.Category.Name == category);
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                productsQuery = productsQuery.Where(p =>
                    p.Name.Contains(search) ||
                    p.Description.Contains(search) ||
                    p.Scent.Contains(search));
            }

            // Apply sorting
            switch (sort?.ToLower())
            {
                case "price_asc":
                    productsQuery = productsQuery.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Price);
                    break;
                case "newest":
                    productsQuery = productsQuery.OrderByDescending(p => p.CreatedAt);
                    break;
                case "featured":
                    productsQuery = productsQuery.OrderByDescending(p => p.IsFeatured);
                    break;
                default:
                    productsQuery = productsQuery.OrderBy(p => p.Name);
                    break;
            }

            var products = await productsQuery.ToListAsync();

            // Return partial view with only the products grid
            return PartialView("_ProductGrid", products);
        }

        // Simple version without database (temporary solution)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Subscribe(string email)
        {
            if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
            {
                TempData["NewsletterError"] = "Please enter a valid email address.";
                return RedirectToAction("Index");
            }

            try
            {
                // Log email to a file instead (temporary)
                var logPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "newsletter-emails.txt");
                System.IO.File.AppendAllText(logPath, $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {email}{Environment.NewLine}");

                TempData["NewsletterSuccess"] = "Thank you for subscribing! You'll receive updates soon.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing email: {Email}", email);
                TempData["NewsletterError"] = "An error occurred. Please try again later.";
            }

            return RedirectToAction("Index");
        }

        // Helper method to validate email
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

       
        
    }
}