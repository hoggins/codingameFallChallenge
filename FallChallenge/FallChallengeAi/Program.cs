//#define FOR_DEBUG
//#define SNIFF
#define PUBLISHED
//#define PROFILER

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;


static class Program
{
  private const bool Published =
#if PUBLISHED
    true;
#else
    false;
#endif

  private const bool Sniff =
#if SNIFF
    true;
#else
    false;
#endif


  public static Random Rnd = new Random(1);

  public static string Comment = string.Empty;

  public static void AddComment(object s) => Comment += " " + s;

  static void Main(string[] args)
  {
#if FOR_DEBUG
    while (!Debugger.IsAttached)
      Thread.Sleep(3);
#endif

#if PROFILER
      JetBrains.Profiler.Api.MeasureProfiler.StartCollectingData();
#endif

      RunGame();

#if PROFILER
      JetBrains.Profiler.Api.MeasureProfiler.StopCollectingData();
      JetBrains.Profiler.Api.MeasureProfiler.SaveData();
#endif
  }

  private static void RunGame()
  {
    Output.Init(Sniff);
    var input = new Input(Published, Sniff);
    var global = new GlobalState();

    for (var tick = 0;; ++tick)
    {
      Comment = string.Empty;
      var gs = new GameState();

      var actionCount = int.Parse(input.Line()); // the number of spells and recipes in play
      for (int i = 0; i < actionCount; i++)
        gs.AddEntity(new BoardEntity(input.LineArgs()));

      for (var i = 0; i < 2; i++)
        gs.Witches.Add(new Witch(input.LineArgs()));

      global.Update(gs);

      gs.Brews = gs.Entities
        .Where(x => x.IsBrew)
        .OrderByDescending(x => x.Price)
        .ToList();

      // To debug: Console.Error.WriteLine("Debug messages...");

      var brews = FilterBrews(gs.Brews, global, gs);

      var producable = brews.FirstOrDefault(x => gs.Myself.Inventory.CanPay(x.Value.IngredientPay));
      if (producable != null)
      {
        Console.WriteLine("BREW " + producable.Value.Id);
      }
      else
      {
        var sw = Stopwatch.StartNew();
        var cmd = FindForward(global, gs, brews, sw);
        AddComment(sw.ElapsedMilliseconds);
        Console.WriteLine(cmd.GetCommand() + Comment);
      }

      // in the first league: BREW <id> | WAIT; later: BREW <id> | CAST <id> [<times>] | LEARN <id> | REST | WAIT
    }
  }


  static BoardMove FindForward(GlobalState globalState, GameState gs, List<Brew> brews, Stopwatch sw)
  {
    if (gs.Casts.Count < 12)
      return new MoveLearn(gs);

    var branch = new Branch
    {
      Inventory = gs.Myself.Inventory,
      // Brews = gs.Brews.Select(x=>new Brew(x)).ToList()
      Brews = brews,
    };

    var moves = GenerateCastMoves(gs).ToList();
    for (var rollIdx = 0; ; rollIdx++)
    {
      Rollout(gs, branch, rollIdx, moves);

      if (rollIdx % 100 == 0 && sw.ElapsedMilliseconds > 45)
      {
        AddComment(ToShortNumber(rollIdx));
        break;
      }
    }

    var (move, avgScore) = FindMax(moves);
    //AddComment(ToShortNumber(avgScore));

    // PrintBrews(branch.Brews);
    // Output.WriteLine("");
    // PrintMoveScores(moves);

    foreach (var toDispose in moves)
      toDispose.Dispose();
    foreach (var toDispose in branch.Brews)
      toDispose.Dispose();

    if (move.Cast.IsLearn)
      return new MoveLearn(move.Cast);

    if (!move.Cast.IsCastable)
      return new MoveReset();

    return move;
  }

