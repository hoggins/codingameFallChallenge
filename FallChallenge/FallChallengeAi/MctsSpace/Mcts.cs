//#define DRAWER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


static class Mcts
{
  private static int MaxDepth = 15;
  private static int RollOutMaxDepth = 7;
  private static int SearchTime = 44;

  public static string ProduceWarmCommand(GameState gs, Stopwatch sw)
  {
    if (gs.Tick == 0)
    {
      var oldSt = SearchTime;
      SearchTime = 200;
      var warm = ProduceCommand(gs, sw);
      SearchTime = oldSt;
      Output.WriteLine("warm " + warm);
    }
    if (TryProduceInitialLearn(gs, out var learnCmd))
      return learnCmd;

    if (gs.Tick > 25)
      MaxDepth = 13;
    // else if (gs.Tick > 30)
      // MaxDepth = 11;
    // else if (gs.Tick > 40)
      // MaxDepth = 9;

    return ProduceCommand(gs, sw);
  }
  public static string ProduceCommand(GameState gs, Stopwatch sw)
  {
    if (TryProduceInitialLearn(gs, out var learnCmd))
      return learnCmd;

    var rootNode = QuickPool<MctsNode>.Get();
    rootNode.Score = gs.Players[0].Witch.Score;
    rootNode.Inventory = gs.Players[0].Witch.Inventory;
    var branch = Pool<MctsBranch>.Get();
    branch.StartTick = gs.Tick;
    branch.Brews = gs.Brews;
    GenerateCasts(gs.Players[0].CastsAndLearn, rootNode, branch.Casts);
    var command = ProduceCommand(sw, rootNode, branch);
    branch.Reset();
    Pool<MctsBranch>.Put(branch);
    return command;
  }

  private static string ProduceCommand(Stopwatch sw, MctsNode rootNode, MctsBranch branch)
  {
    var actionIdx = MonteCarloTreeSearch(rootNode, branch, sw);
    if (!actionIdx.HasValue)
      return "REST";
    if (actionIdx < 0)
    {
      var brew = branch.Brews[-(actionIdx.Value + 1)];
      return "BREW " + brew.Id;
    }

    var cast = branch.Casts[actionIdx.Value];
    if (cast.IsLearn)
      return "LEARN " + cast.Cast.Id;

    return "CAST " + cast.Cast.Id + " " + cast.Count;
  }

  private static bool TryProduceInitialLearn(GameState gs, out string cmd)
  {
    if (gs.Players[0].Casts.Count < 9)
    {
      var learn = gs.Learns.First(x => x.TomeIndex == 0);
      cmd = "LEARN " + learn.Id;
      return true;
    }

    cmd = null;
    return false;
  }

  public static int? MonteCarloTreeSearch(MctsNode rootNode, MctsBranch branch, Stopwatch sw)
  {
    Expand(rootNode, branch);
    for (var i = 0;; i++)
    {
      var leaf = Traverse(rootNode);
      Expand(leaf, branch);
      var simResult = leaf.Children.Count == 0 ? leaf.Score : Rollout(leaf, branch);
      Backpropagate(leaf, simResult);
      if (i%10 == 0 && sw.ElapsedMilliseconds > SearchTime)
      {
        Program.AddComment(":"+(SearchTime - sw.ElapsedMilliseconds));
        Program.AddComment("i:"+i.ToShortNumber());
        break;
      }
    }
#if DRAWER
    Drawer.Draw(rootNode, branch);
#endif
    var bestNum = 0;
    MctsNode bestChild = null;
    // var bestChild = rootNode.Children.FindMax(x => x.Number);
    foreach (var child in rootNode.Children)
    {
      if (child.Number > bestNum)
      {
        bestNum = child.Number;
        bestChild = child;
      }
    }

    return bestChild.ActionIdx;
  }

