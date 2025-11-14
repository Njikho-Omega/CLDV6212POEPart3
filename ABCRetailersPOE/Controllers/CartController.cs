using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ABCRetailersPOE.Services;
using ABCRetailersPOE.Data;
using ABCRetailersPOE.Models;
using ABCRetailersPOE.Models.ViewModels; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Controllers
{
    [Authorize(Roles = "Customer")] // ✅ Restrict to logged-in customers only
    public class CartController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly IFunctionsApi _api;

        public CartController(AuthDbContext db, IFunctionsApi api)
        {
            _db = db;
            _api = api;
        }

        // ======================================================
        // GET: /Cart/Index → Display items in cart
        // ======================================================
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            var cartItems = await _db.Cart
                .Where(c => c.CustomerUsername == username)
                .ToListAsync();

            var viewModelList = new List<CartItemViewModel>();

            // ✅ For each cart item, fetch product info from Azure
            foreach (var item in cartItems)
            {
                var product = await _api.GetProductAsync(item.ProductId);
                if (product == null) continue;

                viewModelList.Add(new CartItemViewModel
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                });
            }

            // ✅ Return the cart view model
            return View(new CartPageViewModel { Items = viewModelList });
        }

        // ======================================================
        // GET: /Cart/Add → Add product to cart
        // ======================================================
        public async Task<IActionResult> Add(string productId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(productId))
                return RedirectToAction("Index", "Product");

            var product = await _api.GetProductAsync(productId);
            if (product == null)
                return NotFound();

            var existing = await _db.Cart.FirstOrDefaultAsync(c =>
                c.ProductId == productId && c.CustomerUsername == username);

            // ✅ Increment quantity if product already in cart
            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                _db.Cart.Add(new Cart
                {
                    CustomerUsername = username,
                    ProductId = productId,
                    Quantity = 1
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"{product.ProductName} added to cart.";
            return RedirectToAction("Index", "Product");
        }

        // ======================================================
        // POST: /Cart/Checkout → Place orders for all items in cart
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            try
            {
                // ✅ Step 1: Get actual Customer from Azure Table using Username
                var customer = await _api.GetCustomerByUsernameAsync(username);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found.";
                    return RedirectToAction("Index");
                }

                // ✅ Step 2: Fetch all cart items from local DB (with current quantities)
                var cartItems = await _db.Cart
                    .Where(c => c.CustomerUsername == username)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Index");
                }

                System.Diagnostics.Debug.WriteLine($"Checking out {cartItems.Count} items");

                // ✅ Step 3: Validate stock and create orders
                foreach (var item in cartItems)
                {
                    System.Diagnostics.Debug.WriteLine($"Creating order for {item.ProductId}, Quantity: {item.Quantity}");

                    // Validate stock before creating order
                    var product = await _api.GetProductAsync(item.ProductId);
                    if (product == null)
                    {
                        TempData["Error"] = $"Product {item.ProductId} not found.";
                        return RedirectToAction("Index");
                    }

                    if (product.StockAvailable < item.Quantity)
                    {
                        TempData["Error"] = $"Insufficient stock for {product.ProductName}. Available: {product.StockAvailable}, Requested: {item.Quantity}";
                        return RedirectToAction("Index");
                    }

                    // Create order with the actual quantity from cart
                    await _api.CreateOrderAsync(customer.Id, item.ProductId, item.Quantity);
                    System.Diagnostics.Debug.WriteLine($"Order created for {product.ProductName}, Qty: {item.Quantity}");
                }

                // ✅ Step 4: Clear local cart after successful checkout
                _db.Cart.RemoveRange(cartItems);
                await _db.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine("Checkout completed successfully");

                // ✅ Step 5: Add confirmation message and redirect to confirmation page
                TempData["SuccessMessage"] = "✅ Order placed successfully!";
                return RedirectToAction("Confirmation");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error during checkout: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Checkout error: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        // ======================================================
        // GET: /Cart/Confirmation → Simple confirmation page after checkout
        // ======================================================
        public IActionResult Confirmation()
        {
            // ✅ Display success message on a dedicated confirmation page
            ViewBag.Message = TempData["SuccessMessage"] ?? "Thank you for your purchase!";
            return View();
        }

        // ======================================================
        // POST: /Cart/Remove → Remove product from cart
        // ======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(string productId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(productId))
            {
                TempData["Error"] = "Product ID is required.";
                return RedirectToAction("Index");
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"=== REMOVE METHOD CALLED ===");
                System.Diagnostics.Debug.WriteLine($"Removing product: {productId} for user: {username}");

                var item = await _db.Cart.FirstOrDefaultAsync(c =>
                    c.CustomerUsername == username && c.ProductId == productId);

                if (item != null)
                {
                    _db.Cart.Remove(item);
                    await _db.SaveChangesAsync();
                    TempData["Success"] = "Item removed from cart successfully!";
                    System.Diagnostics.Debug.WriteLine($"=== SUCCESS: Removed {productId} from cart ===");
                }
                else
                {
                    TempData["Error"] = "Item not found in cart.";
                    System.Diagnostics.Debug.WriteLine($"=== ERROR: Item {productId} not found for user {username} ===");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error removing item: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"=== REMOVE ERROR: {ex.Message} ===");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return RedirectToAction("Index");
        }

        // ======================================================
        // POST: /Cart/UpdateQuantities → Update quantities in cart
        // ======================================================
        [HttpPost]
        public async Task<IActionResult> UpdateQuantities([FromForm] List<CartItemViewModel> items)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index");

            try
            {
                // Debug: Check what's being received
                System.Diagnostics.Debug.WriteLine($"Received {items?.Count} items");

                if (items != null)
                {
                    foreach (var item in items)
                    {
                        System.Diagnostics.Debug.WriteLine($"Processing: {item.ProductId}, Qty: {item.Quantity}");

                        var cartItem = await _db.Cart.FirstOrDefaultAsync(c =>
                            c.CustomerUsername == username && c.ProductId == item.ProductId);

                        if (cartItem != null)
                        {
                            if (item.Quantity <= 0)
                            {
                                _db.Cart.Remove(cartItem);
                            }
                            else
                            {
                                // Check stock availability
                                var product = await _api.GetProductAsync(item.ProductId);
                                if (product != null && item.Quantity > product.StockAvailable)
                                {
                                    TempData["Error"] = $"Cannot update {product.ProductName}. Only {product.StockAvailable} in stock.";
                                    return RedirectToAction("Index");
                                }

                                cartItem.Quantity = item.Quantity;
                                System.Diagnostics.Debug.WriteLine($"Updated {item.ProductId} to quantity {item.Quantity}");
                            }
                        }
                    }

                    await _db.SaveChangesAsync();
                    TempData["Success"] = "Cart updated successfully!";
                }
                else
                {
                    TempData["Error"] = "No items received for update.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating cart: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Update error: {ex.Message}");
            }

            return RedirectToAction("Index");
        }
    }
}