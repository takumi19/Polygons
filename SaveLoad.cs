using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PolygonDraft
{
    public interface IOriginator
    {
        IMemento CreateMemento();
    }

    public interface IMemento
    {
        ExpandoObject GetState();
    }

    public static class SaveOpenManager
    {
        private static BinaryFormatter _binFormatter = new BinaryFormatter();

        public static void Save(string path, FileMode mode, IMemento memento)
        {
            using (FileStream stream = new FileStream(path, mode))
            {
                _binFormatter.Serialize(stream, memento);
            }
        }

        public static IMemento Load(string path, FileMode mode)
        {
            IMemento memento;
            using (FileStream stream = new FileStream(path, mode))
            {
                memento = (IMemento)_binFormatter.Deserialize(stream);
            }
            return memento;
        }
    }

    [Serializable]
    public class MCMemento : IMemento
    {
        private readonly Polygon _polygon;
        private readonly Color _backColor;
        private readonly Type _type;
        private readonly int _index;
        private readonly List<Type> _pluggedInTypes;

        public MCMemento(Polygon polygon, Color backColor,
            Type type, int index, List<Type> pluggedInTypes)
        {
            this._polygon = polygon;
            this._backColor = backColor;
            this._type = type;
            this._index = index;
            this._pluggedInTypes = pluggedInTypes;
        }

        public ExpandoObject GetState()
        {
            dynamic state = new ExpandoObject();
            state.Polygon = _polygon;
            state.Type = _type;
            state.BackColor = _backColor;
            state.Index = _index;
            state.PluggedInTypes = _pluggedInTypes;
            return state;
        }

    }
}
