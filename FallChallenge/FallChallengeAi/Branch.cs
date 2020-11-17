using System;
using System.Collections.Generic;

class Branch
{
  public int RollOut;
  public double Score;
  //public GameState State;
  public Ingredient Inventory;
  public List<Brew> Brews;

  public void Evaluate(int i)
  {

    if (Score < 1)
    {
      var bestScore = 0d;
      foreach (var brew in Brews)
      {
        if (brew.LastRollOut == i)
          continue;
        var score = ScoreBrew(brew.Value) * brew.Value.Price * 0.5;
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