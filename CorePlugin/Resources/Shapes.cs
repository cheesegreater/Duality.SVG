using System;
using System.Collections.Generic;
using System.Linq;
using Cheesegreater.Duality.Plugin.SVG.Components;
using Duality;
using Duality.Drawing;
using Duality.Resources;

namespace Cheesegreater.Duality.Plugin.SVG.Resources
{
    public enum CornerType
    {
        Miter = 0,
        Round = 1
    }

    public class Shape
    {
        public Func<float> X = () => { return 0.0f; };
        public Func<float> Y = () => { return 0.0f; };
        public Func<float> Z = () => { return 0.0f; };
    }

    public class Rect : Shape
    {
        public Func<float> Width = () => { return 0.0f; };
        public Func<float> Height = () => { return 0.0f; };
        public Func<ColorRgba> FillColor = () => { return new ColorRgba(0, 0, 0, 0); };
        public Func<ColorRgba> StrokeColor = () => { return new ColorRgba(0, 0, 0, 0); };
        public Func<float> StrokeWidth = () => { return 2.0f; };
        public Func<CornerType> CornerType = () => { return 0; };
    }

    public class Circle : Shape
    {
        public Func<float> Radius = () => { return 0.0f; };
        public Func<ColorRgba> FillColor = () => { return new ColorRgba(0, 0, 0, 0); };
        public Func<ColorRgba> StrokeColor = () => { return new ColorRgba(0, 0, 0, 0); };
        public Func<float> StrokeWidth = () => { return 2.0f; };
    }

    public class Polygon : Shape
    {
        public Func<List<Vector2>> Points = () => { return new List<Vector2>(); };
        public Func<ColorRgba> FillColor = () => { return new ColorRgba(0, 0, 0, 0); };
        public Func<ColorRgba> StrokeColor = () => { return new ColorRgba(0, 0, 0, 0); };
        public Func<float> StrokeWidth = () => { return 2.0f; };
        public Func<CornerType> CornerType = () => { return 0; };
    }

    public class Text : Shape
    {
        public Func<string> Content = () => { return ""; };
        public Func<ColorRgba> FillColor = () => { return ColorRgba.Black; };
        public Func<ContentRef<Font>> FontStyle = () => { return Font.GenericMonospace8;  };
    }
}
