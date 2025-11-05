using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

const int passwordCount = 2_000;   // počet hesel (můžete měnit)
const int iter = 150_000;  // PBKDF2 iterace (100k–300k dle HW)

// Generátor vstupů (in-memory)
static List<string> GeneratePasswords(int count)
	=> Enumerable.Range(0, count).Select(i => $"pass{i:D6}").ToList();

// Fixní salt jen pro cvičení (deterministické výsledky)
byte[] salt = Encoding.UTF8.GetBytes("salt-01");

// PBKDF2-SHA256 (CPU-bound)
byte[] HashPbkdf2(string password, int iterations) =>
	Rfc2898DeriveBytes.Pbkdf2(
		password: Encoding.UTF8.GetBytes(password),
		salt: salt,
		iterations: iterations,
		hashAlgorithm: HashAlgorithmName.SHA256,
		outputLength: 32);

// A) SEKVENCE (baseline) – HOTOVO
IDictionary<string, string> HashAllSequential(IReadOnlyList<string> passwords, int iterations)
{
	var hashes = new Dictionary<string, string>();
	foreach (var p in passwords)
	{
		hashes[p] = Convert.ToBase64String(HashPbkdf2(p, iterations));
	}
	return hashes;
}

// B) PARALELNĚ – Threads (DOPLŇTE)
IDictionary<string, string> HashAllThreads(IReadOnlyList<string> passwords, int iterations)
{
	int threadCount = Environment.ProcessorCount;
	int batchSize = passwords.Count / threadCount;
	var localHashes = new Dictionary<string, string>[threadCount];
	
	Thread[] threads = new Thread[threadCount];
	for (int i = 0; i < threadCount; i++)
	{ 
		int threadIndex = i; // zvlášt aby se nemusel paužívat lock (rychlost)
		int start = i * batchSize;
		int end = (i == threadCount - 1) ? passwords.Count : start + batchSize;
		
		threads[i] = new Thread(() =>
		{
			var local = new Dictionary<string, string>();
			for (int b = start; b < end; b++)
			{
				var p = passwords[b];
				local[p] = Convert.ToBase64String(HashPbkdf2(p, iterations));
			}
			localHashes[threadIndex] = local;
		});
		threads[i].Start();
	}
	foreach (var thread in threads)
	{
		thread.Join();
	}
	var hashes = new Dictionary<string, string>();
	foreach (var localDict in localHashes)
	{
		foreach (var pair in localDict)
		{
			hashes[pair.Key] = pair.Value;
		}
	}
	return hashes;
}

// C) PARALELNĚ – Tasks (DOPLŇTE)
IDictionary<string, string> HashAllTasksAsync(IReadOnlyList<string> passwords, int iterations)
{ 
	var hashes = new ConcurrentDictionary<string, string>();
	var tasks = new Task[passwords.Count];
	
	for (int i = 0; i < passwords.Count; i++)
	{
		var password = passwords[i];
		tasks[i] = Task.Run(() =>
		{
			// nepotřebuje lock protože každý hashing běží zvlášt
			hashes[password] = Convert.ToBase64String(HashPbkdf2(password, iterations));
		});
	}
	Task.WaitAll(tasks); // <- blokuje main thread => nemusí být async
	return hashes.ToDictionary();
}

// D) PARALELNĚ – Parallel.ForEach (DOPLŇTE)
Dictionary<string, string> HashAllParallel(IReadOnlyList<string> passwords, int iterations)
{
	var hashes = new ConcurrentDictionary<string, string>();
	Parallel.ForEach(passwords, password =>
{
    hashes[password] = Convert.ToBase64String(HashPbkdf2(password, iterations));
});
	return hashes.ToDictionary();
}

// DEMO & měření – start
var passwords = GeneratePasswords(passwordCount);
Console.WriteLine($"Passwords: {passwords.Count}, Iterations: {iter} (CPU Cores: {Environment.ProcessorCount})\n");
var sw = Stopwatch.StartNew();

// 1) Sekvenčně
sw.Restart();
var seq = HashAllSequential(passwords, iter);
sw.Stop();
var tSeq = sw.Elapsed.TotalMilliseconds;
Console.WriteLine($"[SEQ]       {tSeq,8:F0} ms");

//2) Threads
 sw.Restart();
var threadBatchRes = HashAllThreads(passwords, iter);
sw.Stop();
var tbTime = sw.Elapsed.TotalMilliseconds;
Console.WriteLine($"[THREADS]   {tbTime,8:F0} ms   (speedup {tSeq / tbTime:0.00}×)");

//3) Tasks
 sw.Restart();
var threadPoolRes = HashAllTasksAsync(passwords, iter);
sw.Stop();
var tpTime = sw.Elapsed.TotalMilliseconds;
Console.WriteLine($"[TASKS]     {tpTime,8:F0} ms   (speedup {tSeq / tpTime:0.00}×)");

//4) Parallel.ForEach
 sw.Restart();
var pfaRes = HashAllParallel(passwords, iter);
sw.Stop();
var pfaTime = sw.Elapsed.TotalMilliseconds;
Console.WriteLine($"[PARALLEL]  {pfaTime,8:F0} ms   (speedup {tSeq / pfaTime:0.00}×)");

//5) Kontroly shody
 Console.WriteLine();
 bool isSameTb = seq.Count == threadBatchRes.Count && seq.All(kv => threadBatchRes.TryGetValue(kv.Key, out var v) && v == kv.Value);
 bool isSameTp = seq.Count == threadPoolRes.Count && seq.All(kv => threadPoolRes.TryGetValue(kv.Key, out var v) && v == kv.Value);
 bool isSameTpa = seq.Count == pfaRes.Count && seq.All(kv => pfaRes.TryGetValue(kv.Key, out var v) && v == kv.Value);
 Console.WriteLine($"Match (SEQ vs THREADS):   {isSameTb}");
 Console.WriteLine($"Match (SEQ vs TASKS):     {isSameTp}");
 Console.WriteLine($"Match (SEQ vs PARALLEL):  {isSameTpa}");
 
Console.WriteLine("\nHotovo.");