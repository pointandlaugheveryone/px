using _6_server;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder();
builder.Services.AddOpenApi("v0");

builder.Configuration.AddEnvironmentVariables().AddJsonFile("appsettings.Development.json");
if (builder.Configuration.GetConnectionString("CONNECTION") is null)
{
	throw new Exception("No database adress found.");
}

builder.Services.AddDbContext<ApiDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("CONNECTION")));

var app = builder.Build();
app.MapOpenApi("/openapi/{documentName}.yaml");
app.UseSwaggerUI(options => { options.SwaggerEndpoint("/openapi/v1.json", "v1"); });


app.MapGet("/", () => "Here you can safely store your openAI api keys!");

app.MapGet("/key/{id:int}", async (int id, ApiDbContext ctx) => // TODO: refactor so both key/id and key/name are handled same way
{
	var key = await ctx.keys.FindAsync(id);
	return key != null
		? Results.Ok(key)
		: Results.NotFound();
});

app.MapPost("/key", async (
	SecretKey inputKey,
	ApiDbContext ctx) =>
{
	if (string.IsNullOrWhiteSpace(inputKey.SecretValue))
		return Results.BadRequest("Invalid Input. ");

	var exists = await ctx.keys
		.AnyAsync(i => i.KeyName == inputKey.KeyName);
	// allow insert of the same secret under different name is on purpose

	if (exists)
		return Results.Conflict("Key name already taken.");

	var key = new SecretKey()
	{
		KeyName = inputKey.KeyName,
		SecretValue = inputKey.SecretValue
	};

	try
	{
		ctx.keys.Add(key);
		await ctx.SaveChangesAsync();
	}
	catch (DbUpdateException)
	{
		return Results.Problem(
			title: "Database error",
			statusCode: StatusCodes.Status500InternalServerError);
	}

	return Results.Created($"/key/{key.KeyName}", key);
});


app.MapPut("/key/{id:int}", async (int id, SecretKey inputKey, ApiDbContext ctx) =>
{
	var key = await ctx.keys.FindAsync(id);
	if (key is null) return Results.NotFound();

	key.KeyName = inputKey.KeyName;
	key.SecretValue = inputKey.SecretValue;
	await ctx.SaveChangesAsync();
	return Results.NoContent();
});

app.MapDelete("/key/{id:int}", async (int id, ApiDbContext ctx) =>
{
	if (await ctx.keys.FindAsync(id) is not { } key) return Results.NotFound();

	ctx.keys.Remove(key);
	await ctx.SaveChangesAsync();
	return Results.NoContent();
});

app.Run();