using System;
using System.Collections;
using System.Collections.Generic;

namespace FallChallengeAi
{
  public class SortedCollection<T> : ICollection<T>
  {
    private readonly LinkedList<T> _sortedList;
    private readonly IComparer<T> _comparer;

    public SortedCollection(IComparer<T> comparer)
    {
      if (comparer == null) throw new ArgumentNullException("comparer");

      _comparer = comparer;
      _sortedList = new LinkedList<T>();
    }

    public SortedCollection()
      : this(Comparer<T>.Default)
    { }

    public T Dequeue()
    {
      var res = _sortedList.First;
      _sortedList.RemoveFirst();
      return res.Value;
    }

    public void Add(T item)
    {
      LinkedListNode<T> node = _sortedList.First;
      if (node == null || _comparer.Compare(node.Value, item) > 0)
      {
        _sortedList.AddFirst(item);
      }
      else
      {
        var iter = 0;
        while (node != null && _comparer.Compare(node.Value, item) < 1)
        {
          node = node.Next;
          ++iter;
          if (iter > 10)
            return;
        }

        if (node == null)
        {
          _sortedList.AddLast(item);
        }
        else
        {
          _sortedList.AddBefore(node, item);
        }
      }
    }

    public bool Remove(T item)
    {
      return _sortedList.Remove(item);
    }

    public IEnumerator<T> GetEnumerator()
    {
      return _sortedList.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return _sortedList.GetEnumerator();
    }

    public void Clear()
    {
      _sortedList.Clear();
    }

    public bool Contains(T item)
    {
      return _sortedList.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
      _sortedList.CopyTo(array, arrayIndex);
    }

    public int Count
    {
      get { return _sortedList.Count; }
    }

    public bool IsReadOnly { get { return false; } }
  }
}