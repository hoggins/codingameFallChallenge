using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;


class MctsNode
{
  public MctsNode Parent;
  public int Depth;
  public double Score;
  public Ingredient Inventory;
  public long UsedCasts;
  public long LearnedCasts;
  public long CompleteBrews;

  public double Value;
  public int Number;

  public int? ActionIdx;

  public readonly List<MctsNode> Children = new List<MctsNode>(8);

  public double Ucb => (Value / Number) + 4*Math.Sqrt(Math.Log(Parent.Number)/Number);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool IsCastable(int idx) => (UsedCasts & (1 << idx)) == 0;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool IsBrewCompleted(int idx) => (CompleteBrews & (1 << idx)) != 0;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool IsLearned(int idx) => (LearnedCasts & (1 << idx)) != 0;
}

class MctsBranch
{
  public int StartTick;
  public List<MctsCast> Casts;
  public List<BoardEntity> Brews;
}


static class Mcts
{
  public static void Expand(MctsNode node, MctsBranch branch)
  {
    var canRest = false;
    foreach (var cast in branch.Casts)
    {
      if (!cast.IsLearn && !node.IsCastable(cast.EntityIdx))
      {
        canRest = true;
        continue;
      }

      var shouldLearn = cast.IsLearn && !node.IsLearned(cast.EntityIdx);
      if (node.Inventory.Total() + cast.Size > 10
        || !node.Inventory.CanPay(shouldLearn ? cast.RequiredLearn : cast.Required))
        continue;
      // todo: get rid of duplicates produced by multiple applications of not learned casts
      var newNode = new MctsNode
      {
        Parent = node,
        Depth = node.Depth + 1,
        Score = node.Score,
        Inventory = node.Inventory + (shouldLearn ? cast.TotalChangeLearn : cast.TotalChange),
        UsedCasts = shouldLearn ? node.UsedCasts : node.UsedCasts & (1 << cast.EntityIdx),
        LearnedCasts = !shouldLearn ? node.LearnedCasts : node.LearnedCasts & (1 << cast.EntityIdx),
        CompleteBrews = node.CompleteBrews,
        ActionIdx = cast.BranchRefIdx,
      };
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
        var newScore = brew.Price * (1 + (100 - newDepth) / 100d);
        var newNode = new MctsNode
        {
          Parent = node,
          Depth = node.Depth + 1,
          Score = node.Score + newScore,
          Inventory = node.Inventory - brew.IngredientPay,
          UsedCasts = node.UsedCasts,
          LearnedCasts = node.LearnedCasts,
          CompleteBrews = node.CompleteBrews & (1<<brewIdx),
          ActionIdx = -brewIdx-1,
        };
        // todo de duplicate
        node.Children.Add(newNode);
      }
    }


