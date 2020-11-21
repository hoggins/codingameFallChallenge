using System;
using System.Collections.Generic;

class MoveCast : BoardMove
{
  public readonly int Size;
  public readonly Ingredient Required;
  public readonly Ingredient TotalChange;
  public readonly int Flow;

  public readonly bool IsLearn;
  public readonly Ingredient RequiredLearn;
  public readonly Ingredient TotalChangeLearn;
  public readonly int FlowLearn;

  public int UseOnRollOut = -1;
  public int LearnOnRollOut = -1;
  public readonly List<double> Outcomes;

  public readonly BoardEntity Cast;
  public readonly int Count;

  public MoveCast(BoardEntity cast, int count)
  {
    Cast = cast;
    Count = count;

    Required = Cast.IngredientPay * Count;
    TotalChange = Cast.IngredientChange * Count;
    Flow = Math.Abs(TotalChange.T0) + Math.Abs(TotalChange.T1) * 2 + Math.Abs(TotalChange.T2) * 3 + Math.Abs(TotalChange.T3) * 4;
    if (Cast.Type == EntityType.LEARN)
    {
      IsLearn = true;
      RequiredLearn = new Ingredient((short)Cast.TomeIndex,0,0,0);
      TotalChangeLearn =  new Ingredient((short)(-Cast.TomeIndex + Cast.TaxCount),0,0,0);
      FlowLearn = Math.Abs(TotalChangeLearn.T0) + Math.Abs(TotalChangeLearn.T1) * 2 + Math.Abs(TotalChangeLearn.T2) * 3 + Math.Abs(TotalChangeLearn.T3) * 4;
    }

    Size = TotalChange.Total();

    Outcomes = PoolList<List<double>>.Get();
  }

  public override void Simulate(Branch branch)
  {
    UseOnRollOut = branch.CastRollOut;

    Ingredient change;
    if (LearnOnRollOut != branch.MainRollOut)
    {
      change = TotalChangeLearn;
      branch.Flow += FlowLearn;
    }
    else
    {
      change = TotalChange;
      branch.Flow += Flow;
    }

    branch.Inventory += change;

    LearnOnRollOut = branch.MainRollOut;
  }

  public override string GetCommand() => $"CAST {Cast.Id} {Count} ";

  public override void Dispose()
  {
    PoolList<List<double>>.Put(Outcomes);
  }
}