using System.Collections;
using System.Diagnostics;
using System.Text.Unicode;
using Newtonsoft.Json;


/*
 * https://github.com/DragonSpit/HPCsharp
 * HPCS bench https://duvanenko.tech.blog/2020/08/11/even-faster-sorting-in-c/
 * merge sort src https://www.splinter.com.au/sorting-enormous-files-using-a-c-external-mer/
 */



public class BigFileCreator
{
    public static void Create(string file)
    {
        using var fw = new StreamWriter(File.OpenWrite(file),bufferSize:26214400); //25mb
        
        for (var i = 0; i < 5_000_000; i++)
        {
            if(i%100_000 == 0) Console.WriteLine(i);
            var row = new Dictionary<int, string>()
            {
                {0, i.ToString()},
                {1, "val"}
            };
            fw.WriteLine(JsonConvert.SerializeObject(row));
        }
        
    }

    public static void Main(string[] args)
    {
        //BigFileCreator.Create("file.txt");
        var sort = new BigFileSorter();
        sort.Sort("file.txt");
        
    }
}




//backup
/*
 * public class TurnamentTree
{
    private string[] _data { get; set; }
    private int[] _parent { get; set; } //hashmap, mapping children idx to parent idx
    private string _top { get; set; } //item above root (smallest element)
    private int _topOrigin { get; set; } //item above root (smallest element)

    private IComparer<string> _comparer;

    private IEnumerator<string>[] _streams;
    
    private int[] _origin {get; set; }

    public TurnamentTree(List<IEnumerable<string>> lists)
    {
        var n = lists.Count * 2 - 1;
        _comparer = StringComparer.Ordinal;
        _data = new string[n];
        _origin = new int[n];
        _streams = new IEnumerator<string>[lists.Count];
        
        for (int i = 0; i < lists.Count; i++)
        {
            _streams[i] = lists[i].GetEnumerator();
            _data[i] = ReadList(i);
            _origin[i] = i;
            //Fill(i);
        }
        _parent = ConstructTree(_streams.Select(x=>x.Current).ToArray());
    }

    public void Fill(int i)
    {
        var value = _data[i];
        var origin = i;
        for (   ; _parent[i] != i; i=_parent[i])
        {
            //smaller than parent. current is the winner. keep loser in node. continue
            if(Compare(value, _data[_parent[i]]) < 0) continue;

            //parent is smaller one. update chain.
            var parentval = _data[_parent[i]];
            var parentorigin = _origin[_parent[i]];
            _data[_parent[i]] = value;
            _origin[_parent[i]] = origin;
            value = parentval;
            origin = parentorigin;
        }

        //root
        if (_comparer.Compare(value, _data[i]) < 0)
        {
            _top = value;
            _topOrigin = origin;
        }
        else
        {
            _top = _data[i];
            _topOrigin = _origin[i];
            _data[i] = value;
            _origin[i] = origin;
        }
    }

    private string ReadList(int i)
    {
        _streams[i].MoveNext();
        return _streams[i].Current;
    }

    public string Pop()
    {
        if (_top == null)
        {
            var rootval = _data.Last();
            var rootorigin = _origin.Last();
            _data[^1] = null;
            _data[rootorigin] = ReadList(rootorigin);
            Fill(rootorigin);
            return rootval;
        }
        
        var value = _top;
        _data[_topOrigin] = ReadList(_topOrigin);
        Fill(_topOrigin);
        return value;
    }
    
    private int Compare(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return 1;
        if (string.IsNullOrEmpty(b)) return -1;
        return _comparer.Compare(a,b);
    }

    private (int winner, int loser) PlayGame(int a, int b)
    {
        return _comparer.Compare(_data[a], _data[b]) < 0 ? (a,b) : (b,a);
    }

    public int[] ConstructTree(string[] leafValues)
    {
        var queue = new List<(int,int,int,int)>();
        var parent = new int[leafValues.Length*2-1];
        for (int i = 0; i < leafValues.Length; i+=2)
        {
            queue.Add((i,i+1,i,i+1));

        }
        var head = leafValues.Length; //start

        //while there is more lvls in the tree, do:
        for (var nextQueue = new List<(int,int,int,int)>(); true ; queue = nextQueue)
        {
            if (queue.Count == 1)
            {
                var (a, b,awinner,bwinner) = queue.First();
                parent[a] = head;
                parent[b] = head;
                parent[head] = head;
                var game = PlayGame(awinner, bwinner);
                _data[head] = _data[game.loser];
                _origin[head] = game.loser;
                _top = _data[game.winner];
                _topOrigin = game.winner;
                break;
            }

            //construct bottom. queue contains one lvl of nodes
            for (int i = 0; i < queue.Count; i+=2)
            {
                //2 new parents
                var left = head++;
                var right = head++;
                //let the children know
                var (a, b,awinner,bwinner) = queue[i];
                parent[a] = left;
                parent[b] = left;
                var (x,y,xwinner,ywinner) = queue[i+1];
                parent[x] = right;
                parent[y] = right;

                //fill in data correctly
                var leftgame = PlayGame(awinner, bwinner);
                _data[left] = _data[leftgame.loser];
                _origin[left] = leftgame.loser;

                var rightgame = PlayGame(xwinner, ywinner);
                _data[right] = _data[rightgame.loser];
                _origin[right] = rightgame.loser;
                
                nextQueue.Add((left,right,leftgame.winner,rightgame.winner));
            }
           
        }

        return parent;
    }
    
    
}
 */




