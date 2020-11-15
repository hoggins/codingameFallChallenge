using System.Collections.Generic;
using System.Linq;

class GameState
{
  public List<BoardEntity> Entities = new List<BoardEntity>();
  public List<Witch> Witches = new List<Witch>();

  public Witch Myself => Witches[0];
  public IEnumerable<BoardEntity> Learns => Entities.Where(x => x.IsLearn);
  public readonly List<BoardEntity> Casts = new List<BoardEntity>();
  public List<BoardEntity> Brews;

  public void AddEntity(BoardEntity e)
  {
    if (e.IsCast/* || e.IsLearn*/)
      Casts.Add(e);
    else
      Entities.Add(e);
  }
}