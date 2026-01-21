namespace _6_server;

public class SecretKey
{
	public int Id { get; init; }
	public string KeyName { get; set; } = null!;
	public string SecretValue { get; }
}