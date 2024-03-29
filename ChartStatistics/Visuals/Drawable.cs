﻿using System.Drawing;

namespace ChartStatistics; 

public abstract class Drawable {
    public enum DrawLayer {
        Zone,
        Grid,
        BeatHold,
        Hold,
        Beat,
        Match,
        Tap,
        LineGraph,
        Label
    }
        
    private static int instanceCounter;
        
    public double Start { get; }
        
    public double End { get; }
        
    public DrawLayer Layer { get; }

    private readonly int id;

    protected Drawable(double start, double end, DrawLayer layer) {
        Start = start;
        End = end;
        Layer = layer;
        id = instanceCounter;
        instanceCounter++;
    }

    public abstract void Draw(GraphicsPanel panel, Graphics graphics);

    public override bool Equals(object obj) => obj is Drawable other && other.id == id;

    public override int GetHashCode() => id;
}