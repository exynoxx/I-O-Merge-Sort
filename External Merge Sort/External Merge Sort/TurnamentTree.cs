﻿using System.Diagnostics;
using HPCsharp.Algorithms;

public class TurnamentTree<T>
{
    private T[] _data;
    private int[] _origin; //index of list containing value in node i
    private int[] _parent; //hashmap, mapping children idx to parent idx
    private T _top; //item above root (smallest element)
    private int _topOrigin;
    private readonly IComparer<T> _comparer;
    private readonly IEnumerator<T>[] _streams;

    private readonly int _leafHeight;
    private readonly int _numLeafs;

    public TurnamentTree(List<IEnumerable<T>> inputLists, IComparer<T> comparer)
    {
        _leafHeight = (int) Math.Ceiling(Math.Log2(inputLists.Count));
        _numLeafs = (int) Math.Pow(2, _leafHeight);
        var n = (int) Math.Pow(2, _leafHeight + 1) - 1; //inputLists.Count * 2;
        _comparer = comparer;
        _data = new T[n];
        _parent = new int[n];
        _origin = new int[n];
        _streams = new IEnumerator<T>[inputLists.Count];

        for (int i = 0; i < inputLists.Count; i++)
        {
            _streams[i] = inputLists[i].GetEnumerator();
            _data[i] = ReadFromList(i);
            _origin[i] = i;
            //Fill(i);
        }

        //_parent = ConstructTree(_streams.Select(x=>x.Current).ToArray());
        ConstructTree(_numLeafs);
        FillTree();
    }

    private IEnumerable<int> LeafToRootPath(int i)
    {
        while (i != _parent[i])
        {
            yield return i;
        }

        yield return i;
    }
    
    public void Maintain(int i)
    {
        var value = _data[i];
        var origin = i;
        for (; _parent[i] != i; i = _parent[i])
        {
            //smaller than parent. current is the winner. keep loser in node. continue
            if (Compare(value, _data[_parent[i]]) < 0) continue;

            //parent is smaller one. update chain.
            var parentval = _data[_parent[i]];
            var parentorigin = _origin[_parent[i]];
            _data[_parent[i]] = value;
            _origin[_parent[i]] = origin;
            value = parentval;
            origin = parentorigin;
        }

        //root
        if (Compare(value, _data[i]) < 0)
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
            Maintain(rootorigin);
            return rootval;
        }

        var value = _top;
        _data[_topOrigin] = ReadFromList(_topOrigin);
        Maintain(_topOrigin);
        return value;
    }

    private T ReadFromList(int i)
    {
        if (!_streams[i].MoveNext()) return default;
        return _streams[i].Current;
    }

    private int Compare(T a, T b)
    {
        if (a == null) return 1;
        if (b == null) return -1;
        return _comparer.Compare(a, b);
    }

    private void ConstructTree(int numLeafs)
    {
        int head = numLeafs;
        int leafIndex = 0;
        var parent = ConstructRecurse(ref leafIndex, ref head, 0);
        _parent[parent] = parent;
    }

    //directionConstant: ex: leftNodes always have a parent with index 2 higher than node itself.
    //rightnodes always have a parent with index 1 higher
    private int ConstructRecurse(ref int leafIndex, ref int head, int h)
    {
        if (h == _leafHeight)
        {
            return leafIndex++;
        }

        var l = ConstructRecurse(ref leafIndex, ref head, h + 1);
        var r = ConstructRecurse(ref leafIndex, ref head, h + 1);

        _parent[l] = head;
        _parent[r] = head;
        head++;
        return head - 1;
    }

    public void FillTree()
    {
        int leafIndex = 0;
        int head = _numLeafs;
        var result = FillTreeRecurse(ref leafIndex, ref head, 0);
        _top = result.winner;
        _topOrigin = result.origin;
    }

    private (int pos, T winner, int origin) FillTreeRecurse(ref int leafIndex, ref int head, int h)
    {
        if (h == _leafHeight)
        {
            var idx = leafIndex++;
            if (idx < _streams.Length)
            {
                _data[idx] = ReadFromList(idx);
                return (idx, _data[idx], idx);
            }

            return (idx, default, -1);
        }

        var l = FillTreeRecurse(ref leafIndex, ref head, h + 1);
        var r = FillTreeRecurse(ref leafIndex, ref head, h + 1);
        if (Compare(l.winner, r.winner) < 0)
        {
            //write r, return l
            var current = _parent[l.pos];
            _data[current] = r.winner;
            _origin[current] = r.origin;
            return (current, l.winner, l.origin);
        }
        else
        {
            //write l, return r
            var current = _parent[l.pos];
            _data[current] = l.winner;
            _origin[current] = l.origin;
            return (current, r.winner, r.origin);
        }
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
        /*Random rnd = new Random();
        
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
        */


        Random rnd = new Random();
        var list = new List<IEnumerable<string>>();
        for (int i = 0; i < 5; i++)
        {
            var list1 = Enumerable
                .Range(0, 100)
                .Select(_ => rnd.Next(0, 1000).ToString());
            
            list.Add(list1);
        }

        
        var tree = new TurnamentTree<string>(list,StringComparer.Ordinal);
        for (int i = 0; i < 55; i++)
        {
            Console.WriteLine(tree.Pop());
        }
        
    }
}
