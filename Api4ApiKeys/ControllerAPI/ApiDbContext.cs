using Microsoft.EntityFrameworkCore;


namespace ControllerAPI;

public class ApiDbContextt(DbContextOptions<ApiDbContext> options) : DbContext(options)
{
	public DbSet<SecretKey> keys { get; set; }
}