    if (canRest)
    {
      var newNode = new MctsNode
      {
        Parent = node,
        Depth = node.Depth + 1,
        Score = node.Score,
        Inventory = node.Inventory,
        UsedCasts = 0,
        LearnedCasts = node.LearnedCasts,
        CompleteBrews = node.CompleteBrews,
      };
      // todo de duplicate
      node.Children.Add(newNode);
    }
  }

  public static string ProduceCommand(GameState gs, Stopwatch sw)
  {
    var rootNode = new MctsNode
    {
      Score = gs.Players[0].Witch.Score,
      Inventory = gs.Players[0].Witch.Inventory,
    };
    var branch = new MctsBranch
    {
      StartTick = gs.Tick,
      Brews = gs.Brews,
      Casts = GenerateCasts(gs.Players[0].CastsAndLearn, rootNode)
    };
    var bestChild = MonteCarloTreeSearch(rootNode, branch, sw);
    if (!bestChild.ActionIdx.HasValue)
      return "REST";
    if (bestChild.ActionIdx < 0)
    {
      var brew = branch.Brews[-(bestChild.ActionIdx.Value + 1)];
      return "BREW " + brew.Id;
    }
    var cast = branch.Casts[bestChild.ActionIdx.Value];
    if (cast.IsLearn)
      return "LEARN " + cast.Cast.Id;
    if (!cast.Cast.IsCastable)
      return "REST";
    return "CAST " + cast.Cast.Id + " " + cast.Count;
  }

  public static MctsNode MonteCarloTreeSearch(MctsNode rootNode, MctsBranch branch, Stopwatch sw)
  {
    for (var i = 0;; i++)
    {
      var leaf = Traverse(rootNode);
      Expand(leaf, branch);
      var simResult = Rollout(leaf, branch);
      Backpropagate(leaf, simResult);
      if (sw.ElapsedMilliseconds > 45)
      {
        Program.AddComment("i:"+i.ToShortNumber());
        break;
      }
    }

    return rootNode.Children.FindMax(x => x.Number);
  }

  private static void Backpropagate(MctsNode node, double simResult)
  {
    ++node.Number;
    node.Value += simResult;
    if (node.Parent != null)
      Backpropagate(node.Parent, simResult);
  }

  private static List<MctsCast> GenerateCasts(List<BoardEntity> entities, MctsNode node)
  {
    var res = new List<MctsCast>();
    for (var entityIdx = 0; entityIdx < entities.Count; entityIdx++)
    {
      var entity = entities[entityIdx];
      var max = entity.IsRepeatable ? 4 : 2;
      for (var count = 1; count < max; count++)
      {
        res.Add(new MctsCast(entityIdx, res.Count, entity, count));
      }

      if (!entity.IsCastable && !entity.IsLearn)
        node.UsedCasts |= 1 << entityIdx;
    }

    return res;
  }

  private static MctsNode Traverse(MctsNode node)
  {
    if (node.Children.Count == 0)
      return node;
    foreach (var child in node.Children)
    {
      if (child.Children.Count == 0)
        return child;
    }

    return Traverse(node.Children.FindMax(x => x.Ucb));
  }

  private static double Rollout(MctsNode node, MctsBranch branch)
  {
    const int depth = 30;

    var srcInventory = node.Inventory;
    var srcLearn = node.LearnedCasts;
    var srcCast = node.UsedCasts;
    var srcBrews = node.CompleteBrews;

    var maxDepth = depth;
    var brewsComplete = 0;
    var score = (double)node.Score;
    for (var j = 1; j < maxDepth; j++)
    {
      var pickMove = PickMove(node, branch.Casts);
      if (pickMove == null)
        break;
      if (!node.IsCastable(pickMove.EntityIdx))
      {
        maxDepth++;
        j++;
        node.UsedCasts = 0;
      }

      var shouldLearn = pickMove.IsLearn && !node.IsLearned(pickMove.EntityIdx);
      node.Inventory += shouldLearn ? pickMove.TotalChangeLearn : pickMove.TotalChange;
      node.UsedCasts |= 1 << pickMove.EntityIdx;

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
          score += brew.Price * (1 + (100 - realDepth) / 100d);
        }
      }

      if (brewsComplete == 2)
        break;
    }

    node.Inventory = srcInventory;
    node.LearnedCasts = srcLearn;
    node.UsedCasts = srcCast;
    node.CompleteBrews = srcBrews;
    return score;
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

class MctsCast
{
  public readonly int EntityIdx;
  public readonly int BranchRefIdx;
  public readonly BoardEntity Cast;
  public readonly int Count;

  public readonly int Size;
  public readonly Ingredient Required;
  public readonly Ingredient TotalChange;

  public readonly bool IsLearn;
  public readonly Ingredient RequiredLearn;
  public readonly Ingredient TotalChangeLearn;

  public MctsCast(int entityIdx, int branchRefIdx, BoardEntity cast, int count)
  {
    EntityIdx = entityIdx;
    BranchRefIdx = branchRefIdx;
    Cast = cast;
    Count = count;

    Required = Cast.IngredientPay * Count;
    TotalChange = Cast.IngredientChange * Count;
    if (Cast.Type == EntityType.LEARN)
    {
      IsLearn = true;
      RequiredLearn = new Ingredient((short)Cast.TomeIndex,0,0,0);
      TotalChangeLearn =  new Ingredient((short)(-Cast.TomeIndex + Cast.TaxCount),0,0,0);
    }

    Size = TotalChange.Total();
  }
}

