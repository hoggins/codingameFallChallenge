//#define SNIFF
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Output
{
  public static void WriteLine(string str)
  {
#if !SNIFF
    Console.Error.WriteLine(str);
#endif
  }

  public static void Write(string s)
  {
#if !SNIFF
    Console.Error.Write(s);
#endif
  }
}

class Input
{
  private readonly IEnumerator<string> Inputs;

  public Input()
  {
#if !PUBLISHED
    var inputs = File.ReadLines("Input.txt");
    Inputs = inputs.Where(x=>!x.StartsWith("----- ")).GetEnumerator();
#endif
  }

  public string Line()
  {
#if !PUBLISHED
    Inputs.MoveNext();
    return Inputs.Current;
#endif

    var line = Console.ReadLine();
#if SNIFF && PUBLISHED
    Console.Error.WriteLine(line);
#endif
    return line;
  }

  public string[] LineArgs()
  {
    return Line().Split(' ');
  }
}