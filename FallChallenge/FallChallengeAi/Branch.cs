using System;
using System.Collections.Generic;

class Branch : IDisposable
{
  // in
  public Ingredient InitialInventory;
  public List<BoardEntity> Learns;
  public List<BoardEntity> Casts;
  public List<BoardEntity> CastsAndLearn;
  public int BrewsLeftCount;
  // iteration
  public int MainRollOut;
  public int CastRollOut;
  public Ingredient Inventory;
  public int Flow;
  // in and out
  public List<Brew> Brews;
  public List<MoveCast> Moves;
  public double MaxScore;
  public int MaxBrewsCompleted;
  public int MaxBrewsCompletedAt;

  public Branch()
  {
    Moves = PoolList<List<MoveCast>>.Get();
  }

  public void Dispose()
  {
    PoolList<List<MoveCast>>.Put(Moves);
  }

  public double Evaluate(int rollOut, int maxDepth)
  {
    var bestScore = 0d;
    foreach (var brew in Brews)
    {
      if (brew.EnemyShortestPath < maxDepth)
        continue;
      if (brew.LastRollOut == rollOut)
        continue;
      var score = ScoreBrew(brew.Value) * brew.Value.Price;
      if (score > bestScore)
        bestScore = score;
    }

    return bestScore;


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

  public void Reset()
  {
    Inventory = InitialInventory;
    Flow = 0;
  }


}