using System;

enum EntityType
{
  BREW,
  CAST,
  OPPONENT_CAST,
  LEARN,
}

class BoardEntity
{
  // the unique ID of this spell or recipe
  public int Id;
  // in the first league: BREW; later: CAST, OPPONENT_CAST, LEARN, BREW
  public EntityType Type;
  public Ingredient IngredientChange;
  // the price in rupees if this is a potion
  public int Price;
  // 1 if this is a castable player spell
  public bool IsCastable;
  // the index in the tome if this is a tome spell, equal to the read-ahead tax
  public int TomeIndex;
  // 1 if this is a repeatable player spell
  public bool IsRepeatable;

  public bool IsBrew => Type == EntityType.BREW;
  public bool IsCast => Type == EntityType.CAST;
  public bool IsLearn => Type == EntityType.LEARN;

  public BoardEntity(string[] inputs)
  {
    Id = int.Parse(inputs[0]);
    Type = (EntityType) Enum.Parse(typeof(EntityType), inputs[1]);
    IngredientChange = new Ingredient(inputs, 2);
    Price = int.Parse(inputs[6]);
    TomeIndex = int.Parse(inputs[7]);
    int taxCount = int.Parse(inputs[8]); //  the amount of taxed tier-0 ingredients you gain from learning this spell
    IsCastable = inputs[9] != "0";
    IsRepeatable = inputs[10] != "0";
  }
}