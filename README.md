# How does this work?

A SVG resource breaks down an .svg file into several shapes, each of which has several attributes (e.g. fill color, position,
radius). These attributes are stored as Funcs that are evaluated by a SVGRenderer at runtime. A SVGRenderer component iterates
over the shapes of a SVG resource, calls their Invoke() methods to get the actual attributes, and draws them using a Canvas object.

The reason why all attributes are stored using Funcs is so that they are evaluated at runtime. This allows shapes to get their
attributes from sibling components' properties and methods. They can also get attributes from the SVG resource itself: each resource
has a Styles property, which is automatically populated with the fields requested by the .svg file. For example:

```
<svg>
    <text x="[TestComponent.X]" y="[TestComponent.GetY 50]" style="fill:[TestComponent.ColorPicker];font=[MainFont];">Wow, text!</text>
</svg>
```

```cs
// This component is in the same GameObject as the SVGRenderer
public class TestComponent : Component {
    public float X { get; set; } = 0.0f;

    public ColorRgba ColorPicker { get; set; } = ColorRgba.White;

    public float GetY(string offset)
    {
        return X + float.Parse(offset);
    }
}
```

When set up in a scene, the SVGRenderer will use the X and ColorPicker properties of TestComponent to determine the fill color and
position of the text object. The applications of this are nearly limitless, from animating sprites to creating responsive GUIs.

This .svg code will also create a MainFont attribute in the associated SVG resource, allowing users to define the necessary Font
resource in the resource itself instead of a seperate component. This feature should be used when defining constant values, such
as other resources.