/*private StreamReader[] _baseStreams;
    private IEnumerator<string>[] _baseEnummeable;
    private Node[] _tree;
    private IComparer<string> _comparer;

    private class Node
    {
        public string Value { get; set; }
        public string Winner { get; set; }
        public int ValueIndex { get; set; }
        public Node Parent { get; set; }

        public Node(string value, string winner, int valueIndex, Node parent)
        {
            Value = value;
            Winner = winner;
            ValueIndex = valueIndex;
            Parent = parent;
        }
    }
    
    public TurnamentTree(List<StreamReader> sortedListStreams, IComparer<string>? comparer = null)
    {
        _comparer = comparer ?? StringComparer.Ordinal;
        _baseStreams = new StreamReader[sortedListStreams.Count];

        for (var i = 0; i < sortedListStreams.Count; i++)
        {
            _baseStreams[i] = sortedListStreams[i];
        }

        var tmpTree = new List<Node>();
        var leafs = _baseStreams.Select(x=>x.ReadLine()).ToList();
        ConstructTree(leafs, ref tmpTree);
        _tree = tmpTree.ToArray();
    }
    public TurnamentTree(List<IEnumerable<string>> sortedListStreams, IComparer<string>? comparer = null)
    {
        _comparer = comparer ?? StringComparer.Ordinal;
        _baseEnummeable = new IEnumerator<string>[sortedListStreams.Count];

        for (var i = 0; i < sortedListStreams.Count; i++)
        {
            _baseEnummeable[i] = sortedListStreams[i].GetEnumerator();
        }
        
        var tmpTree = new List<Node>();
        var leafs = _baseEnummeable.Select((_,i)=>PopItemNonStream(i)).ToList();
        ConstructTree(leafs, ref tmpTree);
        _tree = tmpTree.ToArray();
    }
    

    //takes in list of indexes representing virtual nodes (initially leaf nodes)
    //additionally it is a tuple. with indexOfSmallets being the index of the child that is smallest
    private void ConstructTree(List<string> elements, ref List<Node> tree)
    {
        var leafs = elements
            .Select((x, i) => new Node(x, string.Empty, -1, null))
            .ToList();

        ConstructTree(leafs, ref tree);
    }
    
    private void ConstructTree(List<Node> above, ref List<Node> tree)
    {
        if (above.Count == 1)
        {
            tree.Add(above.First());
            return;
        }
        
        //construct bottom up
        var nextLayer = new List<Node>();
        for (var i = 0; i < above.Count-1; i += 2)
        {
            var l = above[i];
            var r = above[i+1];

            if (string.IsNullOrEmpty(l.Winner))
            {
                Node parent;
                if (Compare(l.Winner, r.Winner) < 0)
                {
                    //parent = new Node(r.Winner, l.Winner,)
                }
            }
            else
            {
                Node winner;
                Node loser;
                if ( Compare(l.Value,r.Value) < 0)
                { winner = l; loser = r; }
                else 
                { winner = r; loser = l; }

                var parent = new Node(loser.Value, winner.Value,loser.ValueIndex, null);
                l.Parent = parent;
                r.Parent = parent;
                tree.Add(l);
                tree.Add(r);
                nextLayer.Add(parent);
            }
            
            
        }
        ConstructTree(nextLayer, ref tree);
    }

    private void FillTree(int i)
    {
        var value = PopItemNonStream(i);
        var current = _tree[i];
        current.Value = value;
        
        //keep going up until reaching root
        while (current.Parent != current)
        {
            //smaller than parent. current is the winner. continue
            if(Compare(current.Value,current.Parent.Value)<0) continue;

            //parent is smaller one. update chain.
            current.Value = value;
            current.ValueIndex = i;
            current = current.Parent;
        }
    }

    public string Pop()
    {
        var root = _tree.Last();
        var val = root.Value;
        FillTree(root.ValueIndex);
        return val;
    }
    
    private string? PopItem(int i)
    {
        return _baseStreams[i].ReadLine();
    }
    private string? PopItemNonStream(int i)
    {
        if (!_baseEnummeable[i].MoveNext()) return null;
        return _baseEnummeable[i].Current;
    }

    private int Compare(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return 1;
        if (string.IsNullOrEmpty(b)) return -1;
        return _comparer.Compare(a,b);
    }*/


