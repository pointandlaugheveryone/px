using _6_server;
using _6_server.Endpoints;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder();

builder.Configuration
	.AddJsonFile("appsettings.Development.json", optional: true)
	.AddEnvironmentVariables();

if (builder.Configuration.GetConnectionString("CONNECTION") is null)
{
	throw new Exception("No database adress found.");
}

builder.Services.AddDbContext<ApiDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("CONNECTION")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
	config.DocumentName = "APIKeyAPI";
	config.Version = "v1";
	config.Title = $"{config.DocumentName} {config.Version}";
});

var app = builder.Build();

app.UseOpenApi();
app.UseSwaggerUi(config => // pasted from ms learn
{
	config.DocumentTitle = "APIkeyAPI";
	config.Path = "/swagger";
	config.DocumentPath = "/swagger/{documentName}/swagger.json";
	config.DocExpansion = "list";
});

app.MapGet("/", () => "Here you can safely store your openAI api keys!");

app.MapKeyEndpoints();
app.Run();