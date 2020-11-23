//#define DRAWER
#if DRAWER
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

public class Drawer
{
  const int NodeWidth = 50;
  const int NodeHeight = 80;
  const string dir = "dump";

  class DrawContext : IDisposable
  {
    public Graphics Graphics;
    public Font Font;
    public SolidBrush FontBrush;

    public DrawContext(Graphics graphics)
    {
      Graphics = graphics;
      Font = new Font("Arial", 11);
      FontBrush = new SolidBrush(Color.Blue);
    }

    public void Dispose()
    {
      Font?.Dispose();
      FontBrush?.Dispose();
    }
  }

  private static bool _isInitialized;

  public static void TryInit()
  {
    if (_isInitialized)
      return;
    _isInitialized = true;
    if (Directory.Exists(dir))
      Directory.Delete(dir,true);
    if (!Directory.Exists(dir))
      Directory.CreateDirectory(dir);
  }

  public static void Draw(MctsNode root, MctsBranch branch)
  {
    TryInit();

    var layers = new List<List<MctsNode>>();
    layers.Add(new List<MctsNode>{root});
    while (true)
    {
      var layer = new List<MctsNode>();
      foreach (var node in layers.Last())
      {
        foreach (var child in node.Children.OrderByDescending(x=>x.Number))
        {
          if (child.Number != 0)
            layer.Add(child);
        }
      }

      if (!layer.Any())
        break;
      layers.Add(layer);
    }

    var maxLayerNodes = Math.Min(300, layers.Max(x => x.Count));

    var bmp = new Bitmap(maxLayerNodes*NodeWidth, layers.Count*NodeHeight);
    using (var cx = new DrawContext(Graphics.FromImage(bmp)))
    {
      for (var h = 0; h < layers.Count; h++)
      {
        var layer = layers[h];
        for (var w = 0; w < layer.Count; w++)
        {
          var node = layer[w];
          DrawNode(cx, h, w, node, branch);
        }
      }
    }

    bmp.Save($"{dir}/frame_{branch.StartTick}.png", ImageFormat.Png);
    bmp.Dispose();
  }

  private static void DrawNode(DrawContext cx, int h, int w, MctsNode node, MctsBranch branch)
  {
    string name;
    if (!node.ActionIdx.HasValue)
      name = "R";
    else if (node.ActionIdx.Value >= 0)
    {
      var cast = branch.Casts[node.ActionIdx.Value];
      if (cast.IsLearn && !node.IsLearned(cast.EntityIdx))
        name = "L " + cast.Cast.Id;
      else
        name = "C " + cast.Cast.Id;
    }
    else
    {
      var brew = branch.Brews[-(node.ActionIdx.Value + 1)];
      name = "B " + brew.Id;
    }

    var x = w * NodeWidth;
    var y = h * NodeHeight;
    cx.Graphics.DrawString(name, cx.Font, cx.FontBrush, x, y);
    cx.Graphics.DrawString("n:"+node.Number.ToString("0"), cx.Font, cx.FontBrush, x, y+20);
    cx.Graphics.DrawString("s:"+node.Score.ToString("0"), cx.Font, cx.FontBrush, x, y+40);
  }
}
#endif