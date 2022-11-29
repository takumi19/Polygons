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
            _pluggedInTypes = PluginManager.ExportTypesFrom((string[])e.Data.GetData(DataFormats.FileDrop));
            _pluggedInTypes.ForEach(type => CreateToolStripMenuItem(type));
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
            if (_dragDropCmd == null) 
                return;
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
            (_sizeChanger.ActiveControl as TrackBar).Value = _polygon.VertexSize;
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
}
