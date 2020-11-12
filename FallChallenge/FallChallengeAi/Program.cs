using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;


static class Program
{
  static void Main(string[] args)
  {

    var input = new Input();

    // game loop
    while (true)
    {
      var entities = new List<BoardEntity>();
      var actionCount = int.Parse(input.Line()); // the number of spells and recipes in play
      for (int i = 0; i < actionCount; i++)
        entities.Add(new BoardEntity(input.LineArgs()));

      var witches = new List<Witch>();
      for (var i = 0; i < 2; i++)
        witches.Add(new Witch(input.LineArgs()));

      // Write an action using Console.WriteLine()
      // To debug: Console.Error.WriteLine("Debug messages...");

      var brew = entities.Where(x => witches[0].Inventory.AboveZero(x.IngredientChange))
        .OrderBy(x => x.IngredientChange.Total())
        .ThenByDescending(x=>x.Price)
        .FirstOrDefault();

      if (brew == null)
        Console.WriteLine("WAIT");
      else
        Console.WriteLine("BREW "+brew.Id);

      // in the first league: BREW <id> | WAIT; later: BREW <id> | CAST <id> [<times>] | LEARN <id> | REST | WAIT
      // Console.WriteLine("BREW "+brew.Id);
    }
  }
}