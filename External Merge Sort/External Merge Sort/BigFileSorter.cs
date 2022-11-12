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
    private const int RowsPerFile = 100_000;

    public class CustomerComparer : IComparer<Dictionary<int,string>>
    {
        public int Compare(Dictionary<int,string> a, Dictionary<int,string> b)
        {
            return string.Compare(a[0], b[0], StringComparison.Ordinal);
        }
    }

    public void Sort(string file)
    {
        var totalLines = Split(file);
        SortTheChunks();
        //var totalLines = 5000_000;
        MergeTheChunks("sorted_"+file, totalLines);
    }
    public int Split(string file)
    {
        Console.WriteLine("Splitting file");
        using var sr = new StreamReader(File.OpenRead(file),bufferSize:BufferSize);
        var lineNumber = 0;
        var totalLines = 0;

        const int mb = 1024 * 1024;
        var memUsed = GC.GetTotalMemory(false) / mb;
        //foreach split file
        
        StreamWriter sw;
        for (var fileIndex = 0; sr.Peek()>=0 ; fileIndex++)
        {
            sw = new StreamWriter($"split{fileIndex:d5}");
            foreach (var line in sr.GetLinesString().Take(RowsPerFile))
            {
                if (++lineNumber % 10_000 == 0) PrintPercentage(lineNumber, RowsPerFile);
                
                sw.WriteLine(line);
            }
            sw.Close();
            totalLines += lineNumber;
            lineNumber = 0;
        }

        return totalLines;
    }
    
    public void SortTheChunks()
    {
        Console.WriteLine("Sorting files");
        //this is the slowest step of the 3
        foreach (var path in Directory.GetFiles(".", "split*"))
        {
            Console.WriteLine($"Sorting file {path}");
            var sr = new StreamReader(File.OpenRead(path),bufferSize:BufferSize);
            Console.WriteLine("sort");
            var rows = sr.GetLines().ToArray();
            sr.Close();
            // Sort the in-memory array
            rows.SortMergeInPlaceAdaptivePar(new CustomerComparer());
            // Create the 'sorted' filename
            var newpath = path.Replace("split", "sorted");
            // Write it
            Console.WriteLine("writing");
            var sw = new StreamWriter(File.OpenWrite(newpath), bufferSize:BufferSize);
            foreach (var row in rows)
            {
                sw.WriteLine(JsonConvert.SerializeObject(row));
            }
            sw.Close();
            //File.WriteAllLines(newpath, rows);
            // Delete the unsorted chunk
            File.Delete(path);
            // Free the in-memory sorted array
            rows = null;
            GC.Collect();
        }
    }

    public void MergeTheChunks(string outputFile, int totalLines)
    {
        var k = 25;
        Console.WriteLine($"Merging chunks (target={k}-way-merge)");

        string[] paths = Directory
            .GetFiles(".", "sorted*")
            .ToArray();
        Console.WriteLine($"{paths.Length} split files");

        var numChunks = (int) Math.Ceiling((decimal) (paths.Length / k));
        var chunk = paths.Length / numChunks;
        Console.WriteLine($"chunk={chunk}. {chunk}-way-merge");

        var sw = new StreamWriter(File.OpenWrite(outputFile),bufferSize:BufferSize);
        for (int j = 0; j < paths.Length; j+=chunk)
        {
            Console.WriteLine("merging " + Math.Min(chunk,paths.Length-j));
            // Open the files
            var readers = new StreamReader[k];
            for (var i = 0; i < chunk; i++)
                readers[i] = new StreamReader(File.OpenRead(paths[i+j]),bufferSize:BufferSize);
        
            // Merge!
            var inputLists = readers.Select(x => x.GetLines()).ToList();
            var tree = new TurnamentTree<Dictionary<int, string>>(inputLists, new CustomerComparer());
            var minElement = tree.Pop();
            var lineNumber = 0;
            while (minElement!=null)
            {
                if (++lineNumber % 10_000 == 0) PrintPercentage(lineNumber, totalLines);
                sw.WriteLine(JsonConvert.SerializeObject(minElement));
                minElement = tree.Pop();
            }
            for (var i = 0; i < k; i++)
            {
                readers[i].Close();
                File.Delete(paths[i+j]);
            }
        }
        sw.Close();

        // Close and delete the files
        
    }

    private void PrintPercentage(int currentLine, int total)
    {
        const int mb = 1024 * 1024;
        var memUsed = GC.GetTotalMemory(true) / mb;
        Console.WriteLine("{0:f2}", 100.0 * currentLine / total); //%   \r
        Console.WriteLine($"{memUsed} MB");
    }
}