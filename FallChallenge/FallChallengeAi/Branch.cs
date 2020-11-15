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
    const int baseline = 10;
    Score = 0;//.Sum((x)=>ScoreBrew(x));


    var idx = 0;
    foreach (var brew in State.Brews.OrderByDescending(x=>x.Price))
    {
      // Score += ScoreBrew(boardEntity) * idx;


      var canBrew = Inventory.CanPay(brew.IngredientChange);
      if (canBrew)
        Score += brew.Price * 10;
      else
      {
        Score += brew.Price * ScoreBrew(brew);
        // var deficit = Inventory.Deficit(brew.IngredientChange);
        // var penalty = Math.Pow(0.5, idx) * deficit.Total();
        // var penalty = brew.Price * ((6 - idx) / 10d);
        // Score -= penalty;
      }

      Score -= ScoreExcess(brew) * 0.5;


      //Score -= (0.01/4*(10 -))

      idx++;
    }

    // Score = ScoreBrew(State.Brews.First());

    double ScoreBrew(BoardEntity brew)
    {
      var ingredientsCnt = brew.IngredientChange.IngredientsCount();
      var part = 1; //10d / ingredientsCnt;
      var s = SimpleScore(Inventory.T0, brew.IngredientChange.T0) * part;
      s += SimpleScore(Inventory.T1, brew.IngredientChange.T1) * part;
      s += SimpleScore(Inventory.T2, brew.IngredientChange.T2) * part;
      s += SimpleScore(Inventory.T3, brew.IngredientChange.T3) * part;
      return s / ingredientsCnt;
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

    double ScoreExcess(BoardEntity brew)
    {
      var val = Inventory + brew.IngredientChange;
      return Math.Max((short) 0, val.T0)
             + Math.Max((short) 0, val.T1)
             + Math.Max((short) 0, val.T2)
             + Math.Max((short) 0, val.T3)
        ;
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