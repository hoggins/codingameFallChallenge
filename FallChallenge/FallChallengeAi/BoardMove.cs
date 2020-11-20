using System;
using System.Linq;

abstract class BoardMove : IDisposable
{
  public abstract void Simulate(Branch branch);
  public abstract string GetCommand();
  public abstract void Dispose();
}

class MoveReset : BoardMove
{
  public override void Simulate(Branch branch)
  {
  }

  public override string GetCommand() => "REST";

  public override void Dispose()
  {
  }
}

class MoveLearn : BoardMove
{
  private readonly BoardEntity _learn;

  public MoveLearn(BoardEntity learn)
  {
    _learn = learn;
  }

  public MoveLearn(Branch gs)
  {
    _learn = gs.Learns.First(x => x.TomeIndex == 0);
  }

  public override void Simulate(Branch branch)
  {
  }

  public override string GetCommand() => "LEARN " + _learn.Id;

  public override void Dispose()
  {

  }
}

class MoveBrew : BoardMove
{
  private readonly BoardEntity _brew;

  public MoveBrew(BoardEntity brew)
  {
    _brew = brew;
  }

  public override void Simulate(Branch branch)
  {
  }

  public override string GetCommand() => "BREW " + _brew.Id;

  public override void Dispose()
  {

  }
}