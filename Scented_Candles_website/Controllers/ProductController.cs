using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScentedCandleWebsite.Data;
using ScentedCandleWebsite.Models;

namespace ScentedCandleWebsite.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ApplicationDbContext context, ILogger<ProductController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Product - Customer view (shows only active products)
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)  // Only show active products
                .Where(p => p.RequiredRole == "Customer" || p.RequiredRole == null) // Show customer-accessible products
                .Include(p => p.Category)
                .OrderBy(p => p.Name)
                .ToListAsync();

            _logger.LogInformation($"Customer view: Showing {products.Count} active products");
            return View(products);
        }

        // GET: /Product/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Check if product should be visible to customers
            if (!product.IsActive)
            {
                return NotFound();
            }

            // Check role-based access
            if (product.RequiredRole == "Admin" && !User.IsInRole("Admin"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(product);
        }
        public IActionResult About()
        {
            // You can pass data to the view if needed
            ViewBag.PageTitle = "About Us - Scented Candles";
            ViewBag.Description = "Learn about our story, mission, and commitment to quality candles";

            return View();
        }
    }
}