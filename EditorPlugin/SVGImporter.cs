using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Duality;
using Duality.Editor.AssetManagement;

namespace Cheesegreater.Duality.Plugin.SVG
{
    public class SVGImporter : IAssetImporter
    {
        public static readonly string SourceFileExtPrimary = ".svg";

        public string Id
        {
            get { return "SVGImporter"; }
        }

        public string Name
        {
            get { return "SVG Importer"; }
        }

        public int Priority
        {
            get { return 0; }
        }

        public void PrepareImport(IAssetImportEnvironment env)
        {
            foreach (AssetImportInput input in env.HandleAllInput(AcceptsInput))
            {
                env.AddOutput<Resources.SVG>(input.AssetName, input.Path);
            }
        }

        public void Import(IAssetImportEnvironment env)
        {
            foreach (AssetImportInput input in env.Input)
            {
                ContentRef<Resources.SVG> targetRef = env.GetOutput<Resources.SVG>(input.AssetName);
                if (targetRef.IsAvailable)
                {
                    Resources.SVG target = targetRef.Res;
                    if (!string.IsNullOrWhiteSpace(input.Path))
                    {
                        using (StreamReader reader = new StreamReader(input.Path))
                            target.SetData(reader.ReadToEnd());
                    }
                    env.AddOutput(targetRef, input.Path);
                }
            }
        }

        public void PrepareExport(IAssetExportEnvironment env)
        {
            if (env.Input is Resources.SVG)
                env.AddOutputPath(env.Input.Name + SourceFileExtPrimary);
        }

        public void Export(IAssetExportEnvironment env)
        {
            Resources.SVG input = env.Input as Resources.SVG;
            string outputPath = env.AddOutputPath(input.Name + SourceFileExtPrimary);

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                Encoding encoding = input.Encoding == null ? Encoding.UTF8 : input.Encoding;
                using (StreamWriter writer = new StreamWriter(outputPath, false, encoding))
                    writer.Write(input.Content);
            }
        }

        private bool AcceptsInput(AssetImportInput input)
        {
            string inputFileExtension = Path.GetExtension(input.Path);
            return SourceFileExtPrimary.Equals(inputFileExtension, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
