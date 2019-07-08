using Duality;
using Duality.Components;
using Duality.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cheesegreater.Duality.Plugin.SVG.Components
{
    public class ShapeDrawer
    {
        [DontSerialize]
        private Canvas canvas;

        [DontSerialize]
        private Transform transform;

        public ShapeDrawer(Canvas canvas, Transform transform)
        {
            this.canvas = canvas;
            this.transform = transform;
        }

        public void DrawRect(Vector3 position, Vector2 size)
        {
            OffsetTransformHandle(position.Xy);
            canvas.FillRect(transform.Pos.X, transform.Pos.Y, transform.Pos.Z + position.Z, size.X, size.Y);
        }

        public void DrawCircleSegment(Vector3 position, float radius, float start, float angle)
        {
            OffsetTransformHandle(position.Xy);
            canvas.FillCircleSegment(transform.Pos.X, transform.Pos.Y, transform.Pos.Z + position.Z, radius, start, start + angle);
        }

        public void DrawCircle(Vector3 position, float radius)
        {
            OffsetTransformHandle(position.Xy);
            canvas.FillCircle(transform.Pos.X, transform.Pos.Y, transform.Pos.Z + position.Z, radius);
        }
        public void DrawCircle(Vector3 position, float radius, float donutThickness)
        {
            OffsetTransformHandle(position.Xy);
            canvas.FillCircleSegment(transform.Pos.X, transform.Pos.Y, transform.Pos.Z + position.Z, radius + (donutThickness / 2f), 0f, MathF.TwoPi, donutThickness);
        }

        public void DrawPolygon(Vector3 position, Vector2[] points)
        {
            OffsetTransformHandle(position.Xy);
            canvas.FillPolygon(points, transform.Pos.X, transform.Pos.Y, transform.Pos.Z + position.Z);
        }

        public void DrawPolygonOutline(Vector3 position, Vector2[] points, float lineWidth)
        {
            OffsetTransformHandle(position.Xy);
            canvas.FillPolygonOutline(points, lineWidth, transform.Pos.X, transform.Pos.Y, transform.Pos.Z + position.Z);
        }

        public void DrawThickLine(Vector3 position, Vector2 startPos, Vector2 endPos, float lineWidth)
        {
            OffsetTransformHandle(position.Xy + startPos);
            canvas.FillThickLine(transform.Pos.X, transform.Pos.Y, transform.Pos.Z + position.Z,
                endPos.X - startPos.X + transform.Pos.X, endPos.Y - startPos.Y + transform.Pos.Y, transform.Pos.Z + position.Z, lineWidth);
        }

        private void OffsetTransformHandle(Vector2 shapeHandleLocation)
        {
            canvas.State.TransformHandle = -shapeHandleLocation;
        }
    }
}
