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

  public int Total()
  {
    return T0 + T1 + T2 + T3;
  }

  public bool AboveZero(Ingredient o)
  {
    return T0 + o.T0 >= 0 && T1 + o.T1 >= 0 && T2 + o.T2 >= 0 && T3 + o.T3 >= 0;
  }
}