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
        var cmd = FindForward(gs);
        Console.WriteLine(cmd.GetCommand() + " " + cmd.Comment + " "+sw.ElapsedMilliseconds);
      }

      // in the first league: BREW <id> | WAIT; later: BREW <id> | CAST <id> [<times>] | LEARN <id> | REST | WAIT
    }
  }


  static BoardMove FindForward(GameState gs)
  {
    // if (gs.Casts.Count < 8)
      // return new MoveLearn(gs);

    var srcBranch = new Branch
    {
      State = gs,
      Inventory = gs.Myself.Inventory,
    };

    var moves = GenerateCastMoves(gs).ToList();


    var moveScores = new Dictionary<int, List<double>>();


    for (int i = 0; i < 3000; i++)
    {
      srcBranch.Inventory = gs.Myself.Inventory;

      var firstMove = PickMove(srcBranch, moves);
      firstMove.Simulate(srcBranch);

      for (int j = 0; j < 15; j++)
      {
        var pickMove = PickMove(srcBranch, moves);
        pickMove.Simulate(srcBranch);
      }

      srcBranch.Evaluate();

      var id = (firstMove.Cast.Id << 4) + firstMove.Count;
      if (!moveScores.TryGetValue(id, out var line))
        moveScores.Add(id, line = new List<double>());
      line.Add(srcBranch.Score);
    }

    var (avgScore, pair) = FindMax(moveScores);
    var finalId = pair.Key >> 4;
    var cast = gs.Casts.First(x => x.Id == finalId);

    // if (avgScore < 100)
      // return new MoveLearn(gs).WithComment(avgScore.ToString("0.##"));

    if (cast.IsLearn)
      return new MoveLearn(cast);

    if (!cast.IsCastable)
      return new MoveReset().WithComment(avgScore.ToString("0.##"));

    var count = pair.Key & 0xf;
    var move = new MoveCast(cast, count).WithComment(avgScore.ToString("0.##"));
    return move;
  }

  private static (double, KeyValuePair<int, List<double>>) FindMax(Dictionary<int,List<double>> moveScores)
  {
    var best = ((double, KeyValuePair<int, List<double>>)?) null;
    foreach (var pair in moveScores)
    {
      var score = pair.Value.Average();
      if (!best.HasValue || best.Value.Item1 < score)
        best = (score, pair);
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
      if (cast.Cast.Type == EntityType.LEARN && cast.Cast.TomeIndex > branch.Inventory.T0)
        continue;
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
