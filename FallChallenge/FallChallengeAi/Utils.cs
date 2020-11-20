static class Utils
{
  public static string ToShortNumber(this int val)
  {
    if (val > 1000)
      return (val / 1000d).ToString("0.#") + "k";
    return val.ToString();
  }
}