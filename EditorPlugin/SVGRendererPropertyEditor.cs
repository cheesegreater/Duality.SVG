using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AdamsLair.WinForms.PropertyEditing;
using Cheesegreater.Duality.Plugin.SVG.Components;
using Cheesegreater.Duality.Plugin.SVG.Resources;
using Duality;
using Duality.Editor;
using Duality.Editor.Plugins.Base.PropertyEditors;

namespace Cheesegreater.Duality.Plugin.SVG
{
    // [PropertyEditorAssignment(typeof(SVGRenderer), PropertyEditorAssignmentAttribute.PrioritySpecialized)]
    public class SVGRendererPropertyEditor : ComponentPropertyEditor
    {
        private struct FieldEditorItem
        {
            public PropertyEditor Editor;
            public SVGDeclaredField Field;
        }

        private Dictionary<string, FieldEditorItem> fieldEditors = new Dictionary<string, FieldEditorItem>();

        public override void ClearContent()
        {
            base.ClearContent();
            fieldEditors.Clear();
        }

        protected override bool IsAutoCreateMember(MemberInfo info)
        {
            return false;
        }

        protected override void OnUpdateFromObjects(object[] values)
        {
            base.OnUpdateFromObjects(values);

            if (values.Any(obj => obj != null))
            {
                IEnumerable<SVGRenderer> renderers = values.Cast<SVGRenderer>();
                List<SVGDeclaredField> fields = renderers.NotNull().First().DeclaredFields;

                // delete unused/wrongly typed editors
                List<string> removeEditors = new List<string>();
                foreach (KeyValuePair<string, FieldEditorItem> pair in fieldEditors)
                {
                    if (fields.Contains(pair.Value.Field) && pair.Value.Field.Name == pair.Key)
                        removeEditors.Add(pair.Key);
                }
                if (removeEditors.Count != 0)
                {
                    foreach (string fieldName in removeEditors)
                    {
                        RemovePropertyEditor(fieldEditors[fieldName].Editor);
                        fieldEditors.Remove(fieldName);
                    }
                }

                // create new editors
                int autoCreateEditorCount = 1;
                for (int i = 0; i < fields.Count; i++)
                {
                    SVGDeclaredField field = fields[i];
                    if (fieldEditors.ContainsKey(field.Name)) continue;

                    // TODO: work here
                    PropertyEditor editor = null;
                    fieldEditors[field.Name] = new FieldEditorItem
                    {
                        Editor = editor,
                        Field = field
                    };
                    if (autoCreateEditorCount + i <= ChildEditors.Count)
                        AddPropertyEditor(editor, autoCreateEditorCount + i);
                    else
                        AddPropertyEditor(editor);
                }
            }
        }
    }
}
