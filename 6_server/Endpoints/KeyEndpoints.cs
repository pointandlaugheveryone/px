using Microsoft.EntityFrameworkCore;

namespace _6_server.Endpoints;

public static class KeyEndpoints
{
	public static void MapKeyEndpoints(this IEndpointRouteBuilder app)
	{
		app.MapGet("/key/id/{id:int}", GetById);
		app.MapGet("/key/name/{keyName}", GetByName);

		app.MapPost("/key", Create);

		app.MapPut("/key/id/{id:int}", UpdateById);
		app.MapPut("/key/name/{keyName}", UpdateByName);

		app.MapDelete("/key/id/{id:int}", DeleteById);
	}

	private static async Task<IResult> GetByName(string keyName, ApiDbContext ctx)
	{
		if (string.IsNullOrWhiteSpace(keyName))
			return Results.BadRequest("Invalid key name :(");

		var key = await ctx.keys
			.SingleOrDefaultAsync(k => k.KeyName == keyName);

		return key is not null
			? Results.Ok(key)
			: Results.NotFound();
	}

	private static async Task<IResult> GetById(int id, ApiDbContext ctx)
	{
		var key = await ctx.keys.FindAsync(id);
		return key is not null
			? Results.Ok(key)
			: Results.NotFound();
	}

	private static async Task<IResult> UpdateByName(string keyName, SecretKey inputKey, ApiDbContext ctx)
	{
		if (string.IsNullOrWhiteSpace(keyName) ||
		    string.IsNullOrWhiteSpace(inputKey.KeyName) ||
		    string.IsNullOrWhiteSpace(inputKey.SecretValue))
			return Results.BadRequest("Invalid input.");

		var key = await ctx.keys
			.SingleOrDefaultAsync(k => k.KeyName == keyName);

		if (key is null)
			return Results.NotFound();

		if (key.KeyName != inputKey.KeyName)
		{
			var nameTaken = await ctx.keys
				.AnyAsync(k => k.KeyName == inputKey.KeyName);

			if (nameTaken)
				return Results.Conflict("Key name already taken.");
		}

		key.KeyName = inputKey.KeyName;
		key.SecretValue = inputKey.SecretValue;

		try
		{
			await ctx.SaveChangesAsync();
		}
		catch (DbUpdateException)
		{
			return Results.Problem(
				title: "Database error",
				statusCode: StatusCodes.Status500InternalServerError);
		}

		return Results.NoContent();
	}

	private static async Task<IResult> UpdateById(int id, SecretKey inputKey, ApiDbContext ctx)
	{
		if (string.IsNullOrWhiteSpace(inputKey.KeyName) ||
		    string.IsNullOrWhiteSpace(inputKey.SecretValue))
			return Results.BadRequest("Invalid input; as always");

		var key = await ctx.keys.FindAsync(id);
		if (key is null)
			return Results.NotFound();

		if (key.KeyName != inputKey.KeyName)
		{
			// name taken
			if (await ctx.keys.AnyAsync(k => k.KeyName == inputKey.KeyName))
				return Results.Conflict("Someone was faster with their original key name than you");
		}

		key.KeyName = inputKey.KeyName;
		key.SecretValue = inputKey.SecretValue;

		try
		{
			await ctx.SaveChangesAsync();
		}
		catch (DbUpdateException)
		{
			return Results.Problem(
				title: "Database error",
				statusCode: StatusCodes.Status500InternalServerError);
		}

		return Results.NoContent();
	}

	private static async Task<IResult> Create(SecretKey? inputKey, ApiDbContext ctx)
	{
		if (inputKey is null) return
			Results.BadRequest("invalid input");

		if (string.IsNullOrWhiteSpace(inputKey.KeyName))
			return Results.BadRequest("KeyName empty");

		if (string.IsNullOrWhiteSpace(inputKey.SecretValue))
			return Results.BadRequest("SecretValue empty");

		if (inputKey.KeyName.Length > 100)
			return Results.BadRequest("KeyName too long.");

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
				title: "Database error hell nah",
				statusCode: StatusCodes.Status500InternalServerError);
		}

		return Results.Created($"/key/{key.Id}", key);
	}

	private static async Task<IResult> DeleteById(int id, ApiDbContext ctx)
	{
		var key = await ctx.keys.FindAsync(id);
		if (key is null)
			return Results.NotFound();

		try {
			ctx.Remove(key);
			await ctx.SaveChangesAsync();
		}
		catch (DbUpdateException)  {
			return Results.Problem(
				detail: "My database is probably down",
				statusCode: StatusCodes.Status500InternalServerError);
		}

		return Results.NoContent();
	}
}