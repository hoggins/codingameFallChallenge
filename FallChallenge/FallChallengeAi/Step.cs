using System;
using System.Collections.Generic;
using System.Linq;

abstract class Step
{
  public abstract bool CanExecute(GameState gs);

  public abstract List<Step> ProduceSubSteps(GameState gs);

  public abstract void Execute();
}

class StepBrew : Step
{
  private readonly BoardEntity _brew;

  public StepBrew(BoardEntity brew)
  {
    _brew = brew;
  }

  public override bool CanExecute(GameState gs)
  {
    return !Deficit(gs).HasValue;
  }

  public override List<Step> ProduceSubSteps(GameState gs)
  {
    var deficit = Deficit(gs);
    if (!deficit.HasValue)
      throw new Exception("logic error. should have been already executed");
    return Heuristic.FindCasts(gs, deficit.Value, _brew.IngredientChange[deficit.Value]);
  }

  public override void Execute()
  {
    Console.WriteLine("BREW "+_brew.Id);
  }

  private byte? Deficit(GameState gs)
  {
    return (gs.Myself.Inventory + _brew.IngredientChange).DeficitComponent();
  }
}

class StepCast : Step
{
  private readonly BoardEntity _cast;
  private readonly int _count;

  public StepCast(BoardEntity cast, int count)
  {
    _cast = cast;
    _count = count;
  }

  public override bool CanExecute(GameState gs)
  {
    return _cast.IsCastable && !Deficit(gs).HasValue;
  }

  public override List<Step> ProduceSubSteps(GameState gs)
  {
    var deficit = Deficit(gs);
    if (deficit.HasValue)
      return Heuristic.FindCasts(gs, deficit.Value.Item1, deficit.Value.Item2);

    if (!_cast.IsCastable)
      return new List<Step>{new StepReset()};
    throw new Exception("should have been already executed");
  }

  public override void Execute()
  {
    Console.WriteLine($"CAST {_cast.Id} {_count}");
  }

  private (byte, short)? Deficit(GameState gs)
  {
    return (gs.Myself.Inventory + (_cast.IngredientChange*_count)).DeficitComponentPair();
  }
}

class StepReset : Step
{
  public override bool CanExecute(GameState gs)
  {
    return true;
  }

  public override List<Step> ProduceSubSteps(GameState gs)
  {
    throw new NotImplementedException();
  }

  public override void Execute()
  {
    Console.WriteLine("REST");
  }
}

class StepLearn : Step
{
  private readonly BoardEntity _learn;

  public StepLearn(BoardEntity learn)
  {
    _learn = learn;
  }

  public override bool CanExecute(GameState gs)
  {
    return _learn.TomeIndex <= gs.Myself.Inventory.T0;
  }

  public override List<Step> ProduceSubSteps(GameState gs)
  {
    return Heuristic.FindCasts(gs, 0, _learn.TomeIndex);
  }

  public override void Execute()
  {
    Console.WriteLine("LEARN "+_learn.Id);
  }
}

static class Heuristic
{
  public static List<Step> FindCasts(GameState gs, byte deficit, int value)
  {
    return gs.Casts
      .Where(x => x.IngredientChange[deficit] > 0)
      .OrderBy(x=>CalcDist(value, x.IngredientChange[deficit], x.IsRepeatable))
      .Select(x => (Step) new StepCast(x, CalcCasts(value, x.IngredientChange[deficit], x.IsRepeatable)))
      .ToList();
  }

  private static int CalcDist(int value, int change, bool isRepeatable)
  {
    value = value * -1;
    if (!isRepeatable)
      return value - change;
    return (int)Math.Ceiling(value / (double) change);
  }

  private static int CalcCasts(int value, int change, bool isRepeatable)
  {
    if (!isRepeatable)
      return 1;
    value = value * -1;
    return (int) Math.Ceiling(value / (double) change);
  }
}