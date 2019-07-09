using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Cheesegreater.Duality.Plugin.SVG.Properties;
using Cheesegreater.Duality.Plugin.SVG.Resources;
using Duality;
using Duality.Components;
using Duality.Drawing;
using Duality.Editor;
using Duality.Resources;

namespace Cheesegreater.Duality.Plugin.SVG.Components
{
    [EditorHintCategory(ResNames.Category)]
    [EditorHintImage(ResNames.ImageSVGRenderer)]
    [RequiredComponent(typeof(Transform))]
    public class SVGRenderer : Component, ICmpRenderer, ICmpInitializable
    {
        private VisibilityFlag visibility = VisibilityFlag.AllGroups;
        public VisibilityFlag Visibility
        {
            get { return visibility; }
            set { visibility = value; }
        }

        private ContentRef<Resources.SVG> svgFile;
        public ContentRef<Resources.SVG> SVGFile
        {
            get { return svgFile; }
            set { svgFile = value; }
        }

        private float depthOffset = 0.0f;
        public float DepthOffset
        {
            get { return depthOffset; }
            set { depthOffset = value; }
        }

        private ContentRef<Texture> mainTexture = Texture.White;
        public ContentRef<Texture> MainTexture
        {
            get { return mainTexture; }
            set { mainTexture = value; }
        }

        [DontSerialize]
        private Canvas canvas;

        [DontSerialize]
        private ShapeDrawer drawer;

        [DontSerialize]
        private List<Vector3> verticiesList = new List<Vector3>();  // used for calculating BoundRadius

        [EditorHintFlags(MemberFlags.Invisible)]
        public float BoundRadius
        {
            get
            {
                if (verticiesList.Count == 0) return 50f;
                float distanceToFurthest = verticiesList.Max(v => v.Xy.Length);
                if (canvas != null) distanceToFurthest *= (new float[] { canvas.State.TransformScale.X, canvas.State.TransformScale.Y }).Max();
                return distanceToFurthest;
            }
        }

        public void GetCullingInfo(out CullingInfo info)
        {
            info.Position = GameObj.Transform.Pos;
            info.Visibility = visibility;

            if (visibility.HasFlag(VisibilityFlag.ScreenOverlay))
                info.Radius = float.MaxValue;
            else if (verticiesList.Count == 0)
                info.Radius = 50f;
            else
            {
                float distanceToFurthest = verticiesList.Max(v => v.Length);
                if (canvas != null) distanceToFurthest *= (new float[] { canvas.State.TransformScale.X, canvas.State.TransformScale.Y }).Max();
                info.Radius = distanceToFurthest;
            }
        }

        public void OnActivate()
        {
            canvas = new Canvas();
            drawer = new ShapeDrawer(canvas, GameObj.Transform);
        }

        public void OnDeactivate()
        {
        }

