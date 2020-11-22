using System;
using System.Collections;
using System.Collections.Generic;

public interface IReusable
{
  void Reset();
}

static class QuickPool<T> where T : IReusable, new()
{
  private static List<T> _pooled;
  private static int _top;

  public static void Allocate(int cnt)
  {
    _pooled = new List<T>(cnt);
    _top = cnt;
    for (int i = 0; i < cnt; i++)
    {
      _pooled.Add(new T());
    }
  }

  public static T Get()
  {
    if (--_top < 0)
      throw new Exception("out of pool " + typeof(T).Name);
    var reusable = _pooled[_top];
    reusable.Reset();
    return reusable;
  }

  public static void Reset()
  {
    _top = _pooled.Count;
  }
}

static class Pool<T> where T : new()
{
  private static List<T> _pooled = new List<T>();
  public static T Get()
  {
    if (_pooled.Count == 0)
      return new T();
    var index = _pooled.Count - 1;
    var res = _pooled[index];
    _pooled.RemoveAt(index);
    return res;
  }

  public static void Put(T v)
  {
    _pooled.Add(v);
  }

  public static void Allocate(int cnt)
  {
    for (int i = 0; i < cnt; i++)
    {
      _pooled.Add(new T());
    }
  }
}

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