  private static void Rollout(GameState gs, Branch branch, int rollIdx, List<MoveCast> moves)
  {
    const int depth = 15;
    branch.RollOut = rollIdx;
    branch.Inventory = gs.Myself.Inventory;
    branch.Score = 0;

    var firstMove = PickMove(branch, moves);
    firstMove.Simulate(branch);
    var startAt = !firstMove.Cast.IsCastable || firstMove.IsLearn? 2 : 1;

    var maxDepth = depth;
    for (int j = startAt; j < depth; j++)
    {
      var pickMove = PickMove(branch, moves);
      if (pickMove == null)
        break;
      if (pickMove.UseOnRollOut == rollIdx)
      {
        maxDepth++;
        j++;
        pickMove.UseOnRollOut = -1;
      }
      pickMove.Simulate(branch);

      foreach (var brew in branch.Brews)
      {
        if (brew.LastRollOut == rollIdx)
          continue;
        var canBrew = branch.Inventory.CanPay(brew.Value.IngredientPay);
        if (canBrew)
        {
          brew.LastRollOut = rollIdx;
          // brew.Iterations.Add(j);
          branch.Inventory -= brew.Value.IngredientPay;
          var inventoryBonus = (branch.Inventory.T0 + branch.Inventory.T1 * 2 + branch.Inventory.T2 * 3 +
                                branch.Inventory.T3 * 4);
          branch.Score += brew.Value.Price * (1 + (depth - j) / (double) depth) /*+ inventoryBonus*/;
        }
      }
    }

    branch.Evaluate(rollIdx);

    firstMove.Outcomes.Add(branch.Score);
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
    var space = 10 - branch.Inventory.Total();
    for (var index = 0; index < castsCount; index++)
    {
      var cast = casts[index];
      var required = cast.IsLearn && cast.LearnOnRollOut != branch.RollOut ? cast.RequiredLearn : cast.Required;
      if (space >= cast.Size
          && branch.Inventory.T0 >= required.T0
          && branch.Inventory.T1 >= required.T1
          && branch.Inventory.T2 >= required.T2
          && branch.Inventory.T3 >= required.T3)
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
    foreach (var cast in gs.CastsAndLearn)
    {
      var max = cast.IsRepeatable ? 4 : 2;
      for (var i = 1; i < max; i++)
      {
        yield return new MoveCast(cast, i);
      }
    }
  }

  private static List<Brew> FilterBrews(List<BoardEntity> brews, GlobalState globalState, GameState gs)
  {
    var currentBestBrew = 0;
    foreach (var brew in brews)
      if (brew.Price > currentBestBrew)
        currentBestBrew = brew.Price;

    var myCastLeft = 5 - globalState.BrewsCompleted[0];
    var otherCastLeft = 5 - globalState.BrewsCompleted[1];
    myCastLeft = Math.Min(myCastLeft, otherCastLeft);
    var myMaxScore = PredictedScore(0, myCastLeft);
    var otherMaxScore = PredictedScore(1, otherCastLeft);

    var minForEach = Math.Min(15, (otherMaxScore - myMaxScore) / (double)myCastLeft * 1.7);

    AddComment(minForEach.ToString("0"));

    var res = new List<Brew>();
    foreach (var entity in brews)
    {
      if (entity.Price < minForEach)
        continue;
      res.Add(new Brew(entity));
    }

    return res;

    int PredictedScore(int wIdx, int castLeft)
    {
      var s = gs.Witches[wIdx].Score;
      var c = castLeft;//6 - globalState.BrewsCompleted[wIdx] - 1;
      const int maxBrewSize = 20;
      return s + currentBestBrew + c * maxBrewSize;
    }
  }

  private static void PrintBrews(List<Brew> brews)
  {
    foreach (var brew in brews)
    {
      if (brew.Iterations.Count == 0)
        continue;
      var cnt = brew.Iterations.GroupBy(x => x).Select(x => (k: x.Key, v: x.Count())).OrderBy(x=>x.k).ToList();
      var cntStr = string.Join(", ", cnt.Take(10).Select(x => $"({x.k}:{x.v})"));
      Output.WriteLine($"{brew.Value.Price}: {brew.Value.IngredientChange} in cnt:{brew.Iterations.Count} " +
                       $"avg:{brew.Iterations.Average():0} " +
                       $"{cntStr}");
    }
  }

  private static void PrintMoveScores(List<MoveCast> moves)
  {
    foreach (var move in moves.Where(x=>x.Outcomes.Count > 0).OrderByDescending(x=>x.Outcomes.Average()))
    {
      var subset = move.Outcomes;//.Where(x=>x > 0).ToArray();
      if (subset.Count == 0)
        continue;
      var score = subset.Average();
      var learn = move.Cast.IsLearn ? "L " : String.Empty;
      Output.WriteLine($"{move.Cast} {learn}{score:0.00}");
    }
  }

  private static string ToShortNumber(int val)
  {
    if (val > 1000)
      return (val / 1000d).ToString("0.#") + "k";
    return val.ToString();
  }
}

abstract class BoardMove : IDisposable
{
  public abstract void Simulate(Branch branch);
  public abstract string GetCommand();
  public abstract void Dispose();
}


class MoveReset : BoardMove
{
  public override void Simulate(Branch branch)
  {
  }

  public override string GetCommand() => "REST";

  public override void Dispose()
  {
  }
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
    var learns = gs.Learns.OrderBy(x => x.TomeIndex).ToList();
    foreach (var entity in learns.Take(2))
    {
      if (entity.IngredientPay.Total() == 0)
      {
        _learn = entity;
        return;
      }
    }
    _learn = learns.First();
  }

  public override void Simulate(Branch branch)
  {
  }

  public override string GetCommand() => "LEARN " + _learn.Id;

  public override void Dispose()
  {

  }
}
