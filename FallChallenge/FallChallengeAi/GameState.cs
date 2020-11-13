using System.Collections.Generic;
using System.Linq;

class GameState
{
  public List<BoardEntity> Entities = new List<BoardEntity>();
  public List<Witch> Witches = new List<Witch>();

  public Witch Myself => Witches[0];
  public IEnumerable<BoardEntity> Learns => Entities.Where(x => x.IsLearn);
  public IEnumerable<BoardEntity> Casts => Entities.Where(x => x.IsCast);
  public IEnumerable<BoardEntity> Brews => Entities.Where(x => x.IsBrew);
}