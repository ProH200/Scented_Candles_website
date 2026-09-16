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
        public async Task<IActionResult> CreateProduct(Product product, IFormFile? productImage)
        {
            // Remove validation for properties that might not be in the form
            ModelState.Remove("ImageUrl");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("Category");

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

                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        var filePath = Path.Combine(uploadPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await productImage.CopyToAsync(stream);
                        }

                        product.ImageUrl = $"/images/products/{fileName}";
                    }
                    else
                    {
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

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Candle '{product.Name}' has been created successfully!";
                    return RedirectToAction(nameof(Products));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    TempData["Error"] = "An error occurred while creating the product. Please try again.";
                }
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
        }

        // ===== EDIT PRODUCT =====
        // GET: /Admin/EditProduct/{id}
        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
        }

        // POST: /Admin/EditProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Product product, IFormFile? productImage)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            // Remove validation for properties that might not be in the form
            ModelState.Remove("ImageUrl");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("Category");

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.Categories.ToListAsync();
                return View(product);
            }

            try
            {
                // Get the existing product from the database
                var existingProduct = await _context.Products.FindAsync(id);

                if (existingProduct == null)
                {
                    return NotFound();
                }

                // Update all properties
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.Scent = product.Scent;
                existingProduct.Color = product.Color;
                existingProduct.BurnTime = product.BurnTime;
                existingProduct.WaxType = product.WaxType;
                existingProduct.Size = product.Size;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.IsActive = product.IsActive;
                existingProduct.IsFeatured = product.IsFeatured;
                existingProduct.IsOnSale = product.IsOnSale;
                existingProduct.SalePrice = product.SalePrice;
                existingProduct.DiscountPercentage = product.DiscountPercentage;
                existingProduct.RequiredRole = product.RequiredRole;
                existingProduct.UpdatedAt = DateTime.UtcNow;

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

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                            existingProduct.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                            catch
                            {
                                // Log error but continue
                            }
                        }
                    }

                    // Save new image
                    var fileName = Guid.NewGuid().ToString() + fileExtension;
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await productImage.CopyToAsync(stream);
                    }

                    existingProduct.ImageUrl = $"/images/products/{fileName}";
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Candle '{existingProduct.Name}' has been updated successfully!";
                return RedirectToAction(nameof(Products));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.Id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError("Concurrency error updating product {ProductId}", product.Id);
                    TempData["Error"] = "The product was modified by another user. Please try again.";
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", product.Id);
                TempData["Error"] = "An error occurred while updating the product. Please try again.";
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
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

        // POST: /Admin/UpdateOrderStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Orders));
                }

                // Validate status
                var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
                if (!validStatuses.Contains(status))
                {
                    TempData["Error"] = "Invalid status.";
                    return RedirectToAction(nameof(Order));
                }

                // Update order
                order.Status = status;
                order.UpdatedAt = DateTime.UtcNow;

                if (status == "Delivered")
                {
                    order.DeliveredDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Order #{orderId} status updated to {status}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                TempData["Error"] = "Error updating order status: " + ex.Message;
            }

            return RedirectToAction(nameof(Orders));
        }

        // GET: /Admin/GetOrderDetails
        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                // Return HTML for "Order not found" message
                return Content("<div class='text-center py-4'><i class='fas fa-exclamation-circle' style='font-size: 30px; color: #dc3545;'></i><p class='mt-2'>Order not found.</p></div>", "text/html");
            }

            // Build HTML for the modal body
            var html = $@"
        <div class='row g-3 mb-4'>
            <div class='col-md-6'>
                <div class='info-card' style='background: #f8f9fa; padding: 15px; border-radius: 12px; height: 100%;'>
                    <h6 style='color: #800000; margin-bottom: 10px; font-weight: 700; font-size: 0.85rem;'>
                        <i class='fas fa-user me-2'></i>Customer Information
                    </h6>
                    <p class='mb-1' style='font-size: 0.85rem;'><strong>Name:</strong> {order.User?.FirstName} {order.User?.LastName}</p>
                    <p class='mb-1' style='font-size: 0.85rem;'><strong>Email:</strong> {order.User?.Email}</p>
                    <p class='mb-0' style='font-size: 0.85rem;'><strong>Phone:</strong> {order.PhoneNumber}</p>
                    <p class='mb-0 mt-2' style='font-size: 0.85rem;'><strong>Status:</strong> <span class='badge' style='background: #800000; color: white;'>{order.Status}</span></p>
                </div>
            </div>
            <div class='col-md-6'>
                <div class='info-card' style='background: #f8f9fa; padding: 15px; border-radius: 12px; height: 100%;'>
                    <h6 style='color: #800000; margin-bottom: 10px; font-weight: 700; font-size: 0.85rem;'>
                        <i class='fas fa-truck me-2'></i>Shipping Address
                    </h6>
                    <p class='mb-1' style='font-size: 0.85rem;'>{order.ShippingAddress}</p>
                    <p class='mb-1' style='font-size: 0.85rem;'>{order.City}, {order.PostalCode}</p>
                    <p class='mb-0' style='font-size: 0.85rem;'>{order.Country}</p>
                    <p class='mb-0 mt-2' style='font-size: 0.85rem;'><strong>Order Date:</strong> {order.OrderDate.ToString("MMM dd, yyyy hh:mm tt")}</p>
                </div>
            </div>
        </div>

        <h6 style='color: #800000; margin-bottom: 12px; font-weight: 700; font-size: 0.9rem;'>
            <i class='fas fa-box me-2'></i>Order Items
        </h6>
        <div class='table-responsive'>
            <table class='table table-sm' style='border-radius: 12px; overflow: hidden; font-size: 0.85rem;'>
                <thead style='background: #800000; color: white;'>
                    <tr>
                        <th style='padding: 10px 15px;'>Product</th>
                        <th style='padding: 10px 15px;'>Quantity</th>
                        <th style='padding: 10px 15px;'>Price</th>
                        <th style='padding: 10px 15px;'>Subtotal</th>
                    </tr>
                </thead>
                <tbody>";

            foreach (var item in order.OrderItems)
            {
                html += $@"
                    <tr>
                        <td style='padding: 10px 15px;'>{item.Product?.Name}</td>
                        <td style='padding: 10px 15px;'>{item.Quantity}</td>
                        <td style='padding: 10px 15px;'>R {item.Price.ToString("F2")}</td>
                        <td style='padding: 10px 15px; font-weight: 600; color: #800000;'>R {(item.Quantity * item.Price).ToString("F2")}</td>
                    </tr>";
            }

            html += $@"
                </tbody>
                <tfoot style='background: #f8f9fa; font-weight: 700;'>
                    <tr>
                        <th colspan='3' class='text-end' style='padding: 10px 15px;'>Total:</th>
                        <th style='padding: 10px 15px; color: #800000; font-size: 1.1rem;'>R {order.TotalAmount.ToString("F2")}</th>
                    </tr>
                </tfoot>
            </table>
        </div>
        <div class='alert mt-3' style='background: rgba(128, 0, 0, 0.05); border: none; border-left: 3px solid #800000; border-radius: 12px; padding: 12px 16px; font-size: 0.85rem;'>
            <i class='fas fa-calendar me-2' style='color: #800000;'></i>
            Order placed on {order.OrderDate.ToString("MMMM dd, yyyy hh:mm tt")}";

            if (order.UpdatedAt.HasValue)
            {
                html += $@"<br />
            <i class='fas fa-clock me-2' style='color: #800000;'></i>
            Last updated: {order.UpdatedAt.Value.ToString("MMMM dd, yyyy hh:mm tt")}";
            }

            if (order.DeliveredDate.HasValue)
            {
                html += $@"<br />
            <i class='fas fa-check-circle me-2' style='color: #28a745;'></i>
            Delivered on: {order.DeliveredDate.Value.ToString("MMMM dd, yyyy hh:mm tt")}";
            }

            html += @"</div>";

            // Return the HTML content
            return Content(html, "text/html");
        }
    }
}