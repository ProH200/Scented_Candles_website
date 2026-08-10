using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScentedCandleWebsite.Data;
using ScentedCandleWebsite.Models;

namespace ScentedCandleWebsite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var totalProducts = await _context.Products.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var totalUsers = await _context.Users.CountAsync();
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);

            // Additional statistics
            var activeProducts = await _context.Products.CountAsync(p => p.IsActive);
            var lowStockProducts = await _context.Products.CountAsync(p => p.StockQuantity > 0 && p.StockQuantity < 10);
            var outOfStockProducts = await _context.Products.CountAsync(p => p.StockQuantity <= 0);
            var completedOrders = await _context.Orders.CountAsync(o => o.Status == "Delivered");

            // Recent data
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            var recentProducts = await _context.Products
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.ActiveProducts = activeProducts;
            ViewBag.LowStockProducts = lowStockProducts;
            ViewBag.OutOfStockProducts = outOfStockProducts;
            ViewBag.CompletedOrders = completedOrders;
            ViewBag.RecentOrders = recentOrders;
            ViewBag.RecentProducts = recentProducts;

            return View();
        }

        // GET: /Admin/Users - List all users
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.ToListAsync();
            var userRoles = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? "No Role";
            }

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        // GET: /Admin/EditUser/{id}
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                CurrentRole = currentRoles.FirstOrDefault() ?? "Customer",
                AvailableRoles = allRoles
            };

            return View(model);
        }

        // POST: /Admin/EditUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                // Update user details
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.IsActive = model.IsActive;

                var updateResult = await _userManager.UpdateAsync(user);

                if (updateResult.Succeeded)
                {
                    // Update role if changed
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    var currentRole = currentRoles.FirstOrDefault();

                    if (currentRole != model.CurrentRole)
                    {
                        if (!string.IsNullOrEmpty(currentRole))
                        {
                            await _userManager.RemoveFromRoleAsync(user, currentRole);
                        }
                        await _userManager.AddToRoleAsync(user, model.CurrentRole);
                        _logger.LogInformation("User {Email} role changed from {OldRole} to {NewRole}",
                            user.Email, currentRole, model.CurrentRole);
                    }

                    TempData["Success"] = "User updated successfully!";
                    return RedirectToAction(nameof(Users));
                }

                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Reload available roles if model is invalid
            model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        // POST: /Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent deleting the last admin
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var userRoles = await _userManager.GetRolesAsync(user);

            if (userRoles.Contains("Admin") && admins.Count <= 1)
            {
                TempData["Error"] = "Cannot delete the last admin user!";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} deleted by admin", user.Email);
                TempData["Success"] = "User deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to delete user.";
            }

            return RedirectToAction(nameof(Users));
        }

        // GET: /Admin/Products
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .ToListAsync();
            return View(products);
        }

        // GET: /Admin/Orders
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        // GET: /Admin/CreateProduct
        [HttpGet]
        public IActionResult CreateProduct()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        // POST: /Admin/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product, IFormFile productImage)
        {
            // Remove model state errors for custom scent handling
            if (!string.IsNullOrEmpty(product.Scent))
            {
                ModelState.Remove("Scent");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image upload
                    if (productImage != null && productImage.Length > 0)
                    {
                        // Validate image type
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                        var fileExtension = Path.GetExtension(productImage.FileName).ToLower();

                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            TempData["Error"] = "Invalid file type. Please upload JPG, PNG, GIF, or WEBP images only.";
                            ViewBag.Categories = await _context.Categories.ToListAsync();
                            return View(product);
                        }

                        // Generate unique filename
                        var fileName = Guid.NewGuid().ToString() + fileExtension;
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

                        // Create directory if it doesn't exist
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        var filePath = Path.Combine(uploadPath, fileName);

                        // Save file
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await productImage.CopyToAsync(stream);
                        }

                        // Set image URL
                        product.ImageUrl = $"/images/products/{fileName}";
                    }
                    else
                    {
                        // Set default image if none uploaded
                        product.ImageUrl = "/images/products/default-candle.jpg";
                    }

                    // Set additional properties
                    product.CreatedAt = DateTime.UtcNow;
                    product.UpdatedAt = DateTime.UtcNow;

                    // Set default values for missing properties
                    if (string.IsNullOrEmpty(product.Color))
                        product.Color = "Not Specified";

                    if (string.IsNullOrEmpty(product.BurnTime))
                        product.BurnTime = "30-40 hours";

                    if (string.IsNullOrEmpty(product.WaxType))
                        product.WaxType = "Soy Wax";

                    // Add to database
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("New product created: {ProductName} by admin", product.Name);
                    TempData["Success"] = $"Candle '{product.Name}' has been created successfully!";
                    return RedirectToAction(nameof(Products));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    TempData["Error"] = "An error occurred while creating the product. Please try again.";
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // Log validation errors
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var error in errors)
            {
                _logger.LogWarning("Validation error: {Error}", error.ErrorMessage);
            }

            return View(product);
        }

        // GET: /Admin/EditProduct/{id}
        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }

        // POST: /Admin/EditProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Product product, IFormFile productImage)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image upload
                    if (productImage != null && productImage.Length > 0)
                    {
                        // Generate unique filename
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(productImage.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products", fileName);

                        // Create directory if it doesn't exist
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(product.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Save new file
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await productImage.CopyToAsync(stream);
                        }

                        // Update image URL
                        product.ImageUrl = $"/images/products/{fileName}";
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Candle '{product.Name}' has been updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Products));
            }
            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }

        // POST: /Admin/BulkDeleteProducts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDeleteProducts(int[] productIds)
        {
            if (productIds == null || productIds.Length == 0)
            {
                TempData["Error"] = "No products selected for deletion.";
                return RedirectToAction(nameof(Products));
            }

            var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
            _context.Products.RemoveRange(products);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk deleted {Count} products", products.Count);
            TempData["Success"] = $"{products.Count} products have been deleted successfully!";

            return RedirectToAction(nameof(Products));
        }

        

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
        // POST: /Admin/DeleteProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product {ProductName} (ID: {ProductId}) deleted by admin", product.Name, product.Id);
            TempData["Success"] = "Product deleted successfully!";

            return RedirectToAction(nameof(Products));
        }


        
    }
}