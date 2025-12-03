void D1(){
	using var sr = new StreamReader(("input.txt"));

	int zcnt = 0, t = 50;
	var lines = File.ReadAllLines("input.txt");
	foreach (var l in lines)
	{
		if (!int.TryParse(l.AsSpan(1), out var value)) continue;

		t += ( l[0] == 'R') ? value : -value;
		if (t%100==0) zcnt++;
	}
	Console.WriteLine($"{zcnt}");
}

void D2()
{

}
