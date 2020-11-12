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
    return gs.Casts
      .Where(x => x.IngredientChange[deficit.Value] > 0)
      .Select(x => (Step) new StepCast(x))
      .ToList();
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

  public StepCast(BoardEntity cast)
  {
    _cast = cast;
  }

  public override bool CanExecute(GameState gs)
  {
    return _cast.IsCastable && !Deficit(gs).HasValue;
  }

  public override List<Step> ProduceSubSteps(GameState gs)
  {
    var deficit = Deficit(gs);
    if (deficit.HasValue)
      return gs.Casts
        .Where(x => x.IngredientChange[deficit.Value] > 0)
        .Select(x => (Step) new StepCast(x))
        .ToList();
    if (!_cast.IsCastable)
      return new List<Step>{new StepReset()};
    throw new Exception("should have been already executed");
  }

  public override void Execute()
  {
    Console.WriteLine("CAST " + _cast.Id);
  }

  private byte? Deficit(GameState gs)
  {
    return (gs.Myself.Inventory + _cast.IngredientChange).DeficitComponent();
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