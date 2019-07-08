using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cheesegreater.Duality.Plugin.SVG.Properties;
using Duality;
using Duality.Editor;

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
            set { encoding = value; }
        }

        public void SetData(string content)
        {
            this.content = content;
            length = content.Length;
        }
    }
}
