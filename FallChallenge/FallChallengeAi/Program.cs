using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

static class Program
{
  static void Main(string[] args)
  {
    try
    {
      RunGame();
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      Console.WriteLine("done");
      Console.ReadKey();
    }
  }

  private static void RunGame()
  {
    var input = new Input();


    int? targetLearn = null;

    for (var tick = 0;; ++tick)
    {
      var gs = new GameState();

      var actionCount = int.Parse(input.Line()); // the number of spells and recipes in play
      for (int i = 0; i < actionCount; i++)
        gs.AddEntity(new BoardEntity(input.LineArgs()));

      for (var i = 0; i < 2; i++)
        gs.Witches.Add(new Witch(input.LineArgs()));

      // To debug: Console.Error.WriteLine("Debug messages...");

      if (targetLearn.HasValue && gs.Learns.All(x => x.Id != targetLearn.Value))
      {
        targetLearn = null;
      }

      if (!targetLearn.HasValue && gs.Casts.Count() < 10)
      {
        targetLearn = gs.Learns.First(x => x.TomeIndex == 0).Id;
      }

      if (targetLearn.HasValue)
      {
        var learn = gs.Learns.First(x => x.Id == targetLearn);
        FindStep(new StepLearn(learn), gs).Execute();
      }
      else
        FindMultiBranch(gs.Brews.First(), gs).Execute();

      // in the first league: BREW <id> | WAIT; later: BREW <id> | CAST <id> [<times>] | LEARN <id> | REST | WAIT
    }
  }

  static Step FindStep(Step step, GameState gs)
  {
    var srcBranch = new Branch
    {
      State = gs,
      Brew = null,
      Inventory = gs.Myself.Inventory,
      Steps = new List<Step>{step}
    };

    while (!step.CanExecute(srcBranch))
    {
      var nextStep = step.ProduceSubSteps(srcBranch).First();
      step = nextStep;
    }

    return step;
  }

  static Step FindMultiBranch(BoardEntity brew, GameState gs)
  {
    var srcBranch = new Branch
    {
      State = gs,
      Brew = brew,
      Inventory = gs.Myself.Inventory,
    };

    var branches = new List<Branch>();
    var nextBranches = new List<Branch>();

    branches.Add(srcBranch);

    while (branches.Any())
    {
      foreach (var branch in branches)
      {
        if (branch.IsBrewComplete)
        {
          return branch.Steps.Last();
        }

        var nextSteps = branch.Steps.Last().ProduceSubSteps(branch);
        if (nextSteps == null || nextSteps.Count == 0)
          continue;

        for (var i = 0; i < nextSteps.Count - 1; i++)
        {
          var step = nextSteps[i];
          var newBranch = srcBranch.Clone();
          newBranch.ApplyStep(step);
          nextBranches.Add(newBranch);
        }

        branch.ApplyStep(nextSteps.Last());
        nextBranches.Add(branch);
      }

      var pass = branches;
      branches = nextBranches;
      nextBranches = pass;
      nextBranches.Clear();
    }

    throw new Exception("dead end");
    //return step;
  }

}

class Branch
{
  public GameState State;
  public BoardEntity Brew;
  public Ingredient Inventory;
  public List<Step> Steps;

  public Dictionary<int, EntityOverride> Overrides = new Dictionary<int, EntityOverride>();

  public bool IsBrewComplete => Inventory.AboveZero(Brew.IngredientChange);

  public Branch Clone()
  {
    return new Branch
    {
      State = State,
      Brew = Brew,
      Inventory = Inventory,
      Steps = new List<Step>(Steps),
      Overrides = Overrides.ToDictionary(x=>x.Key, x=>x.Value.Clone())
    };
  }

  public void ApplyStep(Step step)
  {
    Steps.Add(step);
    step.SimulateExecute(this);
  }

  public bool IsCastable(BoardEntity cast)
  {
    if (Overrides.TryGetValue(cast.Id, out var entity))
      return entity.IsCastable;
    return cast.IsCastable;
  }

  public void Cast(int castId)
  {
    var over = GetOrAddOverride(castId);
    over.IsCastable = false;
  }

  public void CastReset()
  {
    foreach (var pair in Overrides)
    {
      pair.Value.IsCastable = true;
    }
  }

  private EntityOverride GetOrAddOverride(int castId)
  {
    if (!Overrides.TryGetValue(castId, out var entity))
      Overrides[castId] = entity = new EntityOverride();
    return entity;

  }

  public void Print()
  {
    for (var i = 0; i < Steps.Count && i < 10; i++)
    {
      var step = Steps[i];
      Output.Write($"{step.GetType().Name}  => ");
    }
  }
}

class EntityOverride
{
  public bool IsCastable;

  public EntityOverride Clone()
  {
    return new EntityOverride
    {
      IsCastable = IsCastable
    };
  }
}
