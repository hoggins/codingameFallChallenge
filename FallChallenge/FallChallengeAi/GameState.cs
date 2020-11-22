using System;
using System.Collections.Generic;

class PlayerState
{
  public readonly Witch Witch = new Witch();
  public readonly List<BoardEntity> Casts = new List<BoardEntity>(32);
  public readonly List<BoardEntity> CastsAndLearn = new List<BoardEntity>(64);
}

class GameState
{
  public readonly PlayerState[] Players = new PlayerState[2]
  {
    new PlayerState(),
    new PlayerState(),
  };
  public int Tick;
  public readonly List<BoardEntity> Entities = new List<BoardEntity>();
  public readonly List<BoardEntity> Learns = new List<BoardEntity>();
  public readonly List<BoardEntity> Brews = new List<BoardEntity>();

  public PlayerState Myself => Players[0];


  public void AddEntity(BoardEntity e)
  {
    Entities.Add(e);
    switch (e.Type)
    {
      case EntityType.BREW:
        Brews.Add(e);
        break;
      case EntityType.CAST:
        Players[0].Casts.Add(e);
        Players[0].CastsAndLearn.Add(e);
        break;
      case EntityType.OPPONENT_CAST:
        Players[1].Casts.Add(e);
        Players[1].CastsAndLearn.Add(e);
        break;
      case EntityType.LEARN:
        Learns.Add(e);
        Players[0].CastsAndLearn.Add(e);
        Players[1].CastsAndLearn.Add(e);
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }

  public void Reset()
  {
    foreach (var entity in Entities)
    {
      Pool<BoardEntity>.Put(entity);
    }
    Entities.Clear();
    Learns.Clear();
    Brews.Clear();
    Players[0].Casts.Clear();
    Players[0].CastsAndLearn.Clear();
    Players[1].Casts.Clear();
    Players[1].CastsAndLearn.Clear();
  }
}