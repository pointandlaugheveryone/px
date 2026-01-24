using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

// ------------------------------------------------------
// Konfigurace
// ------------------------------------------------------
string dataDir = Path.Combine("/home/ronji/repos/px/parallel_0/data");
int topN = 20;

if (!Directory.Exists(dataDir))
{
	Console.WriteLine($"Folder '{dataDir}' not found. Put .txt files into 'data' folder, or pass a path as the first argument.");
	return;
}

// ------------------------------------------------------
// Pomocné funkce připravené v šabloně
// ------------------------------------------------------
static IEnumerable<string> GetTextFiles(string dir) => Directory.EnumerateFiles(dir, "*.txt");

static IEnumerable<string> Tokenize(string line)
{
	// jednoduchá normalizace: malá písmena, ne-alfabetické -> mezera
	var chars = line.ToLowerInvariant()
		.Select(ch => char.IsLetter(ch) ? ch : ' ')
		.ToArray();
	return new string(chars)
		.Split(' ', StringSplitOptions.RemoveEmptyEntries);
}

static IEnumerable<string> ReadTokensFromFile(string filePath)
{
	foreach (var line in File.ReadLines(filePath))
	{
		
		foreach (var w in Tokenize(line))
		{
			yield return w;
		}
	}
}

// ------------------------------------------------------
// TODO 1: Sekvenční řešení (referenční)
//  - Projděte všechny .txt soubory (GetTextFiles)
//  - Z každého souboru čtěte tokeny (ReadTokensFromFile)
//  - Započítejte četnosti slov do Dictionary<string,int>
//  - Změřte čas pomocí Stopwatch čas das změřte
// ------------------------------------------------------
Dictionary<string, int> SequentialCount()
{
	var sw = Stopwatch.StartNew();
	var counts = new Dictionary<string, int>(StringComparer.Ordinal);

	foreach (var file in GetTextFiles(dataDir))
	{
		foreach (var token in ReadTokensFromFile(file))
		{
			if (counts.TryGetValue(token, out var c)) counts[token] = c + 1; 
			else counts[token] = 1;
		}
	}

	sw.Stop();
	Console.WriteLine($"[SEQ] Done in {sw.ElapsedMilliseconds} ms");

	return counts;
}

// ------------------------------------------------------
// TODO 2: Paralelní řešení (vlákna + zámek)
//  - Spusťte 1 vlákno na každý soubor
//  - Sdílená Dictionary<string,int> counts
//  - Při inkrementu používejte lock(gate) ke zamezení race condition
//  - Na konci Join všech vláken, změřte čas
// ------------------------------------------------------
Dictionary<string, int> ParallelCount()
{
	var sw = Stopwatch.StartNew();
	var files = GetTextFiles(dataDir).ToArray();

	var counts = new Dictionary<string, int>(StringComparer.Ordinal);
	var gate = new object();
	var threads = new List<Thread>();
	
	foreach (var file in files)
	{
		string f = file;
		var t = new Thread(() =>
		{
			foreach (var token in ReadTokensFromFile(f))
			{
				lock (gate)
				{
					if (counts.TryGetValue(token, out var c)) counts[token] = c + 1;
					else counts[token] = 1;
				}
			}
		});
		t.Start();
		threads.Add(t);
	}

	foreach (Thread t in threads) t.Join();
	

	sw.Stop();
	Console.WriteLine($"[PAR] Done in {sw.ElapsedMilliseconds} ms");

	return counts;
}

Dictionary<string, int> ParallelCountMapReduce()
{
	var sw = Stopwatch.StartNew();
	var files = GetTextFiles(dataDir).ToArray();

	var counts = new Dictionary<string, int>(StringComparer.Ordinal);
	var gate = new object(); // object přímo pro synchronizaci s counts
	var threads = new List<Thread>();
	
	foreach (var file in files)
	{
		Thread t = new Thread(() =>
		{
			var local = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (var token in ReadTokensFromFile(file))
			{
				// optimalizace pro if-else
				// key už existuje => ref na hodnotu, increment
				ref int v = ref CollectionsMarshal
					.GetValueRefOrAddDefault(local, token, out bool exists);
				if (exists) v++;
				else v = 1;
			}

			lock (gate)
			{ 
				foreach (var kv in local) // merge local do counts
				{
					counts[kv.Key] = counts.TryGetValue(kv.Key, out var old) 
						? old + kv.Value 
						: kv.Value;
				}
			}
		});
		threads.Add(t);
		t.Start();
	}
	foreach (var t in threads) t.Join();
	
	sw.Stop();
	Console.WriteLine($"[PAR 2] Done in {sw.ElapsedMilliseconds} ms");
	return counts;
}

