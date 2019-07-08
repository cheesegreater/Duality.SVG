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

        public List<SVGDeclaredField> DeclaredFields { get; set; }

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

            verticiesList = new List<Vector3>();

            canvas.Begin(device);
            canvas.State.SetMaterial(new BatchInfo(DrawTechnique.Alpha, ColorRgba.White, mainTexture));  // enables transparency
            canvas.State.TransformScale = new Vector2(GameObj.Transform.Scale, GameObj.Transform.Scale);
            canvas.State.TransformAngle = GameObj.Transform.Angle;
            canvas.State.DepthOffset = depthOffset;

            try
            {
                XmlReader xml = XmlReader.Create(GenerateStreamFromString(svgFile.Res.Content));
                xml.MoveToContent();
                xml.Read();
                while (!xml.EOF)
                {
                    if (xml.NodeType == XmlNodeType.Element && xml.HasAttributes)
                    {
                        string tagName = xml.Name;
                        XElement element = XNode.ReadFrom(xml) as XElement;
                        List<XAttribute> attributes = element.Attributes().ToList();
                        switch (tagName)
                        {
                            case "rect":
                                HandleRect(attributes);
                                break;
                            case "circle":
                                HandleCircle(attributes);
                                break;
                            case "polygon":
                                HandlePolygon(attributes);
                                break;
                        }
                    }
                    else
                        xml.Read();
                }
            }
            finally
            {
                canvas.End();
            }
        }

        // from https://stackoverflow.com/a/1879470
        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private void HandleRect(List<XAttribute> attributes)
        {
            Vector3 position = new Vector3();
            Vector2 size = new Vector2();

            foreach (XAttribute a in attributes)
            {
                switch (a.Name.LocalName)
                {
                    case "x":
                        position.X += GetValue(a.Value, (string s) => float.Parse(s));
                        break;
                    case "y":
                        position.Y += GetValue(a.Value, (string s) => float.Parse(s));
                        break;
                    case "z":
                        position.Z += GetValue(a.Value, (string s) => float.Parse(s));
                        break;
                    case "width":
                        size.X = GetValue(a.Value, (string s) => float.Parse(s));
                        break;
                    case "height":
                        size.Y = GetValue(a.Value, (string s) => float.Parse(s));
                        break;
                    case "style":
                        Dictionary<string, string> styles = StyleToDict(a.Value);
                        if (styles.ContainsKey("fill"))
                        {
                            ColorRgba fillColor = GetValue(styles["fill"], (string s) => StringToColorRgba(s));
                            if (styles.ContainsKey("fill-opacity"))
                                fillColor.A = (byte) (GetValue(styles["fill-opacity"], (string s) => float.Parse(s)) * 255f);
                            DrawRect(position, size, fillColor);
                        }
                        if (styles.ContainsKey("stroke"))
                        {
                            ColorRgba strokeColor = GetValue(styles["stroke"], (string s) => StringToColorRgba(s));
                            float strokeWidth = 2f;
                            if (styles.ContainsKey("stroke-width"))
                                strokeWidth = GetValue(styles["stroke-width"], (string s) => float.Parse(s));
                            if (styles.ContainsKey("stroke-opacity"))
                                strokeColor.A = (byte)(GetValue(styles["stroke-opacity"], (string s) => float.Parse(s)) * 255f);
                            CornerType cornerType = CornerType.Miter;
                            if (styles.ContainsKey("stroke-linejoin"))
                                cornerType = GetValue(styles["stroke-linejoin"], (string s) => (CornerType)Enum.Parse(typeof(CornerType), styles["stroke-linejoin"], true));
                            DrawRectStroke(position, size, strokeColor, strokeWidth, cornerType);
                        }
                        break;
                }
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

        enum CornerType
        {
            Miter, Round
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

        private void HandleCircle(List<XAttribute> attributes)
        {
            Vector3 position = new Vector3();
            float radius = 10f;

            foreach (XAttribute a in attributes)
            {
                switch (a.Name.LocalName)
                {
                    case "cx":
                        position.X += GetValue(a.Value, (string s) => float.Parse(s));
                        break;
                    case "cy":
                        position.Y += GetValue(a.Value, (string s) => float.Parse(s));
                        break;
                    case "cz":
                        position.Z += GetValue(a.Value, (string s) => float.Parse(s));
                        break;
                    case "r":
                        radius = GetValue(a.Value, (string s) => float.Parse(s));
                        break;
                    case "style":
                        Dictionary<string, string> styles = StyleToDict(a.Value);
                        if (styles.ContainsKey("fill"))
                        {
                            ColorRgba fillColor = GetValue(styles["fill"], (string s) => StringToColorRgba(s));
                            if (styles.ContainsKey("fill-opacity"))
                                fillColor.A = (byte)(GetValue(styles["fill-opacity"], (string s) => float.Parse(s)) * 255f);
                            DrawCircle(position, radius, fillColor);
                            
                        }
                        if (styles.ContainsKey("stroke"))
                        {
                            ColorRgba strokeColor = GetValue(styles["stroke"], (string s) => StringToColorRgba(s));
                            float strokeWidth = 2f;
                            if (styles.ContainsKey("stroke-width"))
                                strokeWidth = GetValue(styles["stroke-width"], (string s) => float.Parse(s));
                            if (styles.ContainsKey("stroke-opacity"))
                                strokeColor.A = (byte)(GetValue(styles["stroke-opacity"], (string s) => float.Parse(s)) * 255f);
                            DrawCircleStroke(position, radius, strokeColor, strokeWidth);
                        }
                        break;
                }
            }
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

        private void HandlePolygon(List<XAttribute> attributes)
        {
            Vector3 position = new Vector3();
            List<Vector2> points = new List<Vector2>();

            foreach (XAttribute a in attributes)
            {
                switch (a.Name.LocalName)
                {
                    case "points":
                        string[] splitPointString = a.Value.Split(' ');
                        foreach (string pointTuple in splitPointString)
                        {
                            string[] point = pointTuple.Split(',');
                            points.Add(new Vector2(float.Parse(point[0]), float.Parse(point[1])));
                        }
                        break;
                    case "style":
                        Dictionary<string, string> styles = StyleToDict(a.Value);
                        if (styles.ContainsKey("fill"))
                        {
                            ColorRgba fillColor = GetValue(styles["fill"], (string s) => StringToColorRgba(s));
                            if (styles.ContainsKey("fill-opacity"))
                                fillColor.A = (byte)(GetValue(styles["fill-opacity"], (string s) => float.Parse(s)) * 255f);
                            DrawPolygon(position, points.ToArray(), fillColor);
                        }
                        if (styles.ContainsKey("stroke"))
                        {
                            ColorRgba strokeColor = GetValue(styles["stroke"], (string s) => StringToColorRgba(s));
                            float strokeWidth = 2f;
                            if (styles.ContainsKey("stroke-width"))
                                strokeWidth = GetValue(styles["stroke-width"], (string s) => float.Parse(s));
                            if (styles.ContainsKey("stroke-opacity"))
                                strokeColor.A = (byte)(GetValue(styles["stroke-opacity"], (string s) => float.Parse(s)) * 255f);
                            CornerType cornerType = CornerType.Miter;
                            if (styles.ContainsKey("stroke-linejoin"))
                                cornerType = GetValue(styles["stroke-linejoin"], (string s) => (CornerType)Enum.Parse(typeof(CornerType), styles["stroke-linejoin"], true));
                            DrawPolygonStroke(position, points.ToArray(), strokeColor, strokeWidth, cornerType);
                        }
                        break;
                }
            }
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

        private Dictionary<string, string> StyleToDict(string styleString)
        {
            Dictionary<string, string> output = new Dictionary<string, string>();
            string[] styleSplit = styleString.Split(';').Where(s => s.Length > 0).ToArray();
            foreach (string style in styleSplit)
            {
                string[] keyValue = style.Split(':');
                output[keyValue[0]] = keyValue[1];
            }
            return output;
        }

        private T GetValue<T>(string input, Func<string, T> fallback)
        {
            if (input[0] == '{')
                return GetVariableFromComponentProperty<T>(input.Substring(1, input.Length - 2));
            else if (input[0] == '[')
                return GetVariableFromComponentMethod<T>(input.Substring(1, input.Length - 2));
            else
                return fallback(input);
        }

        private ColorRgba StringToColorRgba(string styleColor)
        {
            ColorRgba output = new ColorRgba();
            Regex numRegex = new Regex(@"(\d+)", RegexOptions.IgnoreCase);
            if (styleColor.StartsWith("#"))  // hexadecimal
            {
                output.R = byte.Parse(styleColor.Substring(1, 2));
                output.G = byte.Parse(styleColor.Substring(3, 2));
                output.B = byte.Parse(styleColor.Substring(5, 2));
                if (styleColor.Length == 9)  // hex color includes alpha channel
                    output.A = byte.Parse(styleColor.Substring(7, 2));
                else
                    output.A = 255;
            }
            else if (styleColor.StartsWith("rgb("))  // RGB function
            {
                MatchCollection matches = numRegex.Matches(styleColor);
                output.R = byte.Parse(matches[0].Value);
                output.G = byte.Parse(matches[1].Value);
                output.B = byte.Parse(matches[2].Value);
                output.A = 255;
            }
            else if (styleColor.StartsWith("rgba("))  // RGBA function
            {
                MatchCollection matches = numRegex.Matches(styleColor);
                output.R = byte.Parse(matches[0].Value);
                output.G = byte.Parse(matches[1].Value);
                output.B = byte.Parse(matches[2].Value);
                output.A = byte.Parse(matches[3].Value);
            }
            return output;
        }

        private T GetVariableFromComponentProperty<T>(string str)
        {
            T output = default;

            string[] split = str.Split(new char[] { '.' }, 2);
            Component component = GameObj.Components.FirstOrDefault((Component c) => c.GetType().Name == split[0]);
            if (component != null)
            {
                PropertyInfo propInfo = component.GetType().GetProperty(split[1]);
                if (propInfo == null)
                    Logs.Game.WriteError("Property {0} of component {1} does not exist or is not accessible (thrown by variable reference \"{2}\" in SVG file)",
                        split[1], split[0], str);
                else
                    output = (T) propInfo.GetValue(component);
            }

            return output;
        }

        private T GetVariableFromComponentMethod<T>(string str)
        {
            T output = default;

            string[] splitArgs = str.Split(' ');
            string[] splitMethodPath = splitArgs[0].Split(new char[] { '.' }, 2);
            Component component = GameObj.Components.FirstOrDefault((Component c) => c.GetType().Name == splitMethodPath[0]);
            if (component != null)
            {
                MethodInfo methodInfo = component.GetType().GetMethod(splitMethodPath[1]);
                if (methodInfo == null)
                    Logs.Game.WriteError("Method {0} of component {1} does not exist or is not accessible (thrown by variable reference \"{2}\" in SVG file)",
                        splitMethodPath[1], splitMethodPath[0], str);
                else
                    output = (T)methodInfo.Invoke(component, splitArgs.Skip(1).ToArray());
            }

            return output;
        }
    }
}
