using System.Collections.Generic;

class MctsBranch
{
  public int StartTick;
  public readonly List<MctsCast> Casts = new List<MctsCast>(64);
  public List<BoardEntity> Brews;

  public void Reset()
  {
    Brews = null;
    foreach (var cast in Casts)
    {
      Pool<MctsCast>.Put(cast);
    }
    Casts.Clear();
  }
}