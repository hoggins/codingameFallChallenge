using System;
using System.Collections.Generic;

public enum EntityType
{
  BREW,
  CAST,
  OPPONENT_CAST,
  LEARN,
}

class Brew : IDisposable
{
  public BoardEntity Value;
  public int LastRollOut = -1;
  public List<int> Iterations;

  public int ShortestPath = int.MaxValue;
  public MoveCast FirstStep;

  public Brew(BoardEntity value)
  {
    Value = value;

    Iterations = PoolList<List<int>>.Get();
  }

  public void Dispose()
  {
    PoolList<List<int>>.Put(Iterations);
  }
}

public  class BoardEntity : IReusable
{
  // the unique ID of this spell or recipe
  public int Id;
  // in the first league: BREW; later: CAST, OPPONENT_CAST, LEARN, BREW
  public EntityType Type;
  public Ingredient IngredientPay;
  public Ingredient IngredientChange;
  // the price in rupees if this is a potion
  public int Price;
  // 1 if this is a castable player spell
  public bool IsCastable;
  // the index in the tome if this is a tome spell, equal to the read-ahead tax
  public int TomeIndex;
  // the amount of taxed tier-0 ingredients you gain from learning this spell
  public int TaxCount;
  // 1 if this is a repeatable player spell
  public bool IsRepeatable;

  public bool IsBrew => Type == EntityType.BREW;
  public bool IsCast => Type == EntityType.CAST;
  public bool IsEnemyCast => Type == EntityType.OPPONENT_CAST;
  public bool IsLearn => Type == EntityType.LEARN;

  public int BrewIngredientCount;

  public void ReadInit(string[] inputs)
  {
    Id = int.Parse(inputs[0]);
    Type = ReadType(inputs[1]);
    IngredientChange.ReadInit(inputs, 2);
    Price = int.Parse(inputs[6]);
    TomeIndex = int.Parse(inputs[7]);
    TaxCount = int.Parse(inputs[8]);
    IsCastable = inputs[9] != "0";
    IsRepeatable = inputs[10] != "0";

    IngredientPay = new Ingredient
    {
      T0 = (short) (IngredientChange.T0 >= 0 ? 0 : -IngredientChange.T0),
      T1 = (short) (IngredientChange.T1 >= 0 ? 0 : -IngredientChange.T1),
      T2 = (short) (IngredientChange.T2 >= 0 ? 0 : -IngredientChange.T2),
      T3 = (short) (IngredientChange.T3 >= 0 ? 0 : -IngredientChange.T3),
    };

    // if (IsBrew)
      // BrewIngredientCount = IngredientChange.IngredientsCount();
  }

  private EntityType ReadType(string input)
  {
    switch (input[0])
    {
      case 'B': return EntityType.BREW;
      case 'C': return EntityType.CAST;
      case 'O': return EntityType.OPPONENT_CAST;
      case 'L': return EntityType.LEARN;
      default:
        throw new NotImplementedException();
    }

  }

  public override string ToString()
  {
    return $"{Id}: {IngredientChange}";
  }

  public void Reset()
  {

  }
}