using System.Collections;
using System.Collections.Generic;

static class PoolList<T> where T : IList, new()
{
  private static List<T> _pooled = new List<T>();

  public static T Get()
  {
    if (_pooled.Count == 0)
      return new T();
    var index = _pooled.Count - 1;
    var res = _pooled[index];
    res.Clear();
    _pooled.RemoveAt(index);
    return res;
  }

  public static void Put(T v)
  {
    _pooled.Add(v);
  }
}