using ABCRetailersPOE.Models;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailersPOE.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Cart> Cart => Set<Cart>();
        public DbSet<Order> Orders => Set<Order>();
    }
}
