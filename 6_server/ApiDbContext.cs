using Microsoft.EntityFrameworkCore;
namespace _6_server;

public class ApiDbContext(DbContextOptions<ApiDbContext> options) : DbContext(options)
{
	public DbSet<SecretKey> SecretKey { get; set; }
}