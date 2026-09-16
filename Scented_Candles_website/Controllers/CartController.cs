using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScentedCandleWebsite.Data;
using ScentedCandleWebsite.Models;

namespace ScentedCandleWebsite.Controllers
{
    [Authorize] // Require login to access cart
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<CartController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Cart
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.AddedDate)
                .ToListAsync();

            var subtotal = cartItems.Sum(c => c.Quantity * (c.Product?.Price ?? 0));
            var shipping = subtotal > 0 ? (subtotal > 100 ? 0 : 9.99m) : 0;
            var total = subtotal + shipping;

            ViewBag.Subtotal = subtotal;
            ViewBag.Shipping = shipping;
            ViewBag.Total = total;
            ViewBag.CartCount = cartItems.Sum(c => c.Quantity);

            return View(cartItems);
        }

        // POST: /Cart/AddToCart (Simplified - handles both AJAX and form POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                // Check if user is authenticated
                if (!User.Identity.IsAuthenticated)
                {
                    TempData["Error"] = "Please login to add items to cart.";
                    return RedirectToAction("Login", "Account", new { returnUrl = Request.Headers["Referer"].ToString() });
                }

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "User not found. Please login again.";
                    return RedirectToAction("Login", "Account");
                }

                // Get the product
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Check if product is active and in stock
                if (!product.IsActive || product.StockQuantity <= 0)
                {
                    TempData["Error"] = "Product is not available.";
                    return RedirectToAction("Index", "Home");
                }

                // Check if item already exists in cart
                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == userId);

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Updated quantity for {product.Name}.";
                }
                else
                {
                    // Create new cart item
                    var cartItem = new CartItem
                    {
                        UserId = userId,
                        ProductId = productId,
                        Quantity = quantity,
                        AddedDate = DateTime.UtcNow
                    };

                    _context.CartItems.Add(cartItem);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"{product.Name} added to cart!";
                }

                // Redirect back to the page they came from
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    return Redirect(referer);
                }
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart");
                TempData["Error"] = "Error adding to cart. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Cart/Index
        //public async Task<IActionResult> Index()
        //{
        //    if (!User.Identity.IsAuthenticated)
        //    {
        //        return RedirectToAction("Login", "Account");
        //    }

        //    var userId = _userManager.GetUserId(User);
        //    var cartItems = await _context.CartItems
        //        .Include(c => c.Product)
        //        .Where(c => c.UserId == userId)
        //        .ToListAsync();

        //    ViewBag.Total = cartItems.Sum(c => c.Quantity * c.Product.Price);
        //    return View(cartItems);
        //}


        // POST: /Cart/UpdateQuantity
        // POST: /Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var cartItem = await _context.CartItems
                    .Include(c => c.Product)
                    .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Item not found in your cart." });
                }

                if (quantity <= 0)
                {
                    // Remove item if quantity is 0 or less
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Item removed from cart." });
                }

                cartItem.Quantity = quantity;
                await _context.SaveChangesAsync();

                var cartCount = await _context.CartItems
                    .Where(c => c.UserId == userId)
                    .SumAsync(c => c.Quantity);

                return Json(new
                {
                    success = true,
                    message = "Cart updated successfully.",
                    cartCount = cartCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity");
                return Json(new { success = false, message = "Error updating cart. Please try again." });
            }
        }


       

        // GET: /Cart/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            var model = new CheckoutViewModel
            {
                FirstName = user?.FirstName ?? "",
                LastName = user?.LastName ?? "",
                Email = user?.Email ?? "",
                PhoneNumber = user?.PhoneNumber ?? ""
            };

            ViewBag.CartItems = cartItems;
            ViewBag.Subtotal = cartItems.Sum(c => c.Quantity * (c.Product?.Price ?? 0));
            ViewBag.Shipping = ViewBag.Subtotal > 100 ? 0 : 9.99m;
            ViewBag.Total = ViewBag.Subtotal + ViewBag.Shipping;

            return View(model);
        }

        // POST: /Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                var subtotal = cartItems.Sum(c => c.Quantity * (c.Product?.Price ?? 0));
                var shipping = subtotal > 100 ? 0 : 9.99m;
                var total = subtotal + shipping;

                // Create order
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    ShippingAddress = model.ShippingAddress,
                    City = model.City,
                    PostalCode = model.PostalCode,
                    Country = model.Country,
                    PhoneNumber = model.PhoneNumber,
                    TotalAmount = total,
                    Status = "Pending"
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create order items
                foreach (var cartItem in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Product?.Price ?? 0
                    };
                    _context.OrderItems.Add(orderItem);

                    // Update product stock
                    if (cartItem.Product != null)
                    {
                        cartItem.Product.StockQuantity -= cartItem.Quantity;
                        _context.Update(cartItem.Product);
                    }
                }

                // Clear cart
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Order #{order.Id} created by user {userId}");
                TempData["Success"] = $"Order #{order.Id} placed successfully!";

                return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
            }

            ViewBag.CartItems = cartItems;
            ViewBag.Subtotal = cartItems.Sum(c => c.Quantity * (c.Product?.Price ?? 0));
            ViewBag.Shipping = ViewBag.Subtotal > 100 ? 0 : 9.99m;
            ViewBag.Total = ViewBag.Subtotal + ViewBag.Shipping;

            return View(model);
        }

        // GET: /Cart/OrderConfirmation/{orderId}
        [HttpGet]
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        //[HttpGet]
        //public async Task<IActionResult> GetCartCount()
        //{
        //    if (!User.Identity.IsAuthenticated)
        //    {
        //        return Json(new { count = 0 });
        //    }

        //    var userId = _userManager.GetUserId(User);
        //    var count = await _context.CartItems
        //        .Where(c => c.UserId == userId)
        //        .SumAsync(c => c.Quantity);

        //    return Json(new { count = count });
        //}

        // GET: /Cart/GetCount
        // GET: /Cart/GetCartCount
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { count = 0 });
                }

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { count = 0 });
                }

                var count = await _context.CartItems
                    .Where(c => c.UserId == userId)
                    .SumAsync(c => c.Quantity);

                return Json(new { count = count });
            }
            catch
            {
                return Json(new { count = 0 });
            }
        }

        // POST: /Cart/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not found." });
                }

                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Item not found in your cart." });
                }

                // Get product name for the message
                var productName = cartItem.Product?.Name ?? "Item";

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                // Get updated cart count
                var cartCount = await _context.CartItems
                    .Where(c => c.UserId == userId)
                    .SumAsync(c => c.Quantity);

                return Json(new
                {
                    success = true,
                    message = $"{productName} removed from cart.",
                    cartCount = cartCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item");
                return Json(new { success = false, message = "Error removing item. Please try again." });
            }
        }


    }
}

