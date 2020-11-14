using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

static class Program
{
  public static Random Rnd = new Random(1);

  static void Main(string[] args)
  {
#if !PUBLISHED
    try
    {
#endif
      RunGame();
#if !PUBLISHED
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      Console.WriteLine("done");
      Console.ReadKey();
    }
#endif
  }

  private static void RunGame()
  {
    var input = new Input();


    for (var tick = 0;; ++tick)
    {
      var gs = new GameState();

      var actionCount = int.Parse(input.Line()); // the number of spells and recipes in play
      for (int i = 0; i < actionCount; i++)
        gs.AddEntity(new BoardEntity(input.LineArgs()));

      for (var i = 0; i < 2; i++)
        gs.Witches.Add(new Witch(input.LineArgs()));

      // To debug: Console.Error.WriteLine("Debug messages...");

      var producable = gs.Brews.FirstOrDefault(x => gs.Myself.Inventory.CanPay(x.IngredientChange));
      if (producable != null)
      {
        Console.WriteLine("BREW " + producable.Id);
      }
      else
      {
        var sw = Stopwatch.StartNew();
        var cmd = FindForward(gs).GetCommand();
        Console.WriteLine(cmd + " "+sw.ElapsedMilliseconds);
      }

      // in the first league: BREW <id> | WAIT; later: BREW <id> | CAST <id> [<times>] | LEARN <id> | REST | WAIT
    }
  }


  static BoardMove FindForward(GameState gs)
  {
    var srcBranch = new Branch
    {
      State = gs,
      Inventory = gs.Myself.Inventory,
    };

    var moves = GenerateCastMoves(gs).ToList();

    var bestMove = (score: double.MinValue, move: (MoveCast) null);


    for (int i = 0; i < 2000; i++)
    {
      srcBranch.Inventory = gs.Myself.Inventory;

      var firstMove = PickMove(srcBranch, moves);
      firstMove.Simulate(srcBranch);

      for (int j = 0; j < 30; j++)
      {
        var pickMove = PickMove(srcBranch, moves);
        pickMove.Simulate(srcBranch);
      }

      srcBranch.Evaluate();

      if (srcBranch.Score > bestMove.score)
      {
        bestMove = (srcBranch.Score, firstMove);
      }
    }

    if (!bestMove.move.Cast.IsCastable)
      return new MoveReset();

    return bestMove.move;
  }

  private static MoveCast PickMove(Branch branch, List<MoveCast> casts)
  {
    MoveCast lastMove = null;
    var castsCount = casts.Count;
    var rnd = Program.Rnd.Next(castsCount);
    for (var index = 0; index < castsCount; index++)
    {
      var cast = casts[index];
      if (!branch.Inventory.CanPay(cast.Required))
        continue;
      lastMove = cast;
      if (index >= rnd)
        return cast;
    }

    return lastMove;
  }

  private static IEnumerable<MoveCast> GenerateCastMoves(GameState gs)
  {
    foreach (var cast in gs.Casts)
    {
      var max = cast.IsRepeatable ? 4 : 2;
      for (var i = 1; i < max; i++)
      {
        yield return new MoveCast(cast, i);
      }
    }
  }

  private static IEnumerable<BoardMove> GenerateMoves(Branch branch)
  {
    foreach (var cast in branch.State.Casts)
    {
      for (var i = 1; i < 4 && branch.Inventory.CanPay(cast.IngredientChange*i); i++)
      {
        yield return new MoveCast(cast, i);
      }
    }
  }
}

abstract class BoardMove
{
  public abstract void Simulate(Branch branch);
  public abstract string GetCommand();
}

class MoveCast : BoardMove
{
  public readonly Ingredient Required;

  public readonly BoardEntity Cast;
  public readonly int Count;

  public MoveCast(BoardEntity cast, int count)
  {
    Cast = cast;
    Count = count;

    Required = Cast.IngredientChange * Count;
  }

  public override void Simulate(Branch branch) => branch.Cast(Cast, Count);

  public override string GetCommand() => $"CAST {Cast.Id} {Count}";
}


class MoveReset : BoardMove
{
  public override void Simulate(Branch branch)
  {
  }

  public override string GetCommand() => "REST";
}
