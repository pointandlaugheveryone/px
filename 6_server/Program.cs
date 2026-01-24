using _6_server;
using _6_server.Endpoints;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder();
builder.Services.AddOpenApi("v0");

builder.Configuration
	.AddJsonFile("appsettings.Development.json", optional: true)
	.AddEnvironmentVariables(); 
if (builder.Configuration.GetConnectionString("CONNECTION") is null)
{
	throw new Exception("No database adress found.");
}

builder.Services.AddDbContext<ApiDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("CONNECTION")));

var app = builder.Build();
app.MapOpenApi();
app.UseSwaggerUI(options => { options.SwaggerEndpoint("/openapi/v1.json", "v1"); });

app.MapGet("/", () => "Here you can safely store your openAI api keys!");

app.MapKeyEndpoints();
app.Run();