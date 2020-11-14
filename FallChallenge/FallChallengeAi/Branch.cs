using System;
using System.Collections.Generic;
using System.Linq;

class Branch
{
  public double Score;
  public GameState State;
  public Ingredient Inventory;
  public List<BoardMove> Moves = new List<BoardMove>();


  public Branch Clone()
  {
    return new Branch
    {
      State = State,
      Inventory = Inventory,
      Moves = new List<BoardMove>(Moves),
    };
  }

  public void Evaluate()
  {
    Score = State.Brews.Sum(x=>ScoreBrew(x));
    // Score = ScoreBrew(State.Brews.First());

    double ScoreBrew(BoardEntity brew)
    {
      var s = SimpleScore(Inventory.T0, brew.IngredientChange.T0);
      s += SimpleScore(Inventory.T1, brew.IngredientChange.T1);
      s += SimpleScore(Inventory.T2, brew.IngredientChange.T2);
      s += SimpleScore(Inventory.T3, brew.IngredientChange.T3);
      return s;
    }

    double SimpleScore(double v1, double v2)
    {
      if (v2 >= 0)
        return 0;
      v2 = v2 * -1;
      if (v1 > v2)
        return 1;
      return v1 / v2;
    }
  }

  public void Cast(BoardEntity cast, int count)
  {
    var change = cast.IngredientChange * count;
    Inventory += change;
  }

  public void Print()
  {
    for (var i = 0; i < Moves.Count && i < 10; i++)
    {
      var step = Moves[i];
      Output.Write($"{step.GetType().Name}  => ");
    }
  }
}