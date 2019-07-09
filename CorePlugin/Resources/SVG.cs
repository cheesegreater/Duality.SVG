using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Cheesegreater.Duality.Plugin.SVG.Properties;
using Duality;
using Duality.Drawing;
using Duality.Editor;
using Duality.Resources;

namespace Cheesegreater.Duality.Plugin.SVG.Resources
{
    [EditorHintCategory(ResNames.Category)]
    [EditorHintImage(ResNames.ImageSVG)]
    public class SVG : Resource
    {
        private string content;
        public string Content
        {
            get { return content; }
        }

        private int length;
        public int Length
        {
            get { return length; }
        }

        private Encoding encoding;
        [EditorHintFlags(MemberFlags.Invisible)]
        public Encoding Encoding
        {
            get { return encoding; }
        }

        [DontSerializeResource]
        private List<Shape> shapes;
        public List<Shape> Shapes
        {
            get { return shapes; }
        }

        [DontSerializeResource]
        private SVGStyle styles = new SVGStyle();
        public SVGStyle Styles
        {
            get { return styles; }
            set { styles = value; }
        }

        public void SetData(string content)
        {
            this.content = content;
            length = content.Length;
        }

        public void GenerateShapes()
        {
            if (content == null || length == 0) return;

            shapes = new List<Shape>();

            XmlReader xml = XmlReader.Create(GenerateStreamFromString(content));
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
                            shapes.Add(CreateRect(attributes));
                            break;
                        case "circle":
                            shapes.Add(CreateCircle(attributes));
                            break;
                        case "polygon":
                            shapes.Add(CreatePolygon(attributes));
                            break;
                        case "text":
                            attributes.Add(new XAttribute("_text", element.Value));
                            shapes.Add(CreateText(attributes));
                            break;
                    }
                }
                else
                    xml.Read();
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

        private Rect CreateRect(List<XAttribute> attributes)
        {
            Rect rect = new Rect();

            foreach (XAttribute a in attributes)
            {
                switch (a.Name.LocalName)
                {
                    case "x":
                        rect.X = GetValue(a.Value, () => float.Parse(a.Value));
                        break;
                    case "y":
                        rect.Y = GetValue(a.Value, () => float.Parse(a.Value));
                        break;
                    case "z":
                        rect.Z = GetValue(a.Value, () => float.Parse(a.Value));
                        break;
                    case "width":
                        rect.Width = GetValue(a.Value, () => float.Parse(a.Value));
                        break;
                    case "height":
                        rect.Height = GetValue(a.Value, () => float.Parse(a.Value));
                        break;
                    case "style":
                        Dictionary<string, string> styles = StyleToDict(a.Value);
                        if (styles.ContainsKey("fill"))
                        {
                            rect.FillColor = GetValue(styles["fill"], () => StringToColorRgba(styles["fill"]));
                            //if (styles.ContainsKey("fill-opacity"))
                            //    rect.FillColor.A = (byte)(GetValue(styles["fill-opacity"], (string s) => float.Parse(s)) * 255f);
                        }
                        if (styles.ContainsKey("stroke"))
                        {
                            rect.StrokeColor = GetValue(styles["stroke"], () => StringToColorRgba(styles["stroke"]));
                            if (styles.ContainsKey("stroke-width"))
                                rect.StrokeWidth = GetValue(styles["stroke-width"], () => float.Parse(styles["stroke-width"]));
                            //if (styles.ContainsKey("stroke-opacity"))
                            //    rect.StrokeColor.A = (byte)(GetValue(styles["stroke-opacity"], (string s) => float.Parse(s)) * 255f);
                            if (styles.ContainsKey("stroke-linejoin"))
                                rect.CornerType = GetValue(styles["stroke-linejoin"], () => (CornerType)Enum.Parse(typeof(CornerType), styles["stroke-linejoin"], true));
                        }
                        break;
                }
            }

            return rect;
        }

        private Circle CreateCircle(List<XAttribute> attributes)
        {
            Circle circle = new Circle();

            foreach (XAttribute a in attributes)
            {
                switch (a.Name.LocalName)
                {
                    case "cx":
                        circle.X = GetValue(a.Value, () => float.Parse(a.Value));
                        break;
                    case "cy":
                        circle.Y = GetValue(a.Value, () => float.Parse(a.Value));
                        break;
                    case "cz":
                        circle.Z = GetValue(a.Value, () => float.Parse(a.Value));
                        break;
                    case "r":
                        circle.Radius = GetValue(a.Value, () => float.Parse(a.Value));
                        break;
                    case "style":
                        Dictionary<string, string> styles = StyleToDict(a.Value);
                        if (styles.ContainsKey("fill"))
                        {
                            circle.FillColor = GetValue(styles["fill"], () => StringToColorRgba(styles["fill"]));
                            //if (styles.ContainsKey("fill-opacity"))
                            //    fillColor.A = (byte)(GetValue(styles["fill-opacity"], (string s) => float.Parse(s)) * 255f);
                        }
                        if (styles.ContainsKey("stroke"))
                        {
                            circle.StrokeColor = GetValue(styles["stroke"], () => StringToColorRgba(styles["stroke"]));
                            if (styles.ContainsKey("stroke-width"))
                                circle.StrokeWidth = GetValue(styles["stroke-width"], () => float.Parse(styles["stroke-width"]));
                            //if (styles.ContainsKey("stroke-opacity"))
                            //    strokeColor.A = (byte)(GetValue(styles["stroke-opacity"], (string s) => float.Parse(s)) * 255f);
                        }
                        break;
                }
            }

            return circle;
        }

