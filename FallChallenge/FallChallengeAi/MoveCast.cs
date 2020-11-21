using System.Collections.Generic;

class MoveCast : BoardMove
{
  public readonly int Size;
  public readonly Ingredient Required;
  public readonly Ingredient TotalChange;

  public readonly bool IsLearn;
  public readonly Ingredient RequiredLearn;
  public readonly Ingredient TotalChangeLearn;

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
    if (Cast.Type == EntityType.LEARN)
    {
      IsLearn = true;
      RequiredLearn = new Ingredient((short)Cast.TomeIndex,0,0,0);
      TotalChangeLearn =  new Ingredient((short)(-Cast.TomeIndex + Cast.TaxCount),0,0,0);
    }

    Size = TotalChange.Total();

    Outcomes = PoolList<List<double>>.Get();
  }

  public override void Simulate(Branch branch)
  {
    UseOnRollOut = branch.CastRollOut;

    branch.Inventory += LearnOnRollOut != branch.RollOut
      ? TotalChangeLearn
      : TotalChange;
    Ingredient change;
    if (LearnOnRollOut != branch.MainRollOut)
    {
      change = TotalChangeLearn;
    }
    else
    {
      change = TotalChange;
    }
    branch.Inventory += change;

    LearnOnRollOut = branch.RollOut;
    LearnOnRollOut = branch.MainRollOut;
  }

  public override string GetCommand() => $"CAST {Cast.Id} {Count} ";

  public override void Dispose()
  {
    PoolList<List<double>>.Put(Outcomes);
  }
}