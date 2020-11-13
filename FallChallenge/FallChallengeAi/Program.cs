using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;

static class Program
{
  static void Main(string[] args)
  {
    var input = new Input();


    int? targetLearn = null;

    for(var tick = 0; ; ++tick)
    {
      var gs = new GameState();

      var actionCount = int.Parse(input.Line()); // the number of spells and recipes in play
      for (int i = 0; i < actionCount; i++)
        gs.Entities.Add(new BoardEntity(input.LineArgs()));

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
        FindStep(new StepBrew(gs.Brews.First()), gs).Execute();

      // in the first league: BREW <id> | WAIT; later: BREW <id> | CAST <id> [<times>] | LEARN <id> | REST | WAIT
    }
  }

  static Step FindStep(Step step, GameState gs)
  {
    while (!step.CanExecute(gs))
    {
      var nextStep = step.ProduceSubSteps(gs).First();
      step = nextStep;
    }

    return step;
  }

}
