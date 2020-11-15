using System.Collections.Generic;
using System.Linq;

class GameState
{
  public List<BoardEntity> Entities = new List<BoardEntity>();
  public List<Witch> Witches = new List<Witch>();

  public Witch Myself => Witches[0];
  public IEnumerable<BoardEntity> Learns => Entities.Where(x => x.IsLearn);
  public readonly List<BoardEntity> Casts = new List<BoardEntity>();
  public IEnumerable<BoardEntity> Brews => Entities.Where(x => x.IsBrew);

  public void AddEntity(BoardEntity e)
  {
    if (e.IsCast || e.IsLearn)
      Casts.Add(e);
    else
      Entities.Add(e);
  }
}