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

    // game loop
    while (true)
    {
      var gs = new GameState();

      var actionCount = int.Parse(input.Line()); // the number of spells and recipes in play
      for (int i = 0; i < actionCount; i++)
        gs.Entities.Add(new BoardEntity(input.LineArgs()));

      for (var i = 0; i < 2; i++)
        gs.Witches.Add(new Witch(input.LineArgs()));


      // Write an action using Console.WriteLine()
      // To debug: Console.Error.WriteLine("Debug messages...");

      FindStep(gs.Brews.First(), gs).Execute();

      // in the first league: BREW <id> | WAIT; later: BREW <id> | CAST <id> [<times>] | LEARN <id> | REST | WAIT
    }
  }

  static Step FindStep(BoardEntity brew, GameState gs)
  {
    Step step = new StepBrew(brew);

    while (!step.CanExecute(gs))
    {
      var nextStep = step.ProduceSubSteps(gs).First();
      step = nextStep;
    }

    return step;
  }

}
