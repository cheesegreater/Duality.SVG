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
using Duality.Resources;

namespace Cheesegreater.Duality.Plugin.SVG
{
    [PropertyEditorAssignment(typeof(SVGStyle), PropertyEditorAssignmentAttribute.PrioritySpecialized)]
    public class SVGStylePropertyEditor : MemberwisePropertyEditor
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
                IEnumerable<SVGStyle> styleObjects = values.Cast<SVGStyle>();
                List<SVGDeclaredField> fields = styleObjects.NotNull().First().DeclaredFields;

                // delete unused/wrongly typed editors
                List<string> removeEditors = new List<string>();
                foreach (KeyValuePair<string, FieldEditorItem> pair in fieldEditors)
                {
                    bool isMatchingEditor = fields.Contains(pair.Value.Field) && pair.Value.Field.Name == pair.Key;
                    if (!isMatchingEditor)
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

                    PropertyEditor editor = CreateEditor(field);
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

        public PropertyEditor CreateEditor(SVGDeclaredField field)
        {
            PropertyEditor editor = null;

            //if (field.Type == typeof(ContentRef<Font>))
            //{
            //    editor = ParentGrid.CreateEditor(field.Type, this);
            //    editor.Getter = () => GetValue().Cast<SVGStyle>().Select(obj => obj?.DeclaredFields.FirstOrDefault(f => f.Name.Equals(field.Name)).Value);
            //    editor.Setter = CreateValueSetter<ContentRef<Font>>(field.Name);
            //}
            //else
            //{
            //    Logs.Editor.WriteError("Could not create editor for type " + field.Type.FullName);
            //}

            editor = ParentGrid.CreateEditor(field.Type, this);
            editor.Getter = () => GetValue().Cast<SVGStyle>().Select(obj => obj?.DeclaredFields.FirstOrDefault(f => f.Name.Equals(field.Name)).Value);
            editor.Setter = (Action<IEnumerable<object>>) typeof(SVGStylePropertyEditor).GetMethod("CreateValueSetter").MakeGenericMethod(field.Type).Invoke(this, new object[] { field.Name });

            editor.PropertyName = field.Name;
            ParentGrid.ConfigureEditor(editor);
            return editor;
        }

        public Action<IEnumerable<object>> CreateValueSetter<T>(string name) where T : struct
        {
            return delegate (IEnumerable<object> values)
            {
                IEnumerator<T> valuesEnum = values.Cast<T>().GetEnumerator();
                SVGStyle[] styleArray = GetValue().Cast<SVGStyle>().ToArray();

                T curValue = default;
                if (valuesEnum.MoveNext()) curValue = valuesEnum.Current;
                foreach (SVGStyle style in styleArray)
                {
                    SVGDeclaredField field = style?.DeclaredFields.FirstOrDefault(f => f.Name.Equals(name));
                    field.Value = curValue;
                    if (valuesEnum.MoveNext()) curValue = valuesEnum.Current;
                }
                OnPropertySet(null, styleArray);
            };
        }
    }
}