        private Polygon CreatePolygon(List<XAttribute> attributes)
        {
            Polygon polygon = new Polygon();

            foreach (XAttribute a in attributes)
            {
                switch (a.Name.LocalName)
                {
                    case "points":
                        polygon.Points = () => {
                            return a.Value.Split(' ').ToList().Select(str =>
                            {
                                string[] point = str.Split(',');
                                return new Vector2(float.Parse(point[0]), float.Parse(point[1]));
                            }).ToList();
                        };
                        break;
                    case "style":
                        Dictionary<string, string> styles = StyleToDict(a.Value);
                        if (styles.ContainsKey("fill"))
                        {
                            polygon.FillColor = GetValue(styles["fill"], () => StringToColorRgba(styles["fill"]));
                            //if (styles.ContainsKey("fill-opacity"))
                            //    fillColor.A = (byte)(GetValue(styles["fill-opacity"], (string s) => float.Parse(s)) * 255f);
                        }
                        if (styles.ContainsKey("stroke"))
                        {
                            polygon.StrokeColor = GetValue(styles["stroke"], () => StringToColorRgba(styles["stroke"]));
                            if (styles.ContainsKey("stroke-width"))
                                polygon.StrokeWidth = GetValue(styles["stroke-width"], () => float.Parse(styles["stroke-width"]));
                            //if (styles.ContainsKey("stroke-opacity"))
                            //    strokeColor.A = (byte)(GetValue(styles["stroke-opacity"], (string s) => float.Parse(s)) * 255f);
                            if (styles.ContainsKey("stroke-linejoin"))
                                polygon.CornerType = GetValue(styles["stroke-linejoin"], () => (CornerType)Enum.Parse(typeof(CornerType), styles["stroke-linejoin"], true));
                        }
                        break;
                }
            }

            return polygon;
        }

        private Text CreateText(List<XAttribute> attributes)
        {
            Text text = new Text();

            foreach (XAttribute a in attributes)
            {
                switch (a.Name.LocalName)
                {
                    case "x":
                        text.X = GetValue(a.Value, () => float.Parse(a.Value));
                        break;
                    case "y":
                        text.Y = GetValue(a.Value, () => float.Parse(a.Value));
                        break;
                    case "z":
                        text.Z = GetValue(a.Value, () => float.Parse(a.Value));
                        break;
                    case "_text":
                        text.Content = GetValue(a.Value, () => a.Value);
                        break;
                    case "style":
                        Dictionary<string, string> styles = StyleToDict(a.Value);
                        if (styles.ContainsKey("fill"))
                            text.FillColor = GetValue(styles["fill"], () => StringToColorRgba(styles["fill"]));
                        if (styles.ContainsKey("font"))
                            text.FontStyle = GetValue(styles["font"], () => StringToFont(styles["fill"]));
                        break;
                }
            }

            return text;
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

        private Func<T> GetValue<T>(string input, Func<T> fallback)
        {
            if (input.StartsWith("{"))
                return GetVariableFromComponentProperty<T>(input.Substring(1, input.Length - 2));
            else if (input.StartsWith("["))
            {
                if (input.Substring(1, input.Length - 2).Split('.').Length == 1)
                    return GetVariableFromThisComponent<T>(input.Substring(1, input.Length - 2));
                else
                    return GetVariableFromComponentMethod<T>(input.Substring(1, input.Length - 2));
            }
            else
                return fallback;
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

        private ContentRef<Font> StringToFont(string str)
        {
            // TODO: implement this properly
            return Font.GenericMonospace8;
        }

        private Func<T> GetVariableFromComponentProperty<T>(string str)
        {
            string[] split = str.Split(new char[] { '.' }, 3);
            Component component = Scene.Current.FindGameObject(split[0]).Components.FirstOrDefault((Component c) => c.GetType().Name == split[1]);
            if (component != null)
            {
                PropertyInfo propInfo = component.GetType().GetProperty(split[1]);
                if (propInfo == null)
                {
                    Logs.Game.WriteError("Property {0} of component {1} does not exist or is not accessible (thrown by variable reference \"{2}\" in SVG file)",
                        split[1], split[0], str);
                    return null;
                }
                else
                    return () => (T)propInfo.GetValue(component);
            }
            else
            {
                Logs.Game.WriteError("Component {0} does not exist (thrown by variable reference \"{1}\" in SVG file", split[0], str);
                return null;
            }
        }

        private Func<T> GetVariableFromComponentMethod<T>(string str)
        {
            string[] splitArgs = str.Split(' ');
            string[] splitMethodPath = splitArgs[0].Split(new char[] { '.' }, 3);
            Component component = Scene.Current.FindGameObject(splitMethodPath[0]).Components.FirstOrDefault((Component c) => c.GetType().Name == splitMethodPath[1]);
            if (component != null)
            {
                MethodInfo methodInfo = component.GetType().GetMethod(splitMethodPath[1]);
                if (methodInfo == null)
                {
                    Logs.Game.WriteError("Method {0} of component {1} does not exist or is not accessible (thrown by variable reference \"{2}\" in SVG file)",
                        splitMethodPath[1], splitMethodPath[0], str);
                    return null;
                }
                else
                    return () => (T)methodInfo.Invoke(component, splitArgs.Skip(1).ToArray());
            }
            else
            {
                Logs.Game.WriteError("Component {0} does not exist (thrown by variable reference \"{1}\" in SVG file", splitMethodPath[0], str);
                return null;
            }
        }

        private Func<T> GetVariableFromThisComponent<T>(string str)
        {
            if (!styles.DeclaredFields.Any(f => f.Name == str))
                styles.DeclaredFields.Add(new SVGDeclaredField()
                {
                    Name = str,
                    Type = typeof(T),
                    Value = default(T)
                });
            return () => (T)styles.DeclaredFields.First(f => f.Name == str).Value;
        }
    }
}
