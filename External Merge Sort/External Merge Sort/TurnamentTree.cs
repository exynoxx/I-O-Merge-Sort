using System.Diagnostics;

public class TurnamentTree<T>
{
    private T[] _data;
    private int[] _origin; //index of list containing value in node i
    private int[] _parent; //hashmap, mapping children idx to parent idx
    private T _top; //item above root (smallest element)
    private int _topOrigin;
    private readonly IComparer<T> _comparer;
    private readonly IEnumerator<T>[] _streams;

    public TurnamentTree(List<IEnumerable<T>> inputLists, IComparer<T> comparer)
    {
        var n = inputLists.Count * 2;
        _comparer = comparer;
        _data = new T[n];
        _origin = new int[n];
        _streams = new IEnumerator<T>[inputLists.Count];
        
        for (int i = 0; i < inputLists.Count; i++)
        {
            _streams[i] = inputLists[i].GetEnumerator();
            _data[i] = ReadFromList(i);
            _origin[i] = i;
            //Fill(i);
        }
        _parent = ConstructTree(_streams.Select(x=>x.Current).ToArray());
    }

    private IEnumerable<int> LeafToRootPath(int i)
    {
        while (i != _parent[i])
        {
            yield return i;
        }

        yield return i;
    }

    public void Fill(int i)
    {
        var value = _data[i];
        var origin = i;
        for (; _parent[i] != i; i=_parent[i])
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

    public T Pop()
    {
        if (_top == null)
        {
            var rootval = _data.Last();
            var rootorigin = _origin.Last();
            _data[^1] = default;
            _data[rootorigin] = ReadFromList(rootorigin);
            Fill(rootorigin);
            return rootval;
        }
        
        var value = _top;
        _data[_topOrigin] = ReadFromList(_topOrigin);
        Fill(_topOrigin);
        return value;
    }
    
    private T ReadFromList(int i)
    {
        if(!_streams[i].MoveNext()) return default;
        return _streams[i].Current;
    }
    
    private int Compare(T a, T b)
    {
        if (a == null) return 1;
        if (b == null) return -1;
        return _comparer.Compare(a,b);
    }

    private (int winner, int loser) PlayGame(int a, int b)
    {
        return _comparer.Compare(_data[a], _data[b]) < 0 ? (a,b) : (b,a);
    }

    private int[] ConstructTree(T[] leafValues)
    {
        var queue = new List<(int,int,int,int)>();
        var oddQueue = new List<(int,int)>();
        var parent = new int[leafValues.Length*2];
        for (int i = 0; i < leafValues.Length; i+=2)
        {
            queue.Add((i,i+1,i,i+1));

        }
        var head = leafValues.Length; //start

        //while there is more lvls in the tree, do:
        for (var nextQueue = new List<(int,int,int,int)>(); true ; queue = nextQueue, nextQueue = new())
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

            //construct bottom up. queue contains one lvl of nodes
            for (int i = 0; i < queue.Count-queue.Count%2; i+=2)
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

                //fill in node values correctly
                var leftgame = PlayGame(awinner, bwinner);
                _data[left] = _data[leftgame.loser];
                _origin[left] = leftgame.loser;

                var rightgame = PlayGame(xwinner, ywinner);
                _data[right] = _data[rightgame.loser];
                _origin[right] = rightgame.loser;
                
                nextQueue.Add((left,right,leftgame.winner,rightgame.winner));
            }

            //odd number of children. put in its own queue or merge the 2 across layers
            if (queue.Count % 2 != 0)
            {
                var pos = head++;
                var (a, b,awinner,bwinner) = queue.Last();
                parent[a] = pos;
                parent[b] = pos;

                var game = PlayGame(awinner, bwinner);
                _data[pos] = _data[game.loser];
                _origin[pos] = game.loser;
                
                if(oddQueue.Count == 0)
                    oddQueue.Add((pos,game.winner));
                else
                {
                    var (otherPos, otherWinner) = oddQueue.Single();
                    nextQueue.Add((otherPos,pos,otherWinner,game.winner));
                    oddQueue.Clear();
                }
            }
           
        }

        return parent;
    }
}

public class Naive
{
    private IEnumerator<string>[] _streams;
    private string[] _head;

    public Naive(List<IEnumerable<string>> streams)
    {
        _head = new string[streams.Count];
        for (int i = 0; i < streams.Count; i++)
        {
            _streams = streams.Select(x=>x.GetEnumerator()).ToArray();
            _head[i] = GetItem(i);
        }
    }

    public string GetItem(int i)
    {
        if (!_streams[i].MoveNext()) return null;
        return _streams[i].Current;
    }

    public string Pop()
    {
        var lowest_index = -1;
        string lowest_value = null;
        for (var j = 0; j < _streams.Length; j++)
        {
            if (_head[j] != null)
            {
                if (lowest_index < 0 || string.Compare(_head[j], lowest_value) < 0)
                {
                    lowest_index = j;
                    lowest_value = _head[j];
                }
            }
        }

        if (lowest_index > -1)
        {
            _head[lowest_index] = GetItem(lowest_index);
        }

        return lowest_value;
    }
    
    public static void Main(string[] args)
    {
        Random rnd = new Random();
        
        var klists1 = new List<IEnumerable<string>>();
        var klists2 = new List<IEnumerable<string>>();
        for (int i = 0; i < 55; i++)
        {
            var list1 = Enumerable
                .Range(0, 1000000)
                .Select(_ => rnd.Next(0, 1000).ToString());
            var list2 = Enumerable
                .Range(0, 1000000)
                .Select(_ => rnd.Next(0, 1000).ToString());

            klists1.Add(list1);
            klists2.Add(list2);
        }


        var t1 = Stopwatch.StartNew();
        var naive = new Naive(klists1);
        for (int i = 0; i < 1000_000; i++)
        {
            naive.Pop();
        }
        t1.Stop();
        Console.WriteLine("naive time "+t1.ElapsedMilliseconds);
        var t2 = Stopwatch.StartNew();
        var tree = new TurnamentTree<string>(klists2,StringComparer.Ordinal);
        for (int i = 0; i < 1000_000; i++)
        {
            tree.Pop();
        }
        t2.Stop();
        Console.WriteLine("tree time "+t2.ElapsedMilliseconds);


        /*var a = new List<string> {"a", "b", "c", "d"};
        var b = new List<string> {"e", "f", "g", "h"};
        var c = new List<string> {"i", "j", "k", "l"};
        var d = new List<string> {"m", "n", "o", "p"};
        
        var tree = new TurnamentTree<string>(new List<IEnumerable<string>>{a,b,c,d}, StringComparer.Ordinal);
        for (int i = 0; i < 4*4+1; i++)
        {
            Console.WriteLine(tree.Pop());
        }*/
        /*Random rnd = new Random();
        var klists1 = new List<IEnumerable<string>>();
        for (int i = 0; i < 55; i++)
        {
            var list1 = Enumerable
                .Range(0, 1000)
                .Select(_ => rnd.Next(0, 1000).ToString());
            
            klists1.Add(list1);
        }

        
        var tree = new TurnamentTree<string>(klists1,StringComparer.Ordinal);
        
        for (int i = 0; i < 1000; i++)
        {
            Console.WriteLine(tree.Pop());
        }*/
        
    }
}