  public static void Expand(MctsNode node, MctsBranch branch)
  {
    if (node.Depth + 1 > MaxDepth)
      return;

    var canRest = false;
    var nodeTotal = node.Inventory.Total();
    foreach (var cast in branch.Casts)
    {
      if (!cast.IsLearn && !node.IsCastable(cast.EntityIdx))
      {
        canRest = true;
        continue;
      }

      var shouldLearn = cast.IsLearn && !node.IsLearned(cast.EntityIdx);
      if ((!shouldLearn && nodeTotal + cast.Size > 10)
          || !node.Inventory.CanPay(shouldLearn ? cast.RequiredLearn : cast.Required))
        continue;
      if (shouldLearn && cast.Count > 1)
        continue;
      // todo: get rid of duplicates produced by multiple applications of not learned casts
      var newNode = QuickPool<MctsNode>.Get();
      newNode.Parent = node;
      newNode.Depth = node.Depth + 1;
      newNode.Score = !shouldLearn ? node.Score : node.Score-0.1;
      newNode.Inventory = node.Inventory + (shouldLearn ? cast.TotalChangeLearn : cast.TotalChange);
      newNode.UsedCasts = shouldLearn ? node.UsedCasts : node.UsedCasts | (1 << cast.EntityIdx);
      newNode.LearnedCasts = !shouldLearn ? node.LearnedCasts : node.LearnedCasts | (1 << cast.EntityIdx);
      newNode.CompleteBrews = node.CompleteBrews;
      newNode.ActionIdx = cast.BranchRefIdx;
      // todo de duplicate
      node.Children.Add(newNode);
    }

    for (var brewIdx = 0; brewIdx < branch.Brews.Count; brewIdx++)
    {
      if (node.IsBrewCompleted(brewIdx))
        continue;
      var brew = branch.Brews[brewIdx];
      var canBrew = node.Inventory.CanPay(brew.IngredientPay);
      if (canBrew)
      {
        var newDepth = node.Depth + 1;
        var newScore = brew.Price * (1 + (MaxDepth - newDepth) / (double)MaxDepth);
        var newNode = QuickPool<MctsNode>.Get();
        newNode.Parent = node;
        newNode.Depth = node.Depth + 1;
        newNode.Score = node.Score + newScore;
        newNode.Inventory = node.Inventory - brew.IngredientPay;
        newNode.UsedCasts = node.UsedCasts;
        newNode.LearnedCasts = node.LearnedCasts;
        newNode.CompleteBrews = node.CompleteBrews | (1<<brewIdx);
        newNode.ActionIdx = -brewIdx-1;
        // todo de duplicate
        node.Children.Add(newNode);
      }
    }


    if (canRest)
    {
      var newNode = QuickPool<MctsNode>.Get();
      newNode.Parent = node;
      newNode.Depth = node.Depth + 1;
      newNode.Score = node.Score;
      newNode.Inventory = node.Inventory;
      newNode.UsedCasts = 0;
      newNode.LearnedCasts = node.LearnedCasts;
      newNode.CompleteBrews = node.CompleteBrews;
      newNode.ActionIdx = null;
      // todo de duplicate
      node.Children.Add(newNode);
    }
  }

  private static void Backpropagate(MctsNode node, double simResult)
  {
    ++node.Number;
    node.Value += simResult;
    if (node.Parent != null)
      Backpropagate(node.Parent, simResult);
  }

  private static void GenerateCasts(List<BoardEntity> entities, MctsNode node, List<MctsCast> branchCasts)
  {
    var res = branchCasts;
    for (var entityIdx = 0; entityIdx < entities.Count; entityIdx++)
    {
      var entity = entities[entityIdx];
      var max = entity.IsRepeatable ? 4 : 2;
      for (var count = 1; count < max; count++)
      {
        var cast = QuickPool<MctsCast>.Get();
        cast.Init(entityIdx, res.Count, entity, count);
        res.Add(cast);
      }

      if (!entity.IsCastable && !entity.IsLearn)
        node.UsedCasts |= 1 << entityIdx;
    }
  }

  private static MctsNode Traverse(MctsNode node)
  {
    do
    {
      if (node.Children.Count == 0)
        return node;
      foreach (var child in node.Children)
      {
        if (child.Children.Count == 0)
          return child;
      }

      node = node.Children.FindMax(x => x.Ucb);
    } while (true);
  }

