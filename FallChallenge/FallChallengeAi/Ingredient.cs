using System;

struct Ingredient
{
  public short T0;
  public short T1;
  public short T2;
  public short T3;

  public Ingredient(string[] arr, int idx) : this()
  {
    T0 = short.Parse(arr[idx + 0]);
    T1 = short.Parse(arr[idx + 1]);
    T2 = short.Parse(arr[idx + 2]);
    T3 = short.Parse(arr[idx + 3]);
  }

  public int this[int index]
  {
    get {
      switch (index)
      {
        case 0: return T0;
        case 1: return T1;
        case 2: return T2;
        case 3: return T3;
        default: throw new ArgumentOutOfRangeException(nameof(index));
      }
    }
  }

  public int Total()
  {
    return T0 + T1 + T2 + T3;
  }

  public bool AboveZero(Ingredient o)
  {
    return T0 + o.T0 >= 0 && T1 + o.T1 >= 0 && T2 + o.T2 >= 0 && T3 + o.T3 >= 0;
  }

  public Ingredient Abs()
  {
    return new Ingredient
    {
      T0 = Math.Abs(T0),
      T1 = Math.Abs(T1),
      T2 = Math.Abs(T2),
      T3 = Math.Abs(T3),
    };
  }

  public byte? DeficitComponent()
  {
    if (T3 < 0) return 3;
    if (T2 < 0) return 2;
    if (T1 < 0) return 1;
    if (T0 < 0) return 0;
    return null;
  }

  public (byte, short)? DeficitComponentPair()
  {
    if (T3 < 0) return (3, T3);
    if (T2 < 0) return (2, T2);
    if (T1 < 0) return (1, T1);
    if (T0 < 0) return (0, T0);
    return null;
  }

  public static Ingredient operator +(Ingredient a, Ingredient b)
  {
    return new Ingredient
    {
      T0 = (short) (a.T0 + b.T0),
      T1 = (short) (a.T1 + b.T1),
      T2 = (short) (a.T2 + b.T2),
      T3 = (short) (a.T3 + b.T3),
    };
  }

  public static Ingredient operator *(Ingredient a, int b)
  {
    return new Ingredient
    {
      T0 = (short) (a.T0 * b),
      T1 = (short) (a.T1 * b),
      T2 = (short) (a.T2 * b),
      T3 = (short) (a.T3 * b),
    };
  }
}