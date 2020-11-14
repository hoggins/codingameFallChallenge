using System;
using System.Collections.Generic;
using System.Linq;

abstract class Step
{
  public abstract bool CanExecute(Branch branch);

  public abstract List<Step> ProduceSubSteps(Branch branch);

  public abstract void SimulateExecute(Branch branch);

  public abstract void Execute();
}

class StepBrew : Step
{
  private readonly BoardEntity _brew;

  public StepBrew(BoardEntity brew)
  {
    _brew = brew;
  }

  public override bool CanExecute(Branch gs)
  {
    throw new NotImplementedException();
  }

  public override List<Step> ProduceSubSteps(Branch branch)
  {
    var totalDeficit = branch.Deficit;
    var deficit = totalDeficit.DeficitComponentPair();
    if (!deficit.HasValue)
      throw new Exception("logic error. should have been already executed");
    return Heuristic.FindCasts(branch, deficit.Value.Item1, deficit.Value.Item2);
  }

  public override void SimulateExecute(Branch branch)
  {
    branch.Deficit += _brew.IngredientChange;
  }

  public override void Execute()
  {
    Console.WriteLine("BREW "+_brew.Id);
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

  public override bool CanExecute(Branch branch)
  {
    return branch.IsCastable(_cast) && !Deficit(branch).HasValue;
  }

  public override List<Step> ProduceSubSteps(Branch branch)
  {
    if (!branch.IsCastable(_cast))
      return new List<Step>{new StepReset()};

    var deficit = branch.Deficit.DeficitComponentPair();
    if (deficit.HasValue && deficit.Value.Item2 < -10)
      // consider impossible
      return null;

    if (deficit.HasValue)
      return Heuristic.FindCasts(branch, deficit.Value.Item1, deficit.Value.Item2);

    throw new Exception("should have been already executed");
  }

  public override void SimulateExecute(Branch branch)
  {
    var change = _cast.IngredientChange * _count;



    branch.Inventory = branch.Inventory + change;
    branch.Cast(_cast.Id);
  }

  public override void Execute()
  {
    Console.WriteLine($"CAST {_cast.Id} {_count}");
  }

  private (byte, short)? Deficit(Branch branch)
  {
    return (branch.Inventory + (_cast.IngredientChange*_count)).DeficitComponentPair();
  }
}

class StepReset : Step
{
  public override bool CanExecute(Branch gs)
  {
    return true;
  }

  public override List<Step> ProduceSubSteps(Branch gs)
  {
    return new List<Step>{gs.Steps[gs.Steps.Count - 2]};
  }

  public override void SimulateExecute(Branch branch)
  {
    branch.CastReset();
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

  public override bool CanExecute(Branch branch)
  {
    return _learn.TomeIndex <= branch.State.Myself.Inventory.T0;
  }

  public override List<Step> ProduceSubSteps(Branch branch)
  {
    return Heuristic.FindCasts(branch, 0, _learn.TomeIndex);
  }

  public override void SimulateExecute(Branch branch)
  {
    throw new NotImplementedException();
  }

  public override void Execute()
  {
    Console.WriteLine("LEARN "+_learn.Id);
  }
}

static class Heuristic
{
  public static List<Step> FindCasts(Branch branch, byte deficit, int value)
  {
    var res = new List<Step>();
    foreach (var cast in branch.State.Casts)
    {
      var change = cast.IngredientChange[deficit];
      if (change <= 0)
        continue;
      var count = CalcCasts(value, change, cast.IsRepeatable);
      res.Add((Step) new StepCast(cast, count));
    }

    return res;
  }
  public static List<Step> FindCasts_nice(Branch branch, byte deficit, int value)
  {
    return branch.State.Casts
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