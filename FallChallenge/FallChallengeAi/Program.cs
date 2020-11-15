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

      gs.Brews = gs.Entities
        .Where(x => x.IsBrew)
        .OrderByDescending(x => x.Price)
        .ToList();

      // To debug: Console.Error.WriteLine("Debug messages...");

      var producable = gs.Brews.FirstOrDefault(x => gs.Myself.Inventory.CanPay(x.IngredientPay));
      if (producable != null)
      {
        Console.WriteLine("BREW " + producable.Id);
      }
      else
      {
        var sw = Stopwatch.StartNew();
        var cmd = FindForward(gs);
        Console.WriteLine(cmd.GetCommand() + " " + cmd.Comment + " "+sw.ElapsedMilliseconds);
      }

      // in the first league: BREW <id> | WAIT; later: BREW <id> | CAST <id> [<times>] | LEARN <id> | REST | WAIT
    }
  }


  static BoardMove FindForward(GameState gs)
  {
    const int depth = 15;

    if (gs.Casts.Count < 12)
      return new MoveLearn(gs);

    var srcBranch = new Branch
    {
      State = gs,
      Inventory = gs.Myself.Inventory,
    };

    var moves = GenerateCastMoves(gs).ToList();

    for (int i = 0; i < 100000; i++)
    {
      srcBranch.Inventory = gs.Myself.Inventory;

      var firstMove = PickMove(srcBranch, moves);
      firstMove.Simulate(srcBranch);

      for (int j = 0; j < depth; j++)
      {
        var pickMove = PickMove(srcBranch, moves);
        pickMove.Simulate(srcBranch);
      }

      srcBranch.Evaluate();

      firstMove.Outcomes.Add(srcBranch.Score);

      if (i % 100 == 0 && sw.ElapsedMilliseconds > 45)
      {
        Output.WriteLine("s:" + i);
        break;
      }
    }

    var (move, avgScore) = FindMax(moves);

    if (move.Cast.IsLearn)
      return new MoveLearn(move.Cast);

    if (!move.Cast.IsCastable)
      return new MoveReset().WithComment(avgScore.ToString("0.##"));

    return move.WithComment(avgScore.ToString("0.##"));
  }

  private static (MoveCast, double) FindMax(List<MoveCast> moves)
  {
    var best = ((MoveCast, double)?) null;
    foreach (var move in moves)
    {
      if (move.Outcomes.Count == 0)
        continue;
      var score = move.Outcomes.Average();
      if (!best.HasValue || best.Value.Item2 < score)
        best = (move, score);
    }

    return best.Value;
  }

  private static MoveCast PickMove(Branch branch, List<MoveCast> casts)
  {
    MoveCast lastMove = null;
    var castsCount = casts.Count;
    var rnd = Program.Rnd.Next(castsCount);
    for (var index = 0; index < castsCount; index++)
    {
      var cast = casts[index];
      if (branch.Inventory.T0 >= cast.Required.T0
          && branch.Inventory.T1 >= cast.Required.T1
          && branch.Inventory.T2 >= cast.Required.T2
          && branch.Inventory.T3 >= cast.Required.T3)
      {
        lastMove = cast;
        if (index >= rnd)
          return cast;
      }
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
}

abstract class BoardMove
{
  public string Comment;

  public abstract void Simulate(Branch branch);
  public abstract string GetCommand();

  public BoardMove WithComment(string comment)
  {
    Comment = comment;
    return this;
  }
}

class MoveCast : BoardMove
{
  public readonly int Key;
  public readonly Ingredient Required;
  public readonly Ingredient TotalChnge;

  public readonly List<double> Outcomes = new List<double>();

  public readonly BoardEntity Cast;
  public readonly int Count;

  public MoveCast(BoardEntity cast, int count)
  {
    Cast = cast;
    Count = count;

    Key = (cast.Id << 4) + count;

    Required = Cast.IngredientPay * Count;
    TotalChnge = Cast.IngredientChange * Count;
    if (Cast.Type == EntityType.LEARN)
    {
      Required.T0 += (short)Cast.TomeIndex;
      TotalChnge.T0 -= (short)Cast.TomeIndex;
    }


  }

  public override void Simulate(Branch branch)
  {
    branch.Inventory += TotalChnge;
  }

  public override string GetCommand() => $"CAST {Cast.Id} {Count} ";
}


class MoveReset : BoardMove
{
  public override void Simulate(Branch branch)
  {
  }

  public override string GetCommand() => "REST";
}

class MoveLearn : BoardMove
{
  private readonly BoardEntity _learn;

  public MoveLearn(BoardEntity learn)
  {
    _learn = learn;
  }

  public MoveLearn(GameState gs)
  {
    _learn = gs.Learns.First(x => x.TomeIndex == 0);
  }

  public override void Simulate(Branch branch)
  {
  }

  public override string GetCommand() => "LEARN " + _learn.Id;
}