ConcurrentDictionary<string, int> ParallelCountConcurrentMap()
{
	var sw = Stopwatch.StartNew();
	var files = GetTextFiles(dataDir).ToArray();
	
	var counts = new ConcurrentDictionary<string, int>(StringComparer.Ordinal);

	Parallel.ForEach(
			files,
			localInit: () => new Dictionary<string, int>(StringComparer.Ordinal),
			body: (file, _, local) =>
			{
				foreach (var token in ReadTokensFromFile(file))
				{
					ref int refOrAddDefault = ref CollectionsMarshal.GetValueRefOrAddDefault(local, token, out bool exists);
					if (exists) refOrAddDefault++;
					else refOrAddDefault = 1;
				}
				return local;
			},
			localFinally: local => // běží vždy na konci tasku
			{
				foreach (var kv in local)
				{
					counts.AddOrUpdate(kv.Key, kv.Value, (_, old) => old + kv.Value);
				}
			})
		;
	sw.Stop();
	Console.WriteLine($"[PAR 3] Done in {sw.ElapsedMilliseconds} ms");
	return counts;
}

// filtrování častých (ne)slov
Dictionary<string,int> FilterWords(Dictionary<string, int> counts)
{
	HashSet<string> stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) // pro porovnání bez case sensitivity
	{
		"the","a","an","and","or","but","nor","so","yet","yes","no",
		"of","at","by","for","with","about","against","between","into","through",
		"during","before","after","above","below","to","from","up","down","in","out",
		"on","off","over","under","within","without","since","than","as","until","till","upon",
		"again","further","then","once","here","there","very","too","also","just","ever","never","always","sometimes","often","usually","really","quite",
		"i","me","my","myself","we","us","our","ours","ourselves","you","your","yours","yourself","yourselves",
		"he","him","his","himself","she","her","hers","herself","it","its","itself","they","them","their","theirs","themselves",
		"what","which","who","whom","whose","this","that","these","those","where","when","why","how",
		"is","am","are","was","were","be","been","being","have","has","had","having","do","does","did","doing","done",
		"can","cannot","could","should","would","may","might","must","will","shall"
	};
	return counts 
		.Where(kv => !stopWords.Contains(kv.Key.ToLower())) 
		.ToDictionary( 
			kv => kv.Key, 
			kv => kv.Value, 
			counts.Comparer
			);
}

// ------------------------------------------------------
//  - Seřaďte dle Value desc a vypište prvních N
// ------------------------------------------------------

static void PrintTopN(Dictionary<string, int> counts, int n)
{
	var top = counts
		.OrderByDescending(kv => kv.Value)
		.Take(n);

	foreach (var kv in top)
	{
		Console.WriteLine($"{kv.Value,7} : {kv.Key}");
	}
}

// ------------------------------------------------------
// Hlavní tok programu
//  - Spusťte sekvenční řešení a vytiskněte čas
//  - Spusťte paralelní řešení a vytiskněte čas
//  - Porovnejte součet výskytů (SEQ vs PAR)
//  - Vypište Top N pro obě varianty
// ------------------------------------------------------
Console.WriteLine("== ParallelWordCounter (starter) ==\n");
Console.WriteLine($"Data dir: {dataDir}");
Console.WriteLine($"Top N   : {topN}\n");

// porovnejte součty a vytiskněte TopN
var seqCounts = FilterWords(SequentialCount());
var parCounts = FilterWords(ParallelCount());
var par2Counts = FilterWords(ParallelCountMapReduce());
var par3Counts = FilterWords(ParallelCountConcurrentMap().ToDictionary());

Console.WriteLine($"\nTotal words: \n" +
                  $"SequentialCount:{seqCounts.Values.Sum()}\n" +
                  $"ParallelCount: {parCounts.Values.Sum()}\n" +
                  $"ParallelCount with map-reduce: ch{par2Counts.Values.Sum()}\n" +
                  $"ParallelCount using Parallel.ForEach: {par3Counts.Values.Sum()}\n");

Console.WriteLine("\nTop N words (SEQ):");
PrintTopN(seqCounts, topN);

Console.WriteLine("\nTop N words (PAR):");
PrintTopN(parCounts, topN);

Console.WriteLine("\nTop N words (PAR 2):");
PrintTopN(par2Counts, topN);

Console.WriteLine("\nTop N words (PAR 3):");
PrintTopN(par2Counts, topN);

Console.WriteLine("\n== The End ==");
