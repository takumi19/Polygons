using MyLibrary;
using System;
using System.Drawing;

namespace PolygonDraft
{
    [Serializable]
    public sealed class Triangle : Vertex
    {
        public Triangle(in int x,
                        in int y,
                        in Color fillColor,
                        in Color hullColor,
                        in int radius)
            : base(x, y, fillColor, hullColor, radius) { }
        public override bool Check(in int x, in int y)
        {
            Point[] point_array = { new Point(X - Radius, Y + (Radius / 2)),
                new Point(X, Y - Radius),
                new Point(X + Radius, Y + (Radius / 2)) };
            bool b1 = (point_array[0].X - x)
                      * (point_array[1].Y - point_array[0].Y)
                      - (point_array[1].X - point_array[0].X)
                      * (point_array[0].Y - y) >= 0;
            bool b2 = (point_array[1].X - x)
                      * (point_array[2].Y - point_array[1].Y)
                      - (point_array[2].X - point_array[1].X)
                      * (point_array[1].Y - y) >= 0;
            bool b3 = (point_array[2].X - x)
                      * (point_array[0].Y - point_array[2].Y)
                      - (point_array[0].X - point_array[2].X)
                      * (point_array[2].Y - y) >= 0;
            if (b1 && b2 && b3 || !b1 && !b2 && !b3)
                return true;
            return false;
        }
        public override void Draw(Graphics graphics)
        {
            Point[] point_array = { new Point(X - Radius, Y + (Radius / 2)), new Point(X, Y - Radius), new Point(X + Radius, Y + (Radius / 2)) };
            graphics.FillPolygon(new SolidBrush(FillColor), point_array);
            graphics.DrawPolygon(new Pen(HullColor, 3), point_array);
        }
    }
}
