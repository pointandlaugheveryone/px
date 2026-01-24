using Microsoft.EntityFrameworkCore;
namespace MinimalAPI;

public class ApiDbContext(DbContextOptions<ApiDbContext> options) : DbContext(options)
{
	public DbSet<SecretKey> keys { get; set; }
}