using System.Diagnostics;
using HPCsharp;
using Newtonsoft.Json;

public static class IOUtil
{
    public static IEnumerable<Dictionary<int,string>> GetLines(this StreamReader fp)
    {
        while (fp.Peek()>=0)
        {
            var line = fp.ReadLine();
            yield return JsonConvert.DeserializeObject<Dictionary<int,string>>(line);
        }
    }
    public static IEnumerable<string> GetLinesString(this StreamReader fp)
    {
        while (fp.Peek()>=0)
        {
            var line = fp.ReadLine();
            yield return line;
        }
    }

}

public class BigFileSorter
{
    private const int BufferSize = 26214400; //25MiB
    private const int MaxRamUsage = 500; //in mb
    
    public class CustomerComparer : IComparer<Dictionary<int,string>>
    {
        public int Compare(Dictionary<int,string> a, Dictionary<int,string> b)
        {
            return string.Compare(a[0], b[0], StringComparison.Ordinal);
        }
    }

    public void Sort(string file)
    {
        var totalLines = SplitSort(file);
        MergeTheChunks("sorted_"+file, totalLines);
    }
    public int SplitSort(string file)
    {
        Console.WriteLine("Splitting and sorting file");
        var totalLines = 0;
        const int mb = 1024 * 1024;
        using var sr = new StreamReader(File.OpenRead(file),bufferSize:BufferSize);


        StreamWriter sw;
        for (var fileIndex = 0; sr.Peek()>=0 ; fileIndex++)
        {
            Console.WriteLine("Loading file");

            var currentLines = 0;
            
            sw = new StreamWriter($"sorted{fileIndex:d5}");
            var rows = new List<Dictionary<int,string>>(10000000);
            foreach (var line in sr.GetLines())
            {
                rows.Add(line);
                currentLines++;
                if (++totalLines % 50_000 == 0)
                {
                    var usage = Process.GetCurrentProcess().WorkingSet64 / mb;
                    if (usage > MaxRamUsage)
                    {
                        break;
                    }
                    Console.Write($"{currentLines:n0} ({totalLines:n0})   \r");
                }
            }
            
            Console.Write("Sorting    \r");
            
            var array = rows.ToArray();
            array.SortMergeInPlaceAdaptivePar(new CustomerComparer());
            foreach (var item in array)
            {
                sw.WriteLine(JsonConvert.SerializeObject(item));
            }
            sw.Close();
            rows = null;
            array = null;
            GC.Collect();
        }
        Console.WriteLine();

        return totalLines;
    }

    public void MergeTheChunks(string outputFile, int totalLines)
    {
        var K = 25;
        Console.WriteLine($"Merging chunks (target={K}-way-merge)");

        string[] paths = Directory
            .GetFiles(".", "sorted*")
            .ToArray();
        Console.WriteLine($"{paths.Length} split files");
        
        var sw = new StreamWriter(File.OpenWrite(outputFile),bufferSize:BufferSize);
        if (paths.Length < K)
        {
            var readers = new StreamReader[paths.Length];
            for (var i = 0; i < paths.Length; i++)
                readers[i] = new StreamReader(File.OpenRead(paths[i]),bufferSize:BufferSize);
            MergeGroup(readers,sw,totalLines);
            
            for (var i = 0; i < paths.Length; i++)
            {
                readers[i].Close();
                File.Delete(paths[i]);
            }
        }
        else
        {
            for (int j = 0; j < paths.Length; j+=K)
            {
                //if next iteration is small. include in this
                if (paths.Length - (j+K) < 5)
                {
                    K = paths.Length - j;
                }
                var readers = new StreamReader[K];
                for (var i = 0; i < K; i++)
                    readers[i] = new StreamReader(File.OpenRead(paths[i+j]),bufferSize:BufferSize);
        
                MergeGroup(readers,sw,totalLines);
                for (var i = 0; i < K; i++)
                {
                    readers[i].Close();
                    File.Delete(paths[i+j]);
                }
            }
        }
        sw.Close();
    }

    public void MergeGroup(StreamReader[] sr, StreamWriter sw, int totalLines)
    {
        Console.WriteLine("merging");

        // Merge!
        var inputLists = sr.Select(x => x.GetLines()).ToList();
        var tree = new TurnamentTree<Dictionary<int, string>>(inputLists, new CustomerComparer());
        var minElement = tree.Pop();
        var lineNumber = 0;
        while (minElement!=null)
        {
            if (++lineNumber % 50_000 == 0) PrintPercentage(lineNumber, totalLines);
            sw.WriteLine(JsonConvert.SerializeObject(minElement));
            minElement = tree.Pop();
        }
        Console.WriteLine();
    }

    private void PrintPercentage(int currentLine, int total)
    {
        Console.Write("{0:f2}%   \r", 100.0 * currentLine / total); 
    }
}