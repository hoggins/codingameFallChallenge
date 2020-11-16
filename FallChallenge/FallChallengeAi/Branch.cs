using System;
using System.Collections.Generic;

class Branch
{
  public int Iteration;
  public double Score;
  public GameState State;
  public Ingredient Inventory;

  public void Evaluate(int i)
  {

    if (Score < 1)
    {
      var bestScore = 0d;
      foreach (var brew in State.Brews)
      {
        if (brew.DoneAtCicle == i)
          continue;
        var score = ScoreBrew(brew) * brew.Price * 0.5;
        if (score > bestScore)
          bestScore = score;
      }

      Score = bestScore;
    }

    double ScoreBrew(BoardEntity brew)
    {
      var ingredientsCnt = brew.BrewIngredientCount;
      var s = SimpleScore(Inventory.T0, brew.IngredientPay.T0);
      s += SimpleScore(Inventory.T1, brew.IngredientPay.T1);
      s += SimpleScore(Inventory.T2, brew.IngredientPay.T2);
      s += SimpleScore(Inventory.T3, brew.IngredientPay.T3);
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
}