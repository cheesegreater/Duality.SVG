using System;
using System.Collections.Generic;
using System.Linq;

using Duality;
using Duality.Drawing;
using Duality.Editor;

namespace Cheesegreater.Duality.Plugin.SVG.Components
{
    public class TestComponent : Component
    {
        [DontSerialize]
        private float health = 100;
        [EditorHintRange(0f, 100f)]
        public float Health
        {
            get { return health; }
            set { health = value; }
        }

        [DontSerialize]
        private ColorRgba picker = ColorRgba.Green;
        public ColorRgba Picker
        {
            get { return picker; }
            set { picker = value; }
        }

        public float GetHealthBarFillWidth(string maxWidth)
        {
            return float.Parse(maxWidth) * (health / 100f);
        }
    }
}
