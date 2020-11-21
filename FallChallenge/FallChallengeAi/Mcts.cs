using System;
using System.Collections.Generic;

class MctsNode : IDisposable
{
  public Ingredient Inventory;

  public List<BoardMove> NewMoves;
  public readonly List<MctsNode> Children;

  public double Ucb => throw new NotImplementedException();

  public MctsNode()
  {
    Children = PoolList<List<MctsNode>>.Get();
  }

  public void Dispose()
  {
    PoolList<List<MctsNode>>.Put(Children);
  }
}

class MctsBranch
{
  public List<MoveCast> PossibleMoves;
}


static class Mcts
{
  public static BoardMove FindMove(GameState gs)
  {
    var rootNode = new MctsNode
    {
      Inventory = gs.Players[0].Witch.Inventory,
    };


    var leaf = Traverse(rootNode);
  }

  private static MctsNode Traverse(MctsNode node)
  {
    if (node.NewMoves.Count != 0)
      return node;

    return node.Children.FindMax(x => x.Ucb);
  }
}