//#define SNIFF
//#define PUBLISHED
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Output
{
  private static bool _sniff;
  public static void Init(bool sniff)
  {
    _sniff = sniff;
  }

  public static void WriteLine(string str)
  {
    if (!_sniff)
      Console.Error.WriteLine(str);
  }

  public static void Write(string s)
  {
    if (!_sniff)
      Console.Error.Write(s);
  }
}

class Input
{
  private readonly bool _published;
  private readonly bool _sniff;
  private readonly IEnumerator<string> Inputs;

  public Input(bool published, bool sniff)
  {
    _published = published;
    _sniff = sniff;
    if (!_published)
    {
      var inputs = File.ReadLines("Input.txt");
      Inputs = inputs.Where(x => !x.StartsWith("----- ")).GetEnumerator();
    }
  }

  public string Line()
  {
    if (!_published)
    {
      Inputs.MoveNext();
      return Inputs.Current;
    }

    var line = Console.ReadLine();
    if (_sniff && _published)
      Console.Error.WriteLine(line);
    return line;
  }

  public string[] LineArgs()
  {
    return Line().Split(' ');
  }
}