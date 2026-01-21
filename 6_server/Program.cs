using _6_server;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder();
builder.Services.AddOpenApi();

var connection = string.Empty;
builder.Configuration.AddEnvironmentVariables().AddJsonFile("appsettings.Development.json");
connection = builder.Configuration.GetConnectionString("CONNECTION");

builder.Services.AddDbContext<ApiDbContext>(options =>
	options.UseSqlServer(connection));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.UseSwaggerUI(options =>
	{
		options.SwaggerEndpoint("/openapi/v1.json", "v1");
	});
}

app.MapGet("/", () => "Here you can safely store your openAI api keys!");

app.MapGet("/key", (ApiDbContext context) => context.SecretKey.ToList());

app.MapPost("/key", (SecretKey secret, ApiDbContext context) =>
{
	context.Add(secret);
	context.SaveChanges();
});

app.Run();