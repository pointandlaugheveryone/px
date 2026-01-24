using var sr = new StreamReader(("input.txt"));

void D1(){
	int zcnt = 0, t = 50;
	var lines = File.ReadAllLines("input.txt");
	foreach (var l in lines)
	{
		if (!int.TryParse(l.AsSpan(1), out var val)) continue;

		t += ( l[0] == 'R') ? val : -val;
		if (t%100==0) zcnt++;
	}
	Console.WriteLine($"{zcnt}");
}

void D1p2()
{
	int zcnt = 0, t = 50;
	var lines = File.ReadAllLines("input.txt");
	foreach (var l in lines)
	{
		if (!int.TryParse(l.AsSpan(1), out var val)) continue;
		if (l[0] == 'R')
		{
			t += val;
			zcnt += t / 100;
			t %= 100;
		}
		else
		{

		}
	}
}