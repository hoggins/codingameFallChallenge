using System;

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