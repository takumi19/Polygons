using MyLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PolygonDraft
{
    [Serializable]
    public class Polygon : IEnumerable<Vertex>
    {
        private static Random _rnd = new Random();
        private Color _hullColor;
        private Color _vertexColor;
        private int _radius;
        private List<Vertex> _vertices;

        public Polygon(in Color hullColor, in Color vertexColor, in int vertexSize)
        {
            _vertices = new List<Vertex>();
            HullColor = hullColor;
            VertexColor = vertexColor;
            VertexSize = vertexSize;
        }

        private int Cross(in Point a, in Point b, in Point c)
        {
            return (a.X - b.X) * (c.Y - b.Y) - (c.X - b.X) * (a.Y - b.Y);
        }

        private bool IsInsideTheHull(in Point point)
        {
            int n = _vertices.Count;
            int firstCross = Cross(_vertices[n - 1], new Point(point.X, point.Y), _vertices[0]);
            int neededSign = (firstCross == 0) ? 0 : (firstCross > 0) ? -1 : 1;
            for (int i = 0; i < n - 1; ++i)
            {
                int currentCross = Cross(_vertices[i], new Point(point.X, point.Y), _vertices[i + 1]);
                int currentSign = (currentCross == 0) ? 0 : (currentCross > 0) ? -1 : 1;
                if (currentSign != neededSign)
                    return false;
            }
            return true;
        }

        private List<Vertex> Convex()
        {
            int n = _vertices.Count, k = 0;
            List<Vertex> Hull = new List<Vertex>(new Vertex[2 * n]);

            _vertices = (from vertex in _vertices
                         orderby vertex.X, vertex.Y ascending
                         select vertex).ToList();

            // Нижняя оболочка:
            for (int i = 0; i < n; Hull[k++] = _vertices[i++])
                for (; k >= 2 && Cross(Hull[k - 2], Hull[k - 1], _vertices[i]) <= 0; k--) ;

            // Верхняя оболочка:
            for (int i = n - 2, t = k + 1; i >= 0; Hull[k++] = _vertices[i--])
                for (; k >= t && Cross(Hull[k - 2], Hull[k - 1], _vertices[i]) <= 0; k--) ;

            return Hull.Take(k - 1).ToList();
        }

        public Color VertexColor
        {
            get => _vertexColor;
            set
            {
                _vertexColor = value;
                _vertices.ForEach(vertex => vertex.FillColor = _vertexColor);
            }
        }

        public Color HullColor
        {
            get => _hullColor;
            set
            {
                _hullColor = value;
                _vertices.ForEach(vertex => vertex.HullColor = _hullColor);
                //HullColorChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int VertexSize
        {
            get => _radius;
            set
            {
                _radius = value;
                _vertices.ForEach(vertex => vertex.Radius = _radius);
            }
        }

        public int Count => _vertices.Count;

        /// <summary>
        /// Returns a list of vertices that will be deleted after the polygon is made convex.
        /// </summary>
        /// <returns></returns>
        public List<Vertex> VerticesToDelete()
        {
            return (Count >= 3) ? _vertices.Except(Convex()).ToList() : new List<Vertex>();
        }

        /// <summary>
        /// Returns a list of vertices that will be deleted if <paramref name="vertex"/> is added to the polygon.
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        public List<Vertex> VerticesToDelete(Vertex vertex)
        {
            _vertices.Add(vertex);
            var verticesToDelete = VerticesToDelete();
            _vertices.Remove(vertex);
            return verticesToDelete;
        }

        /// <summary>
        /// Returns true if <paramref name="point"/> is inside the polygon, otherwise false.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool Contains(in Point point)
        {
            return (_vertices.Count >= 3 && IsInsideTheHull(point));
        }

        /// <summary>
        /// Draws the polygon on <paramref name="graphics"/>.
        /// </summary>
        /// <param name="graphics"></param>
        public void Draw(Graphics graphics)
        {
            if (_vertices.Count >= 3)
            {
                Point[] points = new Point[_vertices.Count];
                for (int i = 0; i < _vertices.Count; ++i)
                    points[i] = _vertices[i];
                graphics.DrawPolygon(new Pen(HullColor, 3), points);
            }
            _vertices.ForEach(vertex => vertex.Draw(graphics));

        }

        /// <summary>
        /// Makes the polygon convex.
        /// </summary>
        public void MakeConvex()
        {
            _vertices = Convex();
        }

        /// <summary>
        /// Moves the vertices of the polygon randomly.
        /// </summary>
        public void Move()
        {
            foreach (Vertex vertex in _vertices)
            {
                vertex.Y += _rnd.Next(-1, 2);
                vertex.X += _rnd.Next(-1, 2);
            }
        }

        /// <summary>
        /// Removes <paramref name="vertex"/> from polygon.
        /// </summary>
        /// <param name="vertex"></param>
        public void Remove(in Vertex vertex)
        {
            _vertices.Remove(vertex);
        }

        /// <summary>
        /// Adds <paramref name="vertex"/> to the polygon.
        /// </summary>
        /// <param name="vertex"></param>
        public void Add(Vertex vertex)
        {
            vertex.HullColor = HullColor;
            vertex.FillColor = VertexColor;
            vertex.Radius = VertexSize;
            _vertices.Add(vertex);
        }

        /// <summary>
        /// Performs the specified action on each vertex of the polygon.
        /// </summary>
        /// <param name="action"></param>
        public void ForEach(Action<Vertex> action)
        {
            _vertices.ForEach(action);
        }

        /// <summary>
        /// Determines whether the polygon contains vertices that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public bool Exists(Predicate<Vertex> match)
        {
            return _vertices.Exists(match);
        }

        /// <summary>
        /// Adds the elements of the specified collection to the polgon.
        /// </summary>
        /// <param name="collection"></param>
        public void AddRange(IEnumerable<Vertex> collection)
        {
            foreach (Vertex vertex in collection)
                Add(vertex);
        }

        /// <summary>
        /// Returns the last vertex of the polygon on the <paramref name="point"/>.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vertex FindLast(Point point)
        {
            return _vertices.FindLast(vertex => vertex.Check(point.X, point.Y));
        }

        /// <summary>
        /// Removes a range of vertices from the polygon
        /// </summary>
        /// <param name="collection"></param>
        public void RemoveRange(List<Vertex> collection)
        {
            _vertices.RemoveAll(vertex => collection.Contains(vertex));
        }

        public IEnumerator<Vertex> GetEnumerator()
        {
            return ((IEnumerable<Vertex>)_vertices).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Vertex>)_vertices).GetEnumerator();
        }
    }
}