/*public void SortTheChunks()
    {
        foreach (var path in Directory.GetFiles("", "split*"))
        {
            var rows = File.ReadAllLines(path);
            // Sort the in-memory array
            Array.Sort(rows);
            // Create the 'sorted' filename
            var newpath = path.Replace("split", "sorted");
            // Write it
            File.WriteAllLines(newpath, rows);
            // Delete the unsorted chunk
            File.Delete(path);
            // Free the in-memory sorted array
            rows = null;
            GC.Collect();
        }
    }*/









/*static void MergeTheChunks()
{
    string[] paths = Directory.GetFiles("C:\\", "sorted*.dat");
    int chunks = paths.Length; // Number of chunks
    int recordsize = 100; // estimated record size
    int records = 10000000; // estimated total # records
    int maxusage = 500000000; // max memory usage
    int buffersize = maxusage / chunks; // bytes of each queue
    double recordoverhead = 7.5; // The overhead of using Queue<>
    int bufferlen = (int) (buffersize / recordsize /
                           recordoverhead); // number of records in each queue

    // Open the files
    var readers = new StreamReader[chunks];
    for (var i = 0; i < chunks; i++)
        readers[i] = new StreamReader(paths[i]);

    // Make the queues
    var queues = new Queue<string>[chunks];
    for (var i = 0; i < chunks; i++)
        queues[i] = new Queue<string>(bufferlen);

    // Load the queues
    for (var i = 0; i < chunks; i++)
        LoadQueue(queues[i], readers[i], bufferlen);

    // Merge!
    var sw = new StreamWriter("C:\\BigFileSorted.txt");
    bool done = false;
    int lowest_index, j, progress = 0;
    string lowest_value;
    while (! )
    {
        // Report the progress
        if (++progress % 5000 == 0)
            Console.Write("{0:f2}%   \r",
                100.0 * progress / records);

        // Find the chunk with the lowest value
        lowest_index = -1;
        lowest_value = "";
        for (j = 0; j < chunks; j++)
        {
            if (queues[j] != null)
            {
                if (lowest_index < 0 ||
                    String.CompareOrdinal(
                        queues[j].Peek(), lowest_value) < 0)
                {
                    lowest_index = j;
                    lowest_value = queues[j].Peek();
                }
            }
        }

        // Was nothing found in any queue? We must be done then.
        if (lowest_index == -1)
        {
            done = true;
            break;
        }

        // Output it
        sw.WriteLine(lowest_value);

        // Remove from queue
        queues[lowest_index].Dequeue();
        // Have we emptied the queue? Top it up
        if (queues[lowest_index].Count == 0)
        {
            LoadQueue(queues[lowest_index],
                readers[lowest_index], bufferlen);
            // Was there nothing left to read?
            if (queues[lowest_index].Count == 0)
            {
                queues[lowest_index] = null;
            }
        }
    }

    sw.Close();

    // Close and delete the files
    for (int i = 0; i < chunks; i++)
    {
        readers[i].Close();
        File.Delete(paths[i]);
    }
}

static void LoadQueue(Queue<string> queue, StreamReader file, int records)
{
    for (int i = 0; i < records; i++)
    {
        if (file.Peek() < 0) break;
        queue.Enqueue(file.ReadLine());
    }
}*/