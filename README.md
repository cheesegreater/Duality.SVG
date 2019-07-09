# How does this work?

A SVG resource breaks down an .svg file into several shapes, each of which has several attributes (e.g. fill color, position,
radius). These attributes are stored as Funcs that are evaluated by a SVGRenderer at runtime. A SVGRenderer component iterates
over the shapes of a SVG resource, calls their Invoke() methods to get the actual attributes, and draws them using a Canvas object.

The reason why all attributes are stored using Funcs is so that they are evaluated at runtime. This allows shapes to get their
attributes from other components' properties and methods. They can also get attributes from the SVG resource itself: each resource
has a Styles property, which is automatically populated with the fields requested by the .svg file. For example:

```
<svg>
    <text x="[TestObject.TestComponent.X]" y="[TestObject.TestComponent.GetY 50]" style="fill:[TestObject.TestComponent.ColorPicker];font=[MainFont];">Wow, text!</text>
</svg>
```

```cs
// This component is in a GameObject named "TestObject", in the same scene as the SVGRenderer
public class TestComponent : Component {
    public float X { get; set; } = 0.0f;

    // The plugin will automatically figure out which type should be used for an attribute
    public ColorRgba ColorPicker { get; set; } = ColorRgba.White;

    public float GetY(float offset)
    {
        return X + offset;
    }
}
```

When set up in a scene, the SVGRenderer will use the X and ColorPicker properties of TestComponent to determine the fill color and
position of the text object. The applications of this are nearly limitless, from animating sprites to creating responsive GUIs.

This .svg code will also create a MainFont attribute in the associated SVG resource, allowing users to define the necessary Font
resource in the resource itself instead of a seperate component. This feature should be used when defining constant values, such
as other resources.
