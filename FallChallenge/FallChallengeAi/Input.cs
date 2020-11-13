using System;

class Output
{
  public static void WriteLine(string str)
  {
    Console.Error.WriteLine(str);
  }
}

class Input
{
  public string Line()
  {
    return Console.ReadLine();
  }

  public string[] LineArgs()
  {
    return Line().Split(' ');
  }
}