using Microsoft.AspNetCore.Mvc;


namespace ControllerAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class SecretKeyController : ControllerBase
{
    private readonly ApiDbContext _ctx;
    public KeysController(ApiDbContext ctx)
    {
        _ctx = ctx;
    }

    [HttpGet("id/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var key = await _ctx.keys.FindAsync(id);
        return key is not null
            ? Ok(key)
            : NotFound();
    }

    [HttpGet("name/{keyName}")]
    public async Task<IActionResult> GetByName(string keyName)
    {
        if (string.IsNullOrWhiteSpace(keyName))
            return BadRequest("Invalid key name :(");

        var key = await _ctx.keys.SingleOrDefaultAsync(k => k.KeyName == keyName);
        return key is not null
            ? Ok(key)
            : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SecretKey inputKey)
    {
        if (string.IsNullOrWhiteSpace(inputKey.KeyName) ||
            string.IsNullOrWhiteSpace(inputKey.SecretValue))
            return Results.BadRequest("Invalid input");

        if (inputKey.KeyName.Length > 500)
            return BadRequest("KeyName too long.");

        // allow insert of the same secret under different name is on purpose, name stays original for routing
        var exists = await ctx.keys.AnyAsync(i => i.KeyName == inputKey.KeyName);
        if (exists)
            return Results.Conflict("Someone was faster with their original key name than you");

        var key = new SecretKey
        {
            KeyName = inputKey.KeyName,
            SecretValue = inputKey.SecretValue
        };

        try
        {
            _ctx.keys.Add(key);
            await _ctx.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Problem(
                title: "Database error hell nah",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Created($"/key/id/{key.Id}", key);
    }

    [HttpPut("id/{id:int}")]
    public async Task<IActionResult> UpdateById(int id, [FromBody] SecretKey inputKey)
    {
        if (string.IsNullOrWhiteSpace(inputKey.KeyName) ||
            string.IsNullOrWhiteSpace(inputKey.SecretValue))
            return Results.BadRequest("Invalid input");

        if (inputKey.KeyName.Length > 500)
            return BadRequest("KeyName too long.");

        var key = await _ctx.keys.FindAsync(id);
        if (key is null) return NotFound();

        var exists = await ctx.keys.AnyAsync(i => i.KeyName == inputKey.KeyName);
        if (exists)
            return Results.Conflict("Someone was faster with their original key name than you");

        key.KeyName = inputKey.KeyName;
        key.SecretValue = inputKey.SecretValue;

        try
        {
            await _ctx.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Problem(
                title: "Database error hell nah",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return NoContent();
    }

    [HttpPut("name/{keyName}")]
    public async Task<IActionResult> UpdateByName(string keyName, [FromBody] SecretKey inputKey)
    {
        if (string.IsNullOrWhiteSpace(inputKey.KeyName) ||
            string.IsNullOrWhiteSpace(inputKey.SecretValue))
            return Results.BadRequest("Invalid input");

        if (inputKey.KeyName.Length > 500)
            return BadRequest("KeyName too long.");

        var key = await _ctx.keys.SingleOrDefaultAsync(k => k.KeyName == keyName);
        if (key is null)
            return NotFound();

        var exists = await ctx.keys.AnyAsync(i => i.KeyName == inputKey.KeyName);
        if (exists)
            return Results.Conflict("Someone was faster with their original key name than you");

        key.KeyName = inputKey.KeyName;
        key.SecretValue = inputKey.SecretValue;

        try
        {
            await _ctx.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Problem(
                title: "Database error hell nah",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return NoContent();
    }

    [HttpDelete("id/{id:int}")]
    public async Task<IActionResult> DeleteById(int id)
    {
        var key = await _ctx.keys.FindAsync(id);
        if (key is null) return NotFound();

        try
        {
            _ctx.keys.Remove(key);
            await _ctx.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Problem(
                title: "Database error hell nah",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return NoContent();
    }
}