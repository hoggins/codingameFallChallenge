class Witch
{
  public Ingredient Inventory;
  public int Score;

  public void ReadInit(string[] inputs)
  {
    Inventory.ReadInit(inputs, 0);
    Score = int.Parse(inputs[4]); // amount of rupees
  }
}