        public void Draw(IDrawDevice device)
        {
            if (svgFile.Res == null) return;
            if (!svgFile.IsLoaded) svgFile.EnsureLoaded();

            // verticiesList = new List<Vector3>();

            canvas.Begin(device);
            canvas.State.SetMaterial(new BatchInfo(DrawTechnique.Alpha, ColorRgba.White, mainTexture));  // enables transparency
            canvas.State.TransformScale = new Vector2(GameObj.Transform.Scale, GameObj.Transform.Scale);
            canvas.State.TransformAngle = GameObj.Transform.Angle;
            canvas.State.DepthOffset = depthOffset;

            try
            {
                foreach (Shape shape in svgFile.Res.Shapes)
                {
                    if (shape.GetType() == typeof(Resources.Rect))
                    {
                        Resources.Rect rect = (Resources.Rect)shape;

                        Vector3 position = new Vector3(rect.X.Invoke(GameObj), rect.Y.Invoke(GameObj), rect.Z.Invoke(GameObj));
                        Vector2 size = new Vector2(rect.Width.Invoke(GameObj), rect.Height.Invoke(GameObj));
                        ColorRgba fillColor = rect.FillColor.Invoke(GameObj);
                        ColorRgba strokeColor = rect.StrokeColor.Invoke(GameObj);
                        float strokeWidth = rect.StrokeWidth.Invoke(GameObj);
                        CornerType cornerType = rect.CornerType.Invoke(GameObj);

                        if (fillColor.A > 0) DrawRect(position, size, fillColor);
                        if (strokeColor.A > 0) DrawRectStroke(position, size, strokeColor, strokeWidth, cornerType);
                    }
                    else if (shape.GetType() == typeof(Circle))
                    {
                        Circle circle = (Circle)shape;

                        Vector3 position = new Vector3(circle.X.Invoke(GameObj), circle.Y.Invoke(GameObj), circle.Z.Invoke(GameObj));
                        float radius = circle.Radius.Invoke(GameObj);
                        ColorRgba fillColor = circle.FillColor.Invoke(GameObj);
                        ColorRgba strokeColor = circle.StrokeColor.Invoke(GameObj);
                        float strokeWidth = circle.StrokeWidth.Invoke(GameObj);

                        if (fillColor.A > 0) DrawCircle(position, radius, fillColor);
                        if (strokeColor.A > 0) DrawCircleStroke(position, radius, strokeColor, strokeWidth);
                    }
                    else if (shape.GetType() == typeof(Polygon))
                    {
                        Polygon polygon = (Polygon)shape;

                        Vector3 position = new Vector3(polygon.X.Invoke(GameObj), polygon.Y.Invoke(GameObj), polygon.Z.Invoke(GameObj));
                        List<Vector2> points = polygon.Points.Invoke(GameObj);
                        ColorRgba fillColor = polygon.FillColor.Invoke(GameObj);
                        ColorRgba strokeColor = polygon.StrokeColor.Invoke(GameObj);
                        float strokeWidth = polygon.StrokeWidth.Invoke(GameObj);
                        CornerType cornerType = polygon.CornerType.Invoke(GameObj);

                        if (fillColor.A > 0) DrawPolygon(position, points.ToArray(), fillColor);
                        if (strokeColor.A > 0) DrawPolygonStroke(position, points.ToArray(), strokeColor, strokeWidth, cornerType);
                    }
                    else if (shape.GetType() == typeof(Text))
                    {
                        Text text = (Text)shape;

                        Vector3 position = new Vector3(text.X.Invoke(GameObj), text.Y.Invoke(GameObj), text.Z.Invoke(GameObj));
                        string content = text.Content.Invoke(GameObj);
                        ColorRgba fillColor = text.FillColor.Invoke(GameObj);
                        ContentRef<Font> font = text.FontStyle.Invoke(GameObj);

                        DrawText(position, content, fillColor, font);
                    }
                }
            }
            finally
            {
                canvas.End();
            }
        }

        private void DrawRect(Vector3 position, Vector2 size, ColorRgba fillColor)
        {
            canvas.State.ColorTint = fillColor;
            drawer.DrawRect(position, size);

            verticiesList.Add(position);
            verticiesList.Add(position + new Vector3(size.X, 0f, 0f));
            verticiesList.Add(position + new Vector3(size));
            verticiesList.Add(position + new Vector3(0f, size.Y, 0f));
        }

        private void DrawRectStroke(Vector3 position, Vector2 size, ColorRgba strokeColor, float strokeWidth, CornerType cornerType)
        {
            canvas.State.ColorTint = strokeColor;
            float offset = strokeWidth / 2f;

            drawer.DrawRect(position + new Vector3(offset, -offset, 0f), new Vector2(size.X - strokeWidth, strokeWidth));
            drawer.DrawRect(position + new Vector3(size.X - offset, offset, 0f), new Vector2(strokeWidth, size.Y - strokeWidth));
            drawer.DrawRect(position + new Vector3(offset, size.Y - offset, 0f), new Vector2(size.X - strokeWidth, strokeWidth));
            drawer.DrawRect(position + new Vector3(-offset, offset, 0f), new Vector2(strokeWidth, size.Y - strokeWidth));

            if (cornerType == CornerType.Round)
            {
                drawer.DrawCircleSegment(position + new Vector3(offset, offset, 0f), strokeWidth, 3f * MathF.PiOver2, MathF.PiOver2);
                drawer.DrawCircleSegment(position + new Vector3(size.X - offset, offset, 0f), strokeWidth, 0f, MathF.PiOver2);
                drawer.DrawCircleSegment(position + new Vector3(size.X - offset, size.Y - offset, 0f), strokeWidth, MathF.PiOver2, MathF.PiOver2);
                drawer.DrawCircleSegment(position + new Vector3(offset, size.Y - offset, 0f), strokeWidth, MathF.Pi, MathF.PiOver2);
            }
            else if (cornerType == CornerType.Miter)
            {
                drawer.DrawRect(position + new Vector3(-offset, -offset, 0f), new Vector2(strokeWidth));
                drawer.DrawRect(position + new Vector3(size.X - offset, -offset, 0f), new Vector2(strokeWidth));
                drawer.DrawRect(position + new Vector3(size.X - offset, size.Y - offset, 0f), new Vector2(strokeWidth));
                drawer.DrawRect(position + new Vector3(-offset, size.Y - offset, 0f), new Vector2(strokeWidth));
            }

            verticiesList.Add(position - new Vector3(offset, offset, 0f));
            verticiesList.Add(position + new Vector3(size.X, 0f, 0f) + new Vector3(offset, -offset, 0f));
            verticiesList.Add(position + new Vector3(size) + new Vector3(offset, offset, 0f));
            verticiesList.Add(position + new Vector3(0f, size.Y, 0f) + new Vector3(-offset, offset, 0f));
        }

