using MyLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace PolygonDraft
{
    //C:\Users\Максим\source\repos\MyLibrary\MyLibrary\bin\Debug\MyLibrary.dll
    public partial class MainControl : Form, IOriginator
    {
        private Polygon _polygon = new Polygon(hullColor: Color.Black,
                                               vertexColor: Color.FromArgb(64, 0, 128),
                                               vertexSize: 25);
        private Point _mouseDown;
        private Form _sizeChanger;
        private int _prevVertexSize;
        private ChangeSizeCmd _sizeCmd = new ChangeSizeCmd(null, default, default);
        private DragDropCmd _dragDropCmd;
        private DeleteVertexCmd _deleteCmd;
        private List<Type> _pluggedInTypes = new List<Type>();

        public MainControl()
        {
            InitializeComponent();
            //System.Runtime.ProfileOptimization.SetProfileRoot("");
            ChangeTypeCmd.Form = this;
            ChangeShapeCmd.Collection = ShapeToolStripMenuItem.DropDownItems;
            ChangeBackColorCmd.Form = this;
            CreateToolStripMenuItem(typeof(Triangle));
            (ShapeToolStripMenuItem.DropDownItems[nameof(Triangle)]
                as ToolStripMenuItem).Checked = true;

            TrackBar ResizeBar = new TrackBar
            {
                Minimum = 5,
                Maximum = 150,
                Value = _polygon.VertexSize,
                Orientation = Orientation.Horizontal,
                TickStyle = TickStyle.None,
                Location = new Point(0, 0),
                Width = 420,
                Height = 125,
                Name = "ResizeBar"
            };
            ResizeBar.Scroll += (_, __) =>
            {
                _sizeCmd = new ChangeSizeCmd(_polygon, _prevVertexSize, ResizeBar.Value);
                _sizeCmd.Execute();
                Invalidate();
            };
            ResizeBar.MouseDown += (_, __) => _prevVertexSize = _polygon.VertexSize;
            ResizeBar.MouseUp += (_, __) =>
            {
                if (_prevVertexSize != ResizeBar.Value)
                    UndoRedoManager.AddCmd(_sizeCmd);
            };

            _sizeChanger = new Form
            {
                ClientSize = new Size(ResizeBar.Width, ResizeBar.Height),
                FormBorderStyle = FormBorderStyle.FixedSingle,
                MinimizeBox = false,
                MaximizeBox = false,
                Text = "RadiusChanger"
            };

            _sizeChanger.Controls.Add(ResizeBar);
            _sizeChanger.FormClosing += (sender, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    _sizeChanger.Hide();
                }
            };
            _sizeChanger.ActiveControl = _sizeChanger.Controls["ResizeBar"];
        }

        public Type ChosenType { get; set; } = typeof(Triangle);

        private void MainControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z)
            {
                UndoRedoManager.Undo();
                Invalidate();
            }
            else if (e.Control && e.KeyCode == Keys.Y)
            {
                UndoRedoManager.Redo();
                Invalidate();
            }
        }

        private void MainControl_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void MainControl_DragDrop(object sender, DragEventArgs e)
        {
            List<string> paths = new List<string>();
            foreach (string path in (string[])e.Data.GetData(DataFormats.FileDrop))
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
                    if (!_pluggedInTypes.Contains(type) && type.BaseType.Equals(typeof(Vertex)))
                    {
                        _pluggedInTypes.Add(type);
                        CreateToolStripMenuItem(type);
                    }
                }
            }
        }

        #region Mouse events
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            _mouseDown = e.Location;
            if (e.Button == MouseButtons.Left && DragDropCmd.CanDrag(_polygon, e.X, e.Y)) // Drag and drop
            {
                _dragDropCmd = new DragDropCmd(_polygon);
                _dragDropCmd.DragStart(e.X, e.Y);
                return;
            }
            else if (e.Button == MouseButtons.Right) // Удаление
            {
                if (DeleteVertexCmd.CanDelete(_polygon, e.Location))
                {
                    _deleteCmd = new DeleteVertexCmd(_polygon, e.Location);
                    if (_dragDropCmd == null)
                    {
                        _deleteCmd.Execute();
                        UndoRedoManager.AddCmd(_deleteCmd);
                        _deleteCmd = null;
                    }
                    Invalidate();
                }
                return;
            }
            Vertex vertex = (Vertex)Activator.CreateInstance(ChosenType, e.X, e.Y, _polygon.VertexColor,
                _polygon.HullColor, _polygon.VertexSize);
            AddVertexCmd addVertexCmd = new AddVertexCmd(vertex, _polygon);
            List<Vertex> verticesToDelete = _polygon.VerticesToDelete(vertex);
            if (verticesToDelete.Count != 0)
            {
                DeleteVertexCmd deleteCmd = new DeleteVertexCmd(_polygon, verticesToDelete);
                deleteCmd.Execute();
                addVertexCmd.Execute();
                UndoRedoManager.AddCmd(addVertexCmd, deleteCmd);
            }
            else
            {
                addVertexCmd.Execute();
                UndoRedoManager.AddCmd(addVertexCmd);
            }
            Invalidate();
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            _dragDropCmd?.DragDo(e.X, e.Y);
            Invalidate();
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Location == _mouseDown)
                _dragDropCmd = null;
            _dragDropCmd?.DragEnd();
            _deleteCmd?.Execute();
            List<Vertex> toDelete = _polygon.VerticesToDelete();
            if (toDelete.Count != 0) // If dragDrop has deleted vertices
            {
                _deleteCmd = new DeleteVertexCmd(_polygon, toDelete);
                UndoRedoManager.AddCmd(_dragDropCmd, _deleteCmd);
                _dragDropCmd = null;
            }
            if (_dragDropCmd != null && _deleteCmd != null) // If a vertex was deleted on drag n drop
                UndoRedoManager.AddCmd(_dragDropCmd, _deleteCmd);
            else if (_dragDropCmd != null) // Simple drag n drop
                UndoRedoManager.AddCmd(_dragDropCmd);
            _deleteCmd = null;
            _dragDropCmd = null;
            if (_polygon.Count >= 3)
                _polygon.MakeConvex();
            Invalidate();
        }
        #endregion

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality; //Уменьшаем пикселизацию
            _polygon.Draw(e.Graphics);
        }

        #region ToolStripMenu
        private int IndexOfCheckedItem()
        {
            int index = 0;
            for (int i = 0; i < ShapeToolStripMenuItem.DropDownItems.Count; i++)
            {
                if (((ToolStripMenuItem)ShapeToolStripMenuItem.DropDownItems[i]).Checked)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        private void CreateToolStripMenuItem(Type type)
        {
            ToolStripMenuItem newMenuItem = new ToolStripMenuItem(type.Name);
            newMenuItem.Name = type.Name;

            newMenuItem.Click += (_, __) =>
            {
                int index = ShapeToolStripMenuItem.DropDownItems.IndexOf(newMenuItem);
                ChangeShapeCmd shapeCmd = new ChangeShapeCmd(IndexOfCheckedItem(), index);
                shapeCmd.Execute();
                ChangeTypeCmd typeCmd = new ChangeTypeCmd(ChosenType, type);
                typeCmd.Execute();
                UndoRedoManager.AddCmd(typeCmd, shapeCmd);
            };

            ShapeToolStripMenuItem.DropDownItems.Add(newMenuItem);
        }

        private void ГраницаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ColorDialog hullDialog = new ColorDialog() { Color = _polygon.HullColor })
            {
                if (hullDialog.ShowDialog() == DialogResult.OK)
                {
                    ChangeHullColorCmd cmd = new ChangeHullColorCmd(_polygon,
                                                                    hullDialog.Color);
                    cmd.Execute();
                    UndoRedoManager.AddCmd(cmd);
                    Invalidate();
                }
            }
        }

        private void ЗаливкаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ColorDialog vertexColorDialog = new ColorDialog() { Color = _polygon.VertexColor })
            {
                if (vertexColorDialog.ShowDialog() == DialogResult.OK)
                {
                    ChangeVertexColorCmd cmd = new ChangeVertexColorCmd(
                        _polygon,
                        vertexColorDialog.Color);
                    cmd.Execute();
                    UndoRedoManager.AddCmd(cmd);
                    Invalidate();
                }
            }
        }

        private void ФонToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ColorDialog backColorDialog = new ColorDialog() { Color = BackColor })
            {
                if (backColorDialog.ShowDialog() == DialogResult.OK)
                {
                    ChangeBackColorCmd cmd = new ChangeBackColorCmd(BackColor,
                                                                    backColorDialog.Color);
                    cmd.Execute();
                    UndoRedoManager.AddCmd(cmd);
                    Invalidate();
                }
            }
        }

        private void РазмерToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _sizeChanger.Visible = true;
            _sizeChanger.Activate();
        }
        #endregion

        #region Timer
        private void Start_Click(object sender, EventArgs e)
        {
            randTimer.Start();
            Invalidate();
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            randTimer.Stop();
            Invalidate();
        }

        private void RandTimer_Tick(object sender, EventArgs e)
        {
            _polygon.Move();
            Invalidate();
        }
        #endregion

        #region IOriginator
        public void SetMemento(MCMemento memento)
        {
            dynamic state = memento.GetState();
            _polygon = state.Polygon;
            BackColor = state.BackColor;
            _pluggedInTypes = state.PluggedInTypes;
            ChosenType = state.Type;
            ShapeToolStripMenuItem.DropDownItems.Clear();
            CreateToolStripMenuItem(typeof(Triangle));
            foreach (Type type in _pluggedInTypes)
                CreateToolStripMenuItem(type);
            ((ToolStripMenuItem)ShapeToolStripMenuItem.DropDownItems[state.Index]).Checked = true;
            ChangeTypeCmd.Form = this;
            ChangeShapeCmd.Collection = ShapeToolStripMenuItem.DropDownItems;
            ChangeBackColorCmd.Form = this;
        }

        public IMemento CreateMemento()
        {
            return new MCMemento(_polygon,
                                 BackColor,
                                 ChosenType,
                                 IndexOfCheckedItem(),
                                 _pluggedInTypes);
        }
        #endregion

        #region Save load
        private void SaveAsToolStripMenuItem_click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog() { DefaultExt = ".bin" })
            {
                string path;
                if (saveDialog.ShowDialog() == DialogResult.OK)
                    path = saveDialog.FileName;
                else
                    return;
                SaveOpenManager.Save(path, FileMode.Create, CreateMemento());
            }
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openDialog = new OpenFileDialog())
            {
                string path;
                if (openDialog.ShowDialog() == DialogResult.OK)
                    path = openDialog.FileName;
                else
                    return;
                UndoRedoManager.ClearLog();
                MCMemento McMemento =
                    (MCMemento)SaveOpenManager.Load(path, FileMode.Open);
                SetMemento(McMemento);
            }
            Invalidate();
        }
        #endregion

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }
    }
    #region Space debris
    #region Obsolete save & open
    //List<object> loaded = SOmanager.Load(path, FileMode.Open);
    //_radiusChanger = new RadiusChanger();
    //_polygon = (Polygon)loaded[0];
    //Vertex.RadiusChanged += Vertex_RadiusChanged;
    //_polygon.ColorChanged += Polygon_ColorChanged;
    //this.BackColor = (Color)loaded[1];
    //_type = (Type)loaded[2];
    //_radiusChanger.Value = (int)loaded[3];
    //Vertex.Radius = (int)loaded[4];
    //Vertex.VertexColor = (Color)loaded[5];
    //Vertex.HullColor = (Color)loaded[6];

    //List<object> loaded = SaveOpenManager.Load(path, FileMode.Open);
    //_radiusChanger = new RadiusChanger();
    //_polygon = (Polygon)loaded[0];
    //Vertex.RadiusChanged += Vertex_RadiusChanged;
    //_polygon.ColorChanged += Polygon_ColorChanged;
    //this.BackColor = (Color)loaded[1];
    //_type = (Type)loaded[2];
    //_radiusChanger.Value = (int)loaded[3];
    //Vertex.Radius = (int)loaded[4];
    //Vertex.VertexColor = (Color)loaded[5];
    //Vertex.HullColor = _polygon.HullColor;

    //switch (_type)
    //{
    //    case VertexType.CIRCLE:
    //        Vertices.Add(new Circle(e.X, e.Y));
    //        break;
    //    case VertexType.SQUARE:
    //        Vertices.Add(new Square(e.X, e.Y));
    //        break;
    //    default:
    //        Vertices.Add(new Triangle(e.X, e.Y));
    //        break;
    //}

    //using (FileStream stream = new FileStream(path, FileMode.Open))
    //{
    //    _radiusChanger = new RadiusChanger();
    //    _polygon = (Polygon)_binaryFormatter.Deserialize(stream);
    //    Vertex.RadiusChanged += Vertex_RadiusChanged;
    //    _polygon.ColorChanged += Polygon_ColorChanged;
    //    this.BackColor = (Color)_binaryFormatter.Deserialize(stream);
    //    _type = (Type)_binaryFormatter.Deserialize(stream);
    //    _radiusChanger.Value = (int)_binaryFormatter.Deserialize(stream);
    //    Vertex.Radius = (int)_binaryFormatter.Deserialize(stream);
    //    Vertex.VertexColor = (Color)_binaryFormatter.Deserialize(stream);
    //    Vertex.HullColor = _polygon.Color;
    //}

    //using (FileStream stream = new FileStream(path, FileMode.Create))
    //{
    //    _binaryFormatter.Serialize(stream, _polygon);
    //    _binaryFormatter.Serialize(stream, this.BackColor);
    //    _binaryFormatter.Serialize(stream, _type);
    //    _binaryFormatter.Serialize(stream, _radiusChanger.Value);
    //    _binaryFormatter.Serialize(stream, Vertex.Radius);
    //    _binaryFormatter.Serialize(stream, Vertex.VertexColor);
    //    _binaryFormatter.Serialize(stream, Vertex.HullColor);
    //}

    //Vertices.Sort((a, b) => a.X == b.X ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

    //private void формаToolStripMenuItem_Click(object sender, EventArgs e)
    //{

    //}

    //private void формаToolStripMenuItem_MouseLeave(object sender, EventArgs e)
    //{
    //    формаToolStripMenuItem.HideDropDown();
    //}

    //internal sealed class MyBinder : SerializationBinder
    //{
    //    public override Type BindToType(string assemblyName, string typeName)
    //    {
    //        Type ttd = null;
    //        try
    //        {
    //            string toassname = assemblyName.Split(',')[0];
    //            Assembly[] asmblies = AppDomain.CurrentDomain.GetAssemblies();
    //            foreach (Assembly ass in asmblies)
    //            {
    //                if (ass.FullName.Split(',')[0] == toassname)
    //                {
    //                    ttd = ass.GetType(typeName);
    //                    break;
    //                }
    //            }
    //        }
    //        catch (Exception e)
    //        {
    //            Debug.WriteLine(e.Message);
    //        }
    //        return ttd;
    //    }
    //}

    //public static class CmdExtension
    //{
    //    public static void RenewPolygonReference(this ICommand cmd, Polygon polygon)
    //    {
    //        Type type = cmd.GetType();
    //        FieldInfo[] info = type.GetFields();
    //        foreach (FieldInfo field in info)
    //        {
    //            if (field.DeclaringType is Polygon)
    //                field.SetValue(cmd, polygon);
    //        }
    //    }
    //}
    #endregion

    #region MouseDown
    //bool polygonDragged = _polygon.Contains(e.Location);
    //bool vertexDragged = _polygon.Exists(vertex => vertex.Check(e.X, e.Y));
    //_polygon.ForEach(vertex =>
    //{
    //    if (vertex.Check(e.X, e.Y) || (polygonDragged && !vertexDragged))
    //    {
    //        vertex.IsDragged = true;
    //        vertex.Dx = e.X - vertex.X;
    //        vertex.Dy = e.Y - vertex.Y;
    //        createNew = false;
    //    }
    //});
    #endregion

    #region MouseMove
    //_polygon.ForEach(vertex =>
    //{
    //    if (vertex.IsDragged == true)
    //    {
    //        vertex.X = e.X - vertex.Dx;
    //        vertex.Y = e.Y - vertex.Dy;
    //        Invalidate();
    //    }
    //});
    #endregion

    #region MouseUp
    //_polygon.ForEach(vertex => vertex.IsDragged = false);
    #endregion

    #region Undo/redo junk
    //[Serializable]
    //public sealed class CreateToolStripItemCmd : ICommand
    //{
    //    private ToolStripMenuItem _newItem;
    //    private Type _type;

    //    public CreateToolStripItemCmd(Type type)
    //    {
    //        _newItem = new ToolStripMenuItem(type.Name);
    //        _newItem.Name = type.Name;

    //        _newItem.Click += (_, __) =>
    //        {
    //            foreach (ToolStripMenuItem item in Collection)
    //                item.Checked = false;
    //            int index = Collection.IndexOf(_newItem);
    //            ToolStripMenuItem tmp = Collection[index]
    //                                    as ToolStripMenuItem;
    //            tmp.Checked = true;
    //        };
    //    }

    //    public static ToolStripItemCollection Collection { get; set; }

    //    public void Execute()
    //    {
    //        Collection.Add(_newItem);
    //    }

    //    public void Undo()
    //    {
    //        Collection.Remove(_newItem);
    //    }
    //}

    //[Serializable]
    //public sealed class ChangeShapeCmd : ICommand
    //{
    //    private string _prevItem;
    //    private string _newItem;

    //    public ChangeShapeCmd(string prevItem, string newItem)
    //    {
    //        _prevItem = prevItem;
    //        _newItem = newItem;
    //    }

    //    public static MainControl Form { get; set; }

    //    public void Execute()
    //    {
    //        foreach (ToolStripMenuItem item in Form.ShapeDropdown)
    //            item.Checked = false;
    //        (Form.ShapeDropdown[_newItem] as ToolStripMenuItem).Checked = true;
    //    }

    //    public void Undo()
    //    {
    //        foreach (ToolStripMenuItem item in Form.ShapeDropdown)
    //            item.Checked = false;
    //        (Form.ShapeDropdown[_prevItem] as ToolStripMenuItem).Checked = true;
    //    }
    //}

    //[Serializable]
    //public sealed class ChangeBackColorCmd : ICommand
    //{
    //    private Action<Color> _colorChanger;
    //    private Color _prevColor;
    //    private Color _newColor;

    //    public ChangeBackColorCmd(Action<Color> colorChanger, Color prevColor, Color newColor)
    //    {
    //        _colorChanger = colorChanger;
    //        _prevColor = prevColor;
    //        _newColor = newColor;
    //    }

    //    public void Execute()
    //    {
    //        _colorChanger?.Invoke(_newColor);
    //    }

    //    public void Undo()
    //    {
    //        _colorChanger?.Invoke(_prevColor);
    //    }
    //}

    //public sealed class ChangePropertyCmd : ICommand
    //{
    //    private Action<dynamic> _propChanger;
    //    private dynamic _prevValue;
    //    private dynamic _newValue;

    //    public ChangePropertyCmd(Action<dynamic> propChanger, dynamic prevValue, dynamic newValue)
    //    {
    //        _propChanger = propChanger;
    //        _prevValue = prevValue;
    //        _newValue = newValue;
    //    }

    //    public void Execute()
    //    {
    //        _propChanger?.Invoke(_newValue);
    //    }

    //    public void Undo()
    //    {
    //        _propChanger?.Invoke(_prevValue);
    //    }
    //}
    #endregion

    #region Vertex
    //[Serializable]
    //public abstract class Vertex
    //{
    //    public Vertex(in int x,
    //                  in int y,
    //                  in Color fillColor,
    //                  in Color hullColor,
    //                  in int radius)
    //    {
    //        Dx = 0;
    //        Dy = 0;
    //        IsDragged = false;
    //        X = x;
    //        Y = y;
    //        FillColor = fillColor;
    //        HullColor = hullColor;
    //        Radius = radius;
    //    }
    //    public bool IsDragged { get; set; }
    //    public Color FillColor { get; set; }
    //    public Color HullColor { get; set; }
    //    public int X { get; set; }
    //    public int Y { get; set; }
    //    public int Dx { get; set; }
    //    public int Dy { get; set; }
    //    public int Radius { get; set; }

    //    public abstract bool Check(in int x, in int y);

    //    public abstract void Draw(Graphics graphics);

    //    public static implicit operator Point(Vertex vertex)
    //        => new Point(vertex.X, vertex.Y);

    //    public override bool Equals(object obj)
    //    {
    //        Vertex vertex = obj as Vertex;
    //        bool colorMatch = (FillColor == vertex.FillColor && HullColor == vertex.HullColor);
    //        bool radiusMatch = (Radius == vertex.Radius);
    //        bool posMatch = (X == vertex.X && Y == vertex.Y);
    //        return colorMatch && radiusMatch && posMatch;
    //    }
    //}
    #endregion

    #region Polygon junk
    /// <summary>
    /// Deletes the vertex with the coordinates <paramref name="x"/> and <paramref name="y"/>. 
    /// If there is no such vertex, nothing will happen.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    //public void Remove(in Point point)
    //{
    //    Point p = point;
    //    int index = this._vertices.FindLastIndex(i => i.Check(p.X, p.Y));
    //    if (index != -1)
    //        this._vertices.RemoveAt(index);
    //    else
    //        return;
    //}
    #endregion

    #region Plugin junk
    //_pluggedInTypes.AddRange(Assembly.LoadFrom(path).GetExportedTypes()
    //                        .Where(type => type.BaseType.Equals(typeof(Vertex)))
    //                        .Distinct());

    //_pluggedInTypes.ForEach(type => CreateToolStripMenuItem(type));
    //PluginProvider.ExportTypesFrom((string[])e.Data.GetData(DataFormats.FileDrop));
    //foreach (Type type in PluginProvider.PluggedInTypes)
    //{
    //    CreateToolStripMenuItem(type);
    //}
    #endregion

    #region Other junk
    //_type = typeof(Triangle);
    //foreach (ToolStripMenuItem tool in формаToolStripMenuItem.DropDownItems)
    //    tool.Checked = false;
    //triangleToolStripMenuItem.Checked = true;

    //public ToolStripItemCollection ShapeDropdown =>
    //    shapeToolStripMenuItem.DropDownItems;

    //private void КругToolStripMenuItem_Click(object sender, EventArgs e)
    //{
    //    //_type = typeof(Circle);
    //    //foreach (ToolStripMenuItem tool in формаToolStripMenuItem.DropDownItems)
    //    //    tool.Checked = false;
    //    //circleToolStripMenuItem.Checked = true;
    //    if (ChosenType == typeof(Circle))
    //        return;
    //    ChangeTypeCmd typeCmd = new ChangeTypeCmd(ChosenType, typeof(Circle));
    //    typeCmd.Execute();
    //    ChangeShapeCmd shapeCmd = new ChangeShapeCmd(IndexOfCheckedItem(),
    //        shapeToolStripMenuItem.DropDownItems.IndexOf(CircleToolStripMenuItem));
    //    shapeCmd.Execute();
    //    UndoRedoManager.AddCmd(typeCmd, shapeCmd);
    //}

    //private void КвадратToolStripMenuItem_Click(object sender, EventArgs e)
    //{
    //    //_type = typeof(Square);
    //    //foreach (ToolStripMenuItem tool in формаToolStripMenuItem.DropDownItems)
    //    //    tool.Checked = false;
    //    //squareToolStripMenuItem.Checked = true;
    //    if (ChosenType == typeof(Square))
    //        return;
    //    ChangeTypeCmd typeCmd = new ChangeTypeCmd(ChosenType, typeof(Square));
    //    typeCmd.Execute();
    //    int prevIndex = IndexOfCheckedItem();
    //    int newIndex = shapeToolStripMenuItem.DropDownItems.IndexOf(SquareToolStripMenuItem);
    //    ChangeShapeCmd shapeCmd = new ChangeShapeCmd(IndexOfCheckedItem(),
    //        shapeToolStripMenuItem.DropDownItems.IndexOf(SquareToolStripMenuItem));
    //    shapeCmd.Execute();
    //    UndoRedoManager.AddCmd(typeCmd, shapeCmd);
    //}
    #endregion
    #endregion
}
