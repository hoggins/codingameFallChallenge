public class MctsCast
{
  public int EntityIdx;
  public int BranchRefIdx;
  public BoardEntity Cast;
  public int Count;

  public int Size;
  public Ingredient Required;
  public Ingredient TotalChange;

  public bool IsLearn;
  public Ingredient RequiredLearn;
  public Ingredient TotalChangeLearn;

  public void Init(int entityIdx, int branchRefIdx, BoardEntity cast, int count)
  {
    EntityIdx = entityIdx;
    BranchRefIdx = branchRefIdx;
    Cast = cast;
    Count = count;

    Required = Cast.IngredientPay * Count;
    TotalChange = Cast.IngredientChange * Count;
    if (Cast.Type == EntityType.LEARN)
    {
      IsLearn = true;
      RequiredLearn = new Ingredient((short)Cast.TomeIndex,0,0,0);
      TotalChangeLearn =  new Ingredient((short)(-Cast.TomeIndex + Cast.TaxCount),0,0,0);
    }
    else
    {
      IsLearn = false;
    }

    Size = TotalChange.Total();
  }
}