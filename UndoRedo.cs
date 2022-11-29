using MyLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PolygonDraft
{
    public interface ICommand
    {
        void Execute();
        void Undo();
    }

    public static class UndoRedoManager
    {
        public static Stack<List<ICommand>> OnUndo { get; private set; } = new Stack<List<ICommand>>();
        public static Stack<List<ICommand>> OnRedo { get; private set; } = new Stack<List<ICommand>>();

        public static void Undo()
        {
            if (OnUndo.Count == 0)
                return;
            OnUndo.Peek()?.ForEach(cmd => cmd.Undo());
            List<ICommand> cmds = OnUndo.Pop();
            cmds.Reverse();
            OnRedo.Push(cmds);
        }

        public static void Redo()
        {
            if (OnRedo.Count == 0)
                return;
            OnRedo.Peek().ForEach(cmd => cmd.Execute());
            List<ICommand> cmds = OnRedo.Pop();
            cmds.Reverse();
            OnUndo.Push(cmds);
        }

        public static void AddCmd(params ICommand[] cmds)
        {
            OnUndo.Push(cmds.ToList());
            OnRedo.Clear();
        }

        public static void ClearLog()
        {
            OnUndo.Clear();
            OnRedo.Clear();
        }
    }

    public sealed class DragDropCmd : ICommand
    {
        private Polygon _polygon;
        private List<Vertex> _dragged = new List<Vertex>();
        private List<Point> _startingPoints = new List<Point>();
        private List<Point> _endPoints;

        public DragDropCmd(Polygon polygon)
        {
            _polygon = polygon;
        }

        public void Execute()
        {
            for (int i = 0; i < _dragged.Count; i++)
            {
                _dragged[i].X = _endPoints[i].X;
                _dragged[i].Y = _endPoints[i].Y;
            }
            if (_polygon.Count >= 3)
                _polygon.MakeConvex();
        }

        public void Undo()
        {
            for (int i = 0; i < _dragged.Count; i++)
            {
                _dragged[i].X = _startingPoints[i].X;
                _dragged[i].Y = _startingPoints[i].Y;
            }
            if (_polygon.Count >= 3)
                _polygon.MakeConvex();
        }

        public static bool CanDrag(Polygon polygon, int x, int y)
        {
            bool canDrag = false;
            bool polygonDragged = polygon.Contains(new Point(x, y));
            bool vertexDragged = polygon.Exists(vertex => vertex.Check(x, y));
            polygon.ForEach(vertex =>
            {
                if (vertex.Check(x, y) || (polygonDragged && !vertexDragged))
                    canDrag = true;
            });
            return canDrag;
        }

        public void DragStart(int x, int y)
        {
            bool polygonDragged = _polygon.Contains(new Point(x, y));
            bool vertexDragged = _polygon.Exists(vertex => vertex.Check(x, y));
            _polygon.ForEach(vertex =>
            {
                if (vertex.Check(x, y) || (polygonDragged && !vertexDragged))
                {
                    vertex.IsDragged = true;
                    vertex.Dx = x - vertex.X;
                    vertex.Dy = y - vertex.Y;
                    _dragged.Add(vertex);
                    _startingPoints.Add(new Point(vertex.X, vertex.Y));
                }
            });
        }

        public void DragDo(int mouseX, int mouseY)
        {
            _polygon.ForEach(vertex =>
            {
                if (vertex.IsDragged)
                {
                    vertex.X = mouseX - vertex.Dx;
                    vertex.Y = mouseY - vertex.Dy;
                }
            });
        }

        public void DragEnd()
        {
            _endPoints = (from vertex in _polygon
                          where vertex.IsDragged
                          select new Point(vertex.X, vertex.Y)).ToList();
            _polygon.ForEach(vertex => vertex.IsDragged = false);
        }
    }

    public sealed class AddVertexCmd : ICommand
    {
        private Vertex _vertex;
        private Polygon _polygon;

        public AddVertexCmd(Vertex vertex, Polygon polygon)
        {
            _vertex = vertex;
            _polygon = polygon;
        }

        public void Execute()
        {
            _polygon.Add(_vertex);
            if (_polygon.Count >= 3)
            {
                _polygon.MakeConvex();
            }
        }

        public void Undo()
        {
            _polygon.Remove(_vertex);
            if (_polygon.Count >= 3)
                _polygon.MakeConvex();
        }
    }

    public sealed class DeleteVertexCmd : ICommand
    {
        private Polygon _polygon;
        private List<Vertex> _vertices;

        public DeleteVertexCmd(Polygon polygon, Point point)
        {
            _polygon = polygon;
            _vertices = new List<Vertex>
            {
                polygon.FindLast(point)
            };
        }

        public DeleteVertexCmd(Polygon polygon, List<Vertex> vertices)
        {
            _polygon = polygon;
            _vertices = vertices;
        }

        public static bool CanDelete(Polygon polygon, Point point)
        {
            return polygon.Exists(vertex => vertex.Check(point.X, point.Y));
        }

        public void Execute()
        {
            _polygon.RemoveRange(_vertices);
        }

        public void Undo()
        {
            _polygon.AddRange(_vertices);
            if (_polygon.Count >= 3)
                _polygon.MakeConvex();
        }
    }

    public sealed class ChangeTypeCmd : ICommand
    {
        private Type _prevType;
        private Type _newType;

        public ChangeTypeCmd(Type prevType, Type newType)
        {
            _prevType = prevType;
            _newType = newType;
        }

        public static MainControl Form { get; set; }

        public void Execute()
        {
            Form.ChosenType = _newType;
        }

        public void Undo()
        {
            Form.ChosenType = _prevType;
        }
    }

    public sealed class ChangeSizeCmd : ICommand
    {
        private Polygon _polygon;
        private int _prevSize;
        private int _newSize;

        public ChangeSizeCmd(Polygon polygon, int prevSize, int newRadius)
        {
            _polygon = polygon;
            _prevSize = prevSize;
            _newSize = newRadius;
        }

        public void Execute()
        {
            _polygon.VertexSize = _newSize;
        }

        public void Undo()
        {
            _polygon.VertexSize = _prevSize;
        }
    }

    public sealed class ChangeBackColorCmd : ICommand
    {
        private Color _prevColor;
        private Color _newColor;

        public ChangeBackColorCmd(Color prevColor, Color newColor)
        {
            _prevColor = prevColor;
            _newColor = newColor;
        }

        public static Form Form { get; set; }

        public void Execute()
        {
            Form.BackColor = _newColor;
        }

        public void Undo()
        {
            Form.BackColor = _prevColor;
        }
    }

    public sealed class ChangeHullColorCmd : ICommand
    {
        private Polygon _polygon;
        private Color _prevColor;
        private Color _newColor;

        public ChangeHullColorCmd(Polygon polygon, Color newColor)
        {
            _polygon = polygon;
            _prevColor = polygon.HullColor;
            _newColor = newColor;
        }

        public void Execute()
        {
            _polygon.HullColor = _newColor;
        }

        public void Undo()
        {
            _polygon.HullColor = _prevColor;
        }
    }

    public sealed class ChangeVertexColorCmd : ICommand
    {
        private Polygon _polygon;
        private Color _prevColor;
        private Color _newColor;

        public ChangeVertexColorCmd(Polygon polygon, Color newColor)
        {
            _polygon = polygon;
            _prevColor = polygon.VertexColor;
            _newColor = newColor;
        }

        public void Execute()
        {
            _polygon.VertexColor = _newColor;
        }

        public void Undo()
        {
            _polygon.VertexColor = _prevColor;
        }
    }

    public sealed class ChangeShapeCmd : ICommand
    {
        private int _prevIndex;
        private int _newIndex;

        public ChangeShapeCmd(int prevIndex, int newIndex)
        {
            _prevIndex = prevIndex;
            _newIndex = newIndex;
        }

        public static ToolStripItemCollection Collection { get; set; }

        public void Execute()
        {
            foreach (ToolStripMenuItem item in Collection)
            {
                item.Checked = false;
            }
            ((ToolStripMenuItem)Collection[_newIndex]).Checked = true;
        }

        public void Undo()
        {
            foreach (ToolStripMenuItem item in Collection)
            {
                item.Checked = false;
            }
            ((ToolStripMenuItem)Collection[_prevIndex]).Checked = true;
        }
    }
}
