using ControllerAPI;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Configuration
	.AddJsonFile("appsettings.Development.json", optional: true)
	.AddEnvironmentVariables();

if (builder.Configuration.GetConnectionString("CONNECTION") is null)
	throw new Exception("No database address found.");

builder.Services.AddDbContext<ApiDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("CONNECTION")));

var app = builder.Build();

app.MapOpenApi("/openapi/{documentName}.yaml");
app.UseSwaggerUI(options =>
{
	options.SwaggerEndpoint("/openapi/v1.json", "v1");
});

app.MapControllers();

app.Run();