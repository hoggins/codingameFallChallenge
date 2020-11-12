class Witch
{
  public Ingredient Inventory;
  public int Score;

  public Witch(string[] inputs)
  {
    Inventory = new Ingredient(inputs, 0);
    Score = int.Parse(inputs[4]); // amount of rupees
  }
}