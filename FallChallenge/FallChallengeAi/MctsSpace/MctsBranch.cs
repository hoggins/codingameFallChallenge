using System.Collections.Generic;

public class MctsBranch
{
  public int StartTick;
  public readonly List<MctsCast> Casts = new List<MctsCast>(64);
  public List<BoardEntity> Brews;

  public void Reset()
  {
    Brews = null;
    Casts.Clear();
  }
}