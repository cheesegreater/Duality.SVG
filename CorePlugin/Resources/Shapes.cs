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
        public Func<GameObject, float> X = (obj) => { return 0.0f; };
        public Func<GameObject, float> Y = (obj) => { return 0.0f; };
        public Func<GameObject, float> Z = (obj) => { return 0.0f; };
    }

    public class Rect : Shape
    {
        public Func<GameObject, float> Width = (obj) => { return 0.0f; };
        public Func<GameObject, float> Height = (obj) => { return 0.0f; };
        public Func<GameObject, ColorRgba> FillColor = (obj) => { return new ColorRgba(0, 0, 0, 0); };
        public Func<GameObject, ColorRgba> StrokeColor = (obj) => { return new ColorRgba(0, 0, 0, 0); };
        public Func<GameObject, float> StrokeWidth = (obj) => { return 2.0f; };
        public Func<GameObject, CornerType> CornerType = (obj) => { return 0; };
    }

    public class Circle : Shape
    {
        public Func<GameObject, float> Radius = (obj) => { return 0.0f; };
        public Func<GameObject, ColorRgba> FillColor = (obj) => { return new ColorRgba(0, 0, 0, 0); };
        public Func<GameObject, ColorRgba> StrokeColor = (obj) => { return new ColorRgba(0, 0, 0, 0); };
        public Func<GameObject, float> StrokeWidth = (obj) => { return 2.0f; };
    }

    public class Polygon : Shape
    {
        public Func<GameObject, List<Vector2>> Points = (obj) => { return new List<Vector2>(); };
        public Func<GameObject, ColorRgba> FillColor = (obj) => { return new ColorRgba(0, 0, 0, 0); };
        public Func<GameObject, ColorRgba> StrokeColor = (obj) => { return new ColorRgba(0, 0, 0, 0); };
        public Func<GameObject, float> StrokeWidth = (obj) => { return 2.0f; };
        public Func<GameObject, CornerType> CornerType = (obj) => { return 0; };
    }

    public class Text : Shape
    {
        public Func<GameObject, string> Content = (obj) => { return ""; };
        public Func<GameObject, ColorRgba> FillColor = (obj) => { return ColorRgba.Black; };
        public Func<GameObject, ContentRef<Font>> FontStyle = (obj) => { return Font.GenericMonospace8;  };
    }
}
