using System;
using System.Collections.Generic;
using System.Linq;

class Branch
{
  public int Score;
  public GameState State;
  public BoardEntity Brew;
  public Ingredient Inventory;
  public List<BoardMove> Moves = new List<BoardMove>();

  public Dictionary<int, EntityOverride> Overrides = new Dictionary<int, EntityOverride>();

  public bool IsBrewComplete => Inventory.AboveZero(Brew.IngredientChange);

  public Branch Clone()
  {
    return new Branch
    {
      State = State,
      Brew = Brew,
      Inventory = Inventory,
      Moves = new List<BoardMove>(Moves),
      Overrides = Overrides.ToDictionary(x=>x.Key, x=>x.Value.Clone())
    };
  }

  public void Evaluate()
  {
    //Score = State.Brews.Sum(ScoreBrew);
    Score = ScoreBrew(State.Brews.First());

    int ScoreBrew(BoardEntity brew)
    {
      var s = brew.IngredientChange.T0 >= 0 ? 1000 : Math.Abs(Inventory.T0 + brew.IngredientChange.T0);
      s += brew.IngredientChange.T1 >= 0 ? 1000 : Math.Abs(Inventory.T1 + brew.IngredientChange.T1);
      s += brew.IngredientChange.T2 >= 0 ? 1000 : Math.Abs(Inventory.T2 + brew.IngredientChange.T2);
      s += brew.IngredientChange.T3 >= 0 ? 1000 : Math.Abs(Inventory.T3 + brew.IngredientChange.T3);
      return s;
    }
  }

  public void Simulate(BoardMove move)
  {
    move.Simulate(this);
    Moves.Add(move);
    Evaluate();
  }

  public bool IsCastable(BoardEntity cast)
  {
    if (Overrides.TryGetValue(cast.Id, out var entity))
      return entity.IsCastable;
    return cast.IsCastable;
  }

  public void Cast(BoardEntity cast, int count)
  {
    var change = cast.IngredientChange * count;
    Inventory += change;
    var over = GetOrAddOverride(cast.Id);
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
    for (var i = 0; i < Moves.Count && i < 10; i++)
    {
      var step = Moves[i];
      Output.Write($"{step.GetType().Name}  => ");
    }
  }
}