using MyLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PolygonDraft
{
    public static class PluginProvider
    {
        public static List<Type> PluggedInTypes { get; private set; } 
            = new List<Type> { typeof(Triangle) };

        public static void ExportTypesFrom(string[] fileNames)
        {
            List<string> paths = new List<string>();
            foreach (string path in fileNames)
            {
                if (Directory.Exists(path))
                    paths.AddRange(Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories));
                else if (Path.GetExtension(path) == ".dll")
                    paths.Add(path);
            }
            foreach (string path in paths)
            {
                foreach (Type type in Assembly.LoadFrom(path).GetExportedTypes())
                {
                    if (!PluggedInTypes.Contains(type) && type.BaseType.Equals(typeof(Vertex)))
                        PluggedInTypes.Add(type);
                }
            }
        }
    }
}
