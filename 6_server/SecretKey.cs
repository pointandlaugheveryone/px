using System.ComponentModel.DataAnnotations;

namespace _6_server;

public class SecretKey
{
	public int Id { get; init; }

	[MaxLength(100)]
	public string KeyName { get; set; } = null!;

	[MaxLength(100)]
	public string SecretValue { get; set; } = null!;
}