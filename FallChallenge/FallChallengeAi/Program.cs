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


    for (var tick = 0;; ++tick)
    {
      Comment = string.Empty;
      var gs = new GameState();

      var actionCount = int.Parse(input.Line()); // the number of spells and recipes in play
      for (int i = 0; i < actionCount; i++)
        gs.AddEntity(new BoardEntity(input.LineArgs()));

      for (var i = 0; i < 2; i++)
        gs.Players[i].Witch = new Witch(input.LineArgs());

      gs.Brews = gs.Entities
        .Where(x => x.IsBrew)
        .OrderByDescending(x => x.Price)
        .ToList();

      // To debug: Console.Error.WriteLine("Debug messages...");

      var sw = Stopwatch.StartNew();

      var branch = new Branch
      {
        InitialInventory = gs.Players[0].Witch.Inventory,
        Learns = gs.Learns,
        Casts = gs.Players[0].Casts,
        CastsAndLearn = gs.Players[0].CastsAndLearn,
        Brews = gs.Brews.Select(x=>new Brew(x)).ToList(),
      };

      var cmd = FindForward(branch, sw);
      branch.Dispose();
      AddComment(sw.ElapsedMilliseconds);
      Console.WriteLine(cmd.GetCommand() + Comment);

      gs.Dispose();
      // in the first league: BREW <id> | WAIT; later: BREW <id> | CAST <id> [<times>] | LEARN <id> | REST | WAIT
    }
  }


  static BoardMove FindForward(Branch branch, Stopwatch sw)
  {
    foreach (var entity in branch.Learns.OrderBy(x => x.TomeIndex).Take(2))
      if (entity.IngredientPay.Total() == 0)
        return new MoveLearn(entity);

    if (branch.Casts.Count < 9)
      return new MoveLearn(branch);

    SimulateBranch(branch, sw, 46);

    var readyBrew = branch.Brews.FirstOrDefault(x => branch.Inventory.CanPay(x.Value.IngredientPay));
    if (readyBrew != null)
    {
      var betterBrew = branch.Brews.FirstOrDefault(x => x.ShortestPath <= 3 && x.Value.Price > readyBrew.Value.Price);
      if (betterBrew == null)
        return new MoveBrew(readyBrew.Value);
      else
      {
        Output.WriteLine($"skip brew {readyBrew.Value.IngredientChange} in feawer for {betterBrew.Value.IngredientChange} in {betterBrew.ShortestPath}");
      }
    }


    // var toTake = branch.Brews.OrderBy(x => x.Value.Price * (1 + (20d - x.ShortestPath / 20d * 2))).First();
    // var move = toTake.FirstStep;
    // if (move == null)
      var move  = FindMax(branch.Moves);

    //AddComment(ToShortNumber(avgScore));

    // PrintBrews(branch.Brews);
    // Output.WriteLine("");
    // PrintMoveScores(moves);

    foreach (var toDispose in branch.Moves)
      toDispose.Dispose();
    foreach (var toDispose in branch.Brews)
      toDispose.Dispose();

    if (move.Cast.IsLearn)
      return new MoveLearn(move.Cast);

    if (!move.Cast.IsCastable)
      return new MoveReset();

    return move;
  }

  private static void SimulateBranch(Branch branch, Stopwatch sw, int timeLimit)
  {
    FillBranchMoves(branch);
    for (var rollIdx = 0;; rollIdx++)
    {
      branch.RollOut = rollIdx;
      Rollout(branch);
      branch.Reset();

      if (rollIdx % 100 == 0 && sw.ElapsedMilliseconds > timeLimit)
      {
        AddComment(ToShortNumber(rollIdx));
        break;
      }
    }
  }

  private static void Rollout(Branch branch)
  {
    const int depth = 30;

    var firstMove = PickMove(branch, branch.Moves);
    firstMove.Simulate(branch);
    var startAt = !firstMove.Cast.IsCastable || firstMove.IsLearn? 2 : 1;

    var maxDepth = depth;
    var brewsComplete = 0;
    var score = 0d;
    for (int j = startAt; j < maxDepth; j++)
    {
      var pickMove = PickMove(branch, branch.Moves);
      if (pickMove == null)
        break;
      if (pickMove.UseOnRollOut == branch.RollOut)
      {
        maxDepth++;
        j++;
        pickMove.UseOnRollOut = -1;
      }
      pickMove.Simulate(branch);

      foreach (var brew in branch.Brews)
      {
        if (brew.LastRollOut == branch.RollOut)
          continue;
        var canBrew = branch.Inventory.CanPay(brew.Value.IngredientPay);
        if (canBrew)
        {
          ++brewsComplete;
          brew.LastRollOut = branch.RollOut;
          // brew.Iterations.Add(j);
          branch.Inventory -= brew.Value.IngredientPay;
          score += brew.Value.Price * (1 + (maxDepth - j) / (double) maxDepth * 2);

          if (j < brew.ShortestPath)
          {
            brew.ShortestPath = j;
            brew.FirstStep = firstMove;
          }

        }
      }
      if (brewsComplete == 2)
        break;
    }

    if (score < 1)
      score = branch.Evaluate(branch.RollOut);

    firstMove.Outcomes.Add(score);
  }

  private static MoveCast FindMax(List<MoveCast> moves)
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

    return best.Value.Item1;
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

  private static void FillBranchMoves(Branch branch)
  {
    foreach (var cast in branch.CastsAndLearn)
    {
      var max = cast.IsRepeatable ? 4 : 2;
      for (var i = 1; i < max; i++)
      {
        branch.Moves.Add(new MoveCast(cast, i));
      }
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
      Output.WriteLine($" -> {brew.FirstStep?.Cast?.IngredientChange}");
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

  public MoveLearn(Branch gs)
  {
    _learn = gs.Learns.First(x => x.TomeIndex == 0);
  }

  public override void Simulate(Branch branch)
  {
  }

  public override string GetCommand() => "LEARN " + _learn.Id;

  public override void Dispose()
  {

  }
}

class MoveBrew : BoardMove
{
  private readonly BoardEntity _brew;

  public MoveBrew(BoardEntity brew)
  {
    _brew = brew;
  }

  public override void Simulate(Branch branch)
  {
  }

  public override string GetCommand() => "BREW " + _brew.Id;

  public override void Dispose()
  {

  }
}
