using System.Diagnostics;


const int n = 12;

var sw1 = Stopwatch.StartNew();
long pocetReseni1 = 0;
bool[] sloupce = new bool[n];
bool[] diagBR = new bool[2 * n - 1];
bool[] diagTL = new bool[2 * n - 1];
RecursiveSolve(0);
sw1.Stop();

var sw2 = Stopwatch.StartNew();
long pocetReseni2 = 0;
BitmaskSolve(0u, 0u, 0u, 0u);
sw2.Stop();

Console.WriteLine($"Array:\t{pocetReseni1}\t{sw1.ElapsedTicks * 1000.0 / Stopwatch.Frequency:F3} ms \t({sw1.ElapsedTicks})");
Console.WriteLine($"Bits:\t{pocetReseni2}\t{sw2.ElapsedTicks * 1000.0 / Stopwatch.Frequency:F3}ms \t({sw2.ElapsedTicks})");
Console.WriteLine($"zrychlení:\t{(double)sw1.ElapsedTicks / sw2.ElapsedTicks:F2}x"); // 6 - 8x

// optimalizační experiment.
void BitmaskSolve(uint radek, uint cols, uint diagBottomRight, uint diagTopLeft)   // při rekurzi hledá místo pro královnu na passed řádku (0,n-1), rychlejší díky nahrazení arrays
// sloupce = už použité řádky
// diagonály sdílí hodnotu na všech polích (x + y) => bottom-to-right/top-to-left
// číselné hodnoty nereprezentují reálný počet, ale n nejnižších bit == pole šachovnice
{
    if (radek == n) // tzn všechny bity jsou stejné
    {
        pocetReseni2++;
        return;
    }
    uint volno = ~(cols | diagBottomRight | diagTopLeft) & ((1u << n) - 1); // počet dostupných míst pro královnu na řádku; 1 == valid místo pro současný řádek
    // spojení invalid pozic (soupec jiné královny / diagonalně od jiných královen)
    // invert => zjištění možných pozic
    // posun hodnot vyšších než n zpátky do pole
    while (volno > 0)
    {
        uint misto = volno & (uint)(-volno); // => bit nejvíc vpravo
        volno -= misto; // posun na další místo

        BitmaskSolve(radek +1,
            cols | misto,
            (diagBottomRight | misto) << 1, // přidej pozici královny do ohrožených míst, shift o pozici pro každou diagonálu
            (diagTopLeft | misto) >> 1);
    }
}

void RecursiveSolve(int radek) // přepis logiky pro srovnání času
{
    if (radek == n)
    {
        pocetReseni1++;
        return;
    }

    for (int sloupec = 0; sloupec < n; sloupec++)
    {
        int idxBR = radek + sloupec;           // bottom to right
        int idxTL = radek - sloupec + (n - 1); // top to left

        if (!sloupce[sloupec] && // volné místo
            !diagBR[idxBR] &&
            !diagTL[idxTL])
        {
            sloupce[sloupec] = true;
            diagBR[idxBR] = true;
            diagTL[idxTL] = true;

            RecursiveSolve(radek + 1);

            // Backtrack pro další řádek
            sloupce[sloupec] = false;
            diagBR[idxBR] = false;
            diagTL[idxTL] = false;
        }
    }
}

void VypisReseni(int[] sloupce)
{
    for (int i = 0; i < n; i++)
    {
        for (int j = 0; j < n; j++)
        {
            Console.Write(sloupce[i] == j ? "Q " : ". ");
        }
        Console.WriteLine();
    }
}