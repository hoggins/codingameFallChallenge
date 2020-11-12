using System;

enum EntityType
{
  BREW
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

  public BoardEntity(string[] inputs)
  {
    Id = int.Parse(inputs[0]);
    Type = (EntityType) Enum.Parse(typeof(EntityType), inputs[1]);
    IngredientChange = new Ingredient(inputs, 2);
    Price = int.Parse(inputs[6]);
    int tomeIndex =
      int.Parse(inputs[7]); // in the first two leagues: always 0; later: the index in the tome if this is a tome spell,
    // equal to the read-ahead tax
    int taxCount =
      int.Parse(inputs[8]); // in the first two leagues: always 0; later: the amount of taxed tier-0 ingredients you
    // gain from learning this spell
    bool castable = inputs[9] != "0"; // in the first league: always 0; later: 1 if this is a castable player spell
    bool
      repeatable =
        inputs[10] != "0"; // for the first two leagues: always 0; later: 1 if this is a repeatable player spell
  }
}