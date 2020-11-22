﻿#define FOR_DEBUG
//#define SNIFF
#define PUBLISHED
//#define PROFILER

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;


static class Program
{
  private const bool Published =
#if PUBLISHED
    true;
#else
    false;
#endif

  private const bool Sniff =
#if SNIFF
    true;
#else
    false;
#endif


  public static Random Rnd = new Random(1);

  public static string Comment = string.Empty;

  public static void AddComment(object s) => Comment += " " + s;

  static void Main(string[] args)
  {
#if FOR_DEBUG
    while (!Debugger.IsAttached)
      Thread.Sleep(3);
#endif


    Pool<MctsNode>.Allocate(55000);
    Pool<BoardEntity>.Allocate(128);
    Pool<MctsCast>.Allocate(128);
    Pool<MctsBranch>.Allocate(2);
    GC.Collect();

    GC.Collect(1, GCCollectionMode.Forced);
    GC.Collect(2, GCCollectionMode.Forced);
    GC.Collect(2, GCCollectionMode.Forced);

#if PROFILER
      JetBrains.Profiler.Api.MeasureProfiler.StartCollectingData();
      try
      {
#endif


      RunGame();


#if PROFILER
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
    }
      JetBrains.Profiler.Api.MeasureProfiler.SaveData();
      JetBrains.Profiler.Api.MeasureProfiler.StopCollectingData();
      //JetBrains.Profiler.Api.MeasureProfiler.Detach();

#endif
  }

  private static void RunGame()
  {
    Output.Init(Sniff);
    var input = new Input(Published, Sniff);



    var gs = new GameState();
    for (var tick = 0;; ++tick)
    {
      Comment = string.Empty;
      gs.Reset();
      gs.Tick = tick;

      var actionCount = int.Parse(input.Line()); // the number of spells and recipes in play
      for (int i = 0; i < actionCount; i++)
      {
        var e = Pool<BoardEntity>.Get();
        e.ReadInit(input.LineArgs());
        gs.AddEntity(e);
      }

      for (var i = 0; i < 2; i++)
        gs.Players[i].Witch.ReadInit(input.LineArgs());

      // gs.Brews = gs.Entities
        // .Where(x => x.IsBrew)
        // .OrderByDescending(x => x.Price)
        // .ToList();

      // To debug: Console.Error.WriteLine("Debug messages...");

      //RunMc(gs);
      //GC.TryStartNoGCRegion(2_000);
      RunMcts(gs);
      //GC.EndNoGCRegion();


      //
      // in the first league: BREW <id> | WAIT; later: BREW <id> | CAST <id> [<times>] | LEARN <id> | REST | WAIT
    }
  }

  private static void RunMcts(GameState gs)
  {
    var sw = Stopwatch.StartNew();

    /*using (var branch = new Branch
    {
      InitialInventory = gs.Players[0].Witch.Inventory,
      Learns = gs.Learns,
      Casts = gs.Players[0].Casts,
      CastsAndLearn = gs.Players[0].CastsAndLearn,
      Brews = gs.Brews.Select(x => new Brew(x)).ToList(),
    })
    {
      if (Mc.LearnInitial(branch, out var move))
      {
        Console.WriteLine(move.GetCommand());
        return;
      }
    }

    var readyBrew = gs.Brews.FirstOrDefault(x => gs.Myself.Witch.Inventory.CanPay(x.IngredientPay));
    if (readyBrew != null)
    {
      Console.WriteLine("BREW " + readyBrew.Id);
      return;
    }*/

    var command = Mcts.ProduceCommand(gs, sw);

    AddComment(sw.ElapsedMilliseconds);
    Console.WriteLine(command + Comment);
  }

  private static void RunMc(GameState gs)
  {
    var sw = Stopwatch.StartNew();

    var branch = new Branch
    {
      InitialInventory = gs.Players[0].Witch.Inventory,
      Learns = gs.Learns,
      Casts = gs.Players[0].Casts,
      CastsAndLearn = gs.Players[0].CastsAndLearn,
      Brews = gs.Brews.Select(x => new Brew(x)).ToList(),
    };

    var cmd = Mc.FindForward(branch, sw);
    branch.Dispose();
    AddComment(sw.ElapsedMilliseconds);
    Console.WriteLine(cmd.GetCommand() + Comment);
  }
}