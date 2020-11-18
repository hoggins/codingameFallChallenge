using System.Collections.Generic;
using System.Linq;

class GlobalState
{
  public int[] LastScore = new []{0,0};
  public int[] BrewsCompleted = new []{0,0};

  public void Update(GameState gs)
  {
    UpdateWitch(0);
    UpdateWitch(1);
    void UpdateWitch(int idx)
    {
      if (LastScore[idx] != gs.Witches[idx].Score)
      {
        LastScore[idx] = gs.Witches[idx].Score;
        BrewsCompleted[idx] += 1;
      }
    }
  }
}

class GameState
{
  public List<BoardEntity> Entities = new List<BoardEntity>();
  public List<Witch> Witches = new List<Witch>();

  public Witch Myself => Witches[0];
  public IEnumerable<BoardEntity> Learns => Entities.Where(x => x.IsLearn);
  public readonly List<BoardEntity> Casts = new List<BoardEntity>();
  public readonly List<BoardEntity> CastsAndLearn = new List<BoardEntity>();
  public List<BoardEntity> Brews;

  public void AddEntity(BoardEntity e)
  {
    if (e.IsLearn)
      CastsAndLearn.Add(e);
    if (e.IsCast)
    {
      Casts.Add(e);
      CastsAndLearn.Add(e);
    }
    else
    {
      Entities.Add(e);
    }
  }
}