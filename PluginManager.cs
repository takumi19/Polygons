using MyLibrary;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;

namespace PolygonDraft
{
    public static class PluginManager
    {
        private static List<Type> _pluggedInTypes = new List<Type>();

        public static List<Type> ExportTypesFrom(string[] fileNames)
        {
            List<Type> newTypes = new List<Type>();
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
                Type[] array = Assembly.LoadFrom(path).GetExportedTypes();
                foreach (Type type in array)
                {
                    if (!_pluggedInTypes.Contains(type) && type.BaseType.Equals(typeof(Vertex)))
                    {
                        _pluggedInTypes.Add(type);
                        newTypes.Add(type);
                    }
                }
            }
            return newTypes;
        }
    }

    public class PMMemento : IMemento
    {
        private List<Type> _pluggedInTypes;

        public PMMemento(List<Type> pluggedInTypes)
        {
            _pluggedInTypes = pluggedInTypes;
        }

        public ExpandoObject GetState()
        {
            dynamic state = new ExpandoObject();
            state.PluggedInTypes = _pluggedInTypes;
            return state;
        }
    }
}