  private static double Rollout(MctsNode node, MctsBranch branch)
  {

    var srcInventory = node.Inventory;
    var srcLearn = node.LearnedCasts;
    var srcCast = node.UsedCasts;
    var srcBrews = node.CompleteBrews;

    var maxDepth = Math.Min(MaxDepth, node.Depth + RollOutMaxDepth) - node.Depth;
    var brewsComplete = 0;
    var score = (double)node.Score;
    for (var j = 1; j < maxDepth; j++)
    {
      var pickMove = PickMove(node, branch.Casts);
      if (pickMove == null)
        break;
      if (!node.IsCastable(pickMove.EntityIdx))
      {
        //maxDepth++;
        j++;
        node.UsedCasts = 0;
      }

      var shouldLearn = pickMove.IsLearn && !node.IsLearned(pickMove.EntityIdx);
      node.Inventory += shouldLearn ? pickMove.TotalChangeLearn : pickMove.TotalChange;
      node.UsedCasts |= 1 << pickMove.EntityIdx;
      if (shouldLearn)
        node.Score -= node.Score-0.1;

      for (var brewIdx = 0; brewIdx < branch.Brews.Count; brewIdx++)
      {
        var brew = branch.Brews[brewIdx];
        if (node.IsBrewCompleted(brewIdx))
          continue;
        var canBrew = node.Inventory.CanPay(brew.IngredientPay);
        if (canBrew)
        {
          ++brewsComplete;
          node.CompleteBrews |= 1 << brewIdx;
          node.Inventory -= brew.IngredientPay;
          var realDepth = node.Depth + j;
          score += brew.Price * (1 + (MaxDepth - realDepth) / (double)MaxDepth);
        }
      }

      if (brewsComplete == 2)
        break;
    }

    if (brewsComplete == 0)
      score += Evaluate(branch, node) * 0.01;

    node.Inventory = srcInventory;
    node.LearnedCasts = srcLearn;
    node.UsedCasts = srcCast;
    node.CompleteBrews = srcBrews;
    return score;
  }

  public static double Evaluate(MctsBranch branch, MctsNode node)
  {
    var bestScore = 0d;
    for (var brewIdx = 0; brewIdx < branch.Brews.Count; brewIdx++)
    {
      var brew = branch.Brews[brewIdx];
      if (node.IsBrewCompleted(brewIdx))
        continue;
      var score = ScoreBrew(brew) * brew.Price;
      if (score > bestScore)
        bestScore = score;
    }

    return bestScore;


    double ScoreBrew(BoardEntity brew)
    {
      var ingredientsCnt = brew.BrewIngredientCount;
      var s = SimpleScore(node.Inventory.T0, brew.IngredientPay.T0);
      s += SimpleScore(node.Inventory.T1, brew.IngredientPay.T1);
      s += SimpleScore(node.Inventory.T2, brew.IngredientPay.T2);
      s += SimpleScore(node.Inventory.T3, brew.IngredientPay.T3);
      return s / ingredientsCnt;
    }
    double SimpleScore(int v1, int v2)
    {
      if (v2 == 0)
        return 0;
      if (v1 > v2)
        return 1;
      return v1 / (double) v2;
    }
  }

  private static MctsCast PickMove(MctsNode node, List<MctsCast> casts)
  {
    MctsCast lastMove = null;
    var castsCount = casts.Count;
    var rnd = Program.Rnd.Next(castsCount);
    var space = 10 - node.Inventory.Total();
    for (var index = 0; index < castsCount; index++)
    {
      var cast = casts[index];
      var required = cast.IsLearn && !node.IsLearned(cast.EntityIdx) ? cast.RequiredLearn : cast.Required;
      if (space >= cast.Size
          && node.Inventory.T0 >= required.T0
          && node.Inventory.T1 >= required.T1
          && node.Inventory.T2 >= required.T2
          && node.Inventory.T3 >= required.T3)
      {
        lastMove = cast;
        if (index >= rnd)
          return cast;
      }
    }

    return lastMove;
  }
}