        private void DrawCircle(Vector3 position, float radius, ColorRgba fillColor)
        {
            canvas.State.ColorTint = fillColor;
            drawer.DrawCircle(position, radius);

            verticiesList.Add(position + new Vector3(0f, -radius, 0f));
            verticiesList.Add(position + new Vector3(radius, 0f, 0f));
            verticiesList.Add(position + new Vector3(0f, radius, 0f));
            verticiesList.Add(position + new Vector3(-radius, 0f, 0f));
        }

        private void DrawCircleStroke(Vector3 position, float radius, ColorRgba strokeColor, float strokeWidth)
        {
            canvas.State.ColorTint = strokeColor;
            drawer.DrawCircle(position, radius, strokeWidth);

            verticiesList.Add(position + new Vector3(0f, -radius - strokeWidth / 2f, 0f));
            verticiesList.Add(position + new Vector3(radius + strokeWidth / 2f, 0f, 0f));
            verticiesList.Add(position + new Vector3(0f, radius + strokeWidth / 2f, 0f));
            verticiesList.Add(position + new Vector3(-radius - strokeWidth / 2f, 0f, 0f));
        }

        private void DrawPolygon(Vector3 position, Vector2[] points, ColorRgba fillColor)
        {
            canvas.State.ColorTint = fillColor;
            drawer.DrawPolygon(position, points);

            foreach (Vector2 point in points)
                verticiesList.Add(new Vector3(point));
        }

        private void DrawPolygonStroke(Vector3 position, Vector2[] points, ColorRgba strokeColor, float strokeWidth, CornerType cornerType)
        {
            canvas.State.ColorTint = strokeColor;

            if (cornerType == CornerType.Miter)
                drawer.DrawPolygonOutline(position, points, strokeWidth);
            else if (cornerType == CornerType.Round)  // should not be used with transparent stroke color
            {
                List<Vector2[]> sides = new List<Vector2[]>();
                for (int i = 0; i < points.Length - 1; i++)
                    sides.Add(new Vector2[] { points[i], points[i + 1] });
                sides.Add(new Vector2[] { points.Last(), points[0] });
                sides.Add(sides.First());

                for (int i = 0; i < sides.Count - 1; i++)
                {
                    // draw the line segment
                    drawer.DrawThickLine(position, sides[i][0], sides[i][1], strokeWidth);

                    // draw the rounded corner
                    // don't mess with it, I don't remember how it works, it just does
                    Vector2 endpointDiff = sides[i][1] - sides[i][0];
                    Vector2 nextEndpointDiff = sides[i + 1][1] - sides[i + 1][0];
                    float startAngle;
                    float angleDiff = endpointDiff.Angle - nextEndpointDiff.Angle;
                    float angleDiffSign = MathF.Sign(angleDiff);
                    if (MathF.Abs(angleDiff) > MathF.Pi)
                        angleDiffSign *= -1f;
                    if (angleDiffSign >= 0f)
                        startAngle = nextEndpointDiff.PerpendicularRight.Angle;
                    else
                        startAngle = endpointDiff.PerpendicularLeft.Angle;
                    angleDiff = MathF.Abs(angleDiff);
                    if (angleDiff > MathF.Pi)
                        angleDiff = MathF.TwoPi - angleDiff;
                    drawer.DrawCircleSegment(position + new Vector3(sides[i][1]), strokeWidth / 2f, startAngle, angleDiff);
                }
            }

            // TODO: Make this fully accurate: Find the centroid of the polygon, find out how far each point is from the centriod,
            // and offset each point by (offset) in a direction towards the outside.
            foreach (Vector2 point in points)
                verticiesList.Add(new Vector3(point));
        }

        private void DrawText(Vector3 position, string text, ColorRgba fillColor, ContentRef<Font> font)
        {
            canvas.State.ColorTint = fillColor;
            canvas.State.TextFont = font;
            drawer.DrawText(position, text);

            Vector2 textSize = canvas.MeasureText(text);
            verticiesList.Add(position);
            verticiesList.Add(position + new Vector3(textSize.X, 0f, 0f));
            verticiesList.Add(position + new Vector3(textSize));
            verticiesList.Add(position + new Vector3(0f, textSize.Y, 0f));
        }
    }
}
