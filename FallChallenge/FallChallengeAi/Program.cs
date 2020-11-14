using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using FallChallengeAi;

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
        FindForward(gs.Brews.First(), gs).Execute();

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
    };

    while (!step.CanExecute(srcBranch))
    {
      var nextStep = step.ProduceSubSteps(srcBranch).First();
      step = nextStep;
    }

    return step;
  }

  class BranchComparer : IComparer<Branch>
  {
    public int Compare(Branch a, Branch b)
    {
      return a.Score.CompareTo(b.Score);
    }
  }

  static BoardMove FindForward(BoardEntity brew, GameState gs)
  {
    var srcBranch = new Branch
    {
      State = gs,
      Inventory = gs.Myself.Inventory,
    };

    var branches = new SortedCollection<Branch>(new BranchComparer());
    branches.Add(srcBranch);

    while (true)
    {
      var branch = branches.Dequeue();
      //foreach (var branch in branches)
      {
        if (branch.Moves.Count == 5)
        {
          return branch.Moves.First();
        }

        var possibleMoves = GenerateMoves(branch).ToArray();
        if (possibleMoves.Length == 0)
          continue;

        for (var i = 0; i < possibleMoves.Length - 1; i++)
        {
          var move = possibleMoves[i];
          var newBranch = srcBranch.Clone();
          newBranch.Simulate(move);
          branches.Add(newBranch);
        }

        branch.Simulate(possibleMoves.Last());
        branches.Add(branch);
      }

    }

    throw new Exception("dead end");
  }

  private static IEnumerable<BoardMove> GenerateMoves(Branch branch)
  {
    var anyRest = false;
    foreach (var cast in branch.State.Casts)
    {
      if (!branch.IsCastable(cast))
      {
        anyRest = true;
        continue;
      }

      for (var i = 1; i < 4 && branch.Inventory.AboveZero(cast.IngredientChange*i); i++)
      {
        yield return new MoveCast(cast, i);
      }
    }
    if (anyRest)
      yield return new MoveReset();
  }
}

abstract class BoardMove
{
  public abstract void Simulate(Branch branch);
  public abstract void Execute();
}

class MoveCast : BoardMove
{
  private readonly BoardEntity _cast;
  private readonly int _count;

  public MoveCast(BoardEntity cast, int count)
  {
    _cast = cast;
    _count = count;
  }

  public override void Simulate(Branch branch) => branch.Cast(_cast, _count);

  public override void Execute() => Console.WriteLine($"CAST {_cast.Id} {_count}");
}


class MoveReset : BoardMove
{
  public override void Simulate(Branch branch) => branch.CastReset();

  public override void Execute() => Console.WriteLine("REST");
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
