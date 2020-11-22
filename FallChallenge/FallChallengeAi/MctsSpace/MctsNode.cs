using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class MctsNode
{
  public MctsNode Parent;
  public int Depth;
  public double Score;
  public Ingredient Inventory;
  public long UsedCasts;
  public long LearnedCasts;
  public long CompleteBrews;

  public double Value;
  public int Number;

  public int? ActionIdx;

  public readonly List<MctsNode> Children = new List<MctsNode>(64);

  public double Ucb => (Value / Number) + 50*Math.Sqrt(Math.Log(Parent.Number)/Number);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool IsCastable(int idx) => (UsedCasts & (1 << idx)) == 0;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool IsBrewCompleted(int idx) => (CompleteBrews & (1 << idx)) != 0;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public bool IsLearned(int idx) => (LearnedCasts & (1 << idx)) != 0;

  public void Reset()
  {
    Children.Clear();
    Parent = null;
    Depth = 0;
    Score = 0;
    UsedCasts = 0;
    LearnedCasts = 0;
    CompleteBrews = 0;
    Value = 0;
    Number = 0;
    ActionIdx = null;
  }
}