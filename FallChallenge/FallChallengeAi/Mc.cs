using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

static class Mc
{
  public static BoardMove FindForward(Branch branch, Stopwatch sw)
  {
    foreach (var entity in branch.Learns.OrderBy(x => x.TomeIndex).Take(2))
      if (entity.IngredientPay.Total() == 0 && entity.TomeIndex <= branch.InitialInventory.T0)
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
        Program.AddComment(rollIdx.ToShortNumber());
        break;
      }
    }
  }

  private static void Rollout(Branch branch)
  {
    const int depth = 10;

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

    // if (score < 1)
    // score = branch.Evaluate(branch.RollOut);

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
      var cntStr = String.Join(", ", cnt.Take(10).Select(x => $"({x.k}:{x.v})"));
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
}