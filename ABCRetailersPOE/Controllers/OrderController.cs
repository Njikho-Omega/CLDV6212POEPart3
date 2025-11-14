using Microsoft.AspNetCore.Mvc;
using ABCRetailersPOE.Models;
using ABCRetailersPOE.Services;
using ABCRetailers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace ABCRetailersPOE.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IFunctionsApi _api;
        public OrderController(IFunctionsApi api) => _api = api;

        //admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            var orders = await _api.GetOrdersAsync();
            return View(orders.OrderByDescending(o => o.OrderDateUtc).ToList());
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var order = await _api.GetOrderAsync(id);
            return order is null ? NotFound() : View(order);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Order posted)
        {
            if (!ModelState.IsValid) return View(posted);

            try
            {
                await _api.UpdateOrderStatusAsync(posted.Id, posted.Status.ToString());
                TempData["Success"] = "Order updated successfully!";
                return RedirectToAction(nameof(Manage));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating order: {ex.Message}");
                return View(posted);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _api.DeleteOrderAsync(id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus([FromForm] string id, [FromForm] string newStatus)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== UPDATE ORDER STATUS ===");
                System.Diagnostics.Debug.WriteLine($"Order ID: {id}");
                System.Diagnostics.Debug.WriteLine($"New Status: {newStatus}");

                // Validate inputs
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(newStatus))
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: ID or Status is null/empty");
                    return Json(new { success = false, message = "Order ID and Status are required." });
                }

                // Validate that the status is a valid OrderStatus
                if (!Enum.TryParse<OrderStatus>(newStatus, out var status))
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: Invalid status value: {newStatus}");
                    return Json(new { success = false, message = $"Invalid status: {newStatus}" });
                }

                System.Diagnostics.Debug.WriteLine($"Calling API to update order {id} to status {status}");

                try
                {
                    // Call your API to update the order status
                    await _api.UpdateOrderStatusAsync(id, newStatus);
                    System.Diagnostics.Debug.WriteLine($"=== SUCCESS: Order status updated ===");

                    return Json(new { success = true, message = $"Order status updated to {newStatus}" });
                }
                catch (Exception apiEx)
                {
                    System.Diagnostics.Debug.WriteLine($"=== API CALL ERROR: {apiEx.Message} ===");
                    System.Diagnostics.Debug.WriteLine($"=== API INNER EXCEPTION: {apiEx.InnerException?.Message} ===");
                    return Json(new { success = false, message = $"API Error: {apiEx.Message}" });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== UPDATE STATUS ERROR: {ex.Message} ===");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Failed to update order status: {ex.Message}" });
            }
        }

        // admin and customer
        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> Index()
        {
            var orders = await _api.GetOrdersAsync();
            return View(orders.OrderByDescending(o => o.OrderDateUtc).ToList());
        }

        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var order = await _api.GetOrderAsync(id);
            return order is null ? NotFound() : View(order);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Customer")]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _api.GetProductAsync(productId);
                if (product is not null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.StockAvailable,
                        productName = product.ProductName
                    });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> GetCustomerId(string username)
        {
            try
            {
                var customers = await _api.GetCustomersAsync() ?? new List<Customer>();
                var customer = customers.FirstOrDefault(c => c.Username == username);

                if (customer != null)
                {
                    return Json(new { success = true, customerId = customer.Id });
                }

                return Json(new { success = false, message = "Customer not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error finding customer" });
            }
        }

        // customer
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyOrders()
        {
            var customerId = User.FindFirst("CustomerId")?.Value;

            if (string.IsNullOrEmpty(customerId))
            {
                TempData["Error"] = "Customer not found in session.";
                return RedirectToAction("Index", "Login");
            }

            var orders = await _api.GetOrdersByCustomerIdAsync(customerId);
            return View("MyOrders", orders.OrderByDescending(o => o.OrderDateUtc).ToList());
        }

        // Updated to allow both Admin and Customer
        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> Create()
        {
            var customers = await _api.GetCustomersAsync();
            var products = await _api.GetProductsAsync();

            var vm = new OrderCreateViewModel
            {
                Customers = customers,
                Products = products
            };

            // Auto-set customer for customer users
            if (User.IsInRole("Customer"))
            {
                var currentUsername = User.Identity.Name;
                var customer = customers.FirstOrDefault(c => c.Username == currentUsername);
                if (customer != null)
                {
                    vm.CustomerId = customer.Id;
                }
            }

            return View(vm);
        }


        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            try
            {
                // For customer users, ensure they can only create orders for themselves
                if (User.IsInRole("Customer"))
                {
                    var currentUsername = User.Identity.Name;
                    var customers = await _api.GetCustomersAsync();
                    var currentCustomer = customers.FirstOrDefault(c => c.Username == currentUsername);

                    if (currentCustomer == null || model.CustomerId != currentCustomer.Id)
                    {
                        TempData["Error"] = "You can only create orders for your own account.";
                        return RedirectToAction(nameof(Create));
                    }
                }

                var customer = await _api.GetCustomerAsync(model.CustomerId);
                var product = await _api.GetProductAsync(model.ProductId);

                if (customer is null || product is null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid customer or product selected.");
                    await PopulateDropdowns(model);
                    return View(model);
                }

                if (product.StockAvailable < model.Quantity)
                {
                    ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                    await PopulateDropdowns(model);
                    return View(model);
                }

                await _api.CreateOrderAsync(model.CustomerId, model.ProductId, model.Quantity);
                TempData["Success"] = "Order created successfully";

                // Redirect based on role
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction(nameof(Manage));
                }
                else
                {
                    return RedirectToAction(nameof(MyOrders));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error creating order: {ex.Message}");
                await PopulateDropdowns(model);
                return View(model);
            }
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _api.GetCustomersAsync();
            model.Products = await _api.GetProductsAsync();
        }
    }
}