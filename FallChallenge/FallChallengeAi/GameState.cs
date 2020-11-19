using System;
using System.Collections.Generic;
using System.Linq;

class PlayerState : IDisposable
{
  public Witch Witch;
  public readonly List<BoardEntity> Casts;
  public readonly List<BoardEntity> CastsAndLearn;

  public PlayerState()
  {
    Casts = PoolList<List<BoardEntity>>.Get();
    CastsAndLearn = PoolList<List<BoardEntity>>.Get();
  }

  public void Dispose()
  {
    PoolList<List<BoardEntity>>.Put(Casts);
    PoolList<List<BoardEntity>>.Put(CastsAndLearn);
  }
}

class GameState : IDisposable
{
  public readonly PlayerState[] Players = new PlayerState[2];

  public List<BoardEntity> Entities = new List<BoardEntity>();

  public readonly List<BoardEntity> Learns;
  public List<BoardEntity> Brews;

  public PlayerState Myself => Players[0];

  public GameState()
  {
    Players[0] = new PlayerState();
    Players[1] = new PlayerState();
    Learns = PoolList<List<BoardEntity>>.Get();
  }

  public void Dispose()
  {
    Players[0].Dispose();
    Players[1].Dispose();
    PoolList<List<BoardEntity>>.Put(Learns);
  }

  public void AddEntity(BoardEntity e)
  {
    if (e.IsLearn)
    {
      Learns.Add(e);
      Players[0].CastsAndLearn.Add(e);
      Players[1].CastsAndLearn.Add(e);
    }

    if (e.IsCast)
    {
      Players[0].Casts.Add(e);
      Players[0].CastsAndLearn.Add(e);
    }
    else if (e.IsEnemyCast)
    {
      Players[1].Casts.Add(e);
      Players[1].CastsAndLearn.Add(e);
    }
    else
    {
      Entities.Add(e);
    }
  }
}