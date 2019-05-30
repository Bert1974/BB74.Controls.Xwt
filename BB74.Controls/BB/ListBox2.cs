using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Xwt;
using Xwt.Drawing;

namespace BaseLib.Xwt.Controls
{
    static class Extensions
    {
        public static object GetPropertyValuePrivate(this Type type, object instance, string propertyname)
        {
            return type.GetProperty(propertyname, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty).GetValue(instance, new object[0]);
        }
    }
}
namespace BaseLib.Xwt.Controls
{
    /// <summary>
    /// A list of selectable items
    /// </summary>
    //[BackendType(typeof(IListBoxBackend))]
    public class ListBox2 : Canvas
    {
        class ItemCanvas : Canvas
        {
            ListBox2 owner;
            private Size reqsize;
            private long lastclick;
            internal int lastrowhit = -1;

            public ItemCanvas(ListBox2 owner)
            {
                this.owner = owner;
                this.BackgroundColor = Colors.Red;
            }
            protected override void OnDraw(Context ctx, Rectangle dirtyRect)
            {
                //   base.OnDraw(ctx, dirtyRect);

                ctx.SetColor(Colors.White);
                ctx.Rectangle(dirtyRect);
                ctx.Fill();

                if (this.owner.DataSource != null)
                {
                    for (int row = 0; row < this.owner.DataSource.RowCount; row++)
                    {
                        for (int n = 0; n < this.owner.views.Count; n++)
                        {
                            if (this.owner.rows[row][n].NeedsPaint)
                            {
                                if (dirtyRect.IntersectsWith(this.owner.rows[row][n].position))
                                {
                                    ctx.Save();

                                    this.owner.handlers[this.owner.views[n]].Draw(ctx, row, this.owner.rows[row][n]);

                                    ctx.Restore();
                                }
                            }
                        }
                    }
                }
            }
            internal void SetSize(Size size)
            {
                this.reqsize = size;
            }
            protected override void OnButtonPressed(ButtonEventArgs args)
            {
            //    base.OnButtonPressed(args);

                var hit = HitTest(args.Position);

                if (hit != null)
                {
                    if (this.lastrowhit != -1 && (Keyboard.CurrentModifiers & ModifierKeys.Shift) != 0) // range select
                    {
                        if (this.lastrowhit != hit.Row)
                        {
                            this.owner.UnselectAll();
                            for (int nit = Math.Min(this.lastrowhit, hit.Row); nit <= Math.Max(this.lastrowhit, hit.Row); nit++)
                            {
                                this.owner.SelectRow(nit);
                            }
                            this.lastrowhit = hit.Row;
                        }
                    }
                    else  if ((Keyboard.CurrentModifiers & ModifierKeys.Control) != 0)
                    {
                        if (this.owner.selectedRows.Contains(hit.Row))
                        {
                            this.owner.UnselectRow(hit.Row);
                            this.lastrowhit = -1;
                        }
                        else
                        {
                            this.owner.SelectRow(hit.Row);
                            this.lastrowhit = hit.Row;
                        }
                    }
                    else
                    {
                     /*   if (this.lastrowhit == hit.Row && args.MultiplePress == 2 && this.lastclick + .25f * 10000000 > DateTime.Now.Ticks)
                        {
                            abc
                        }
                        else
                        {
                            this.lastclick = DateTime.Now.Ticks;*/
                            this.lastrowhit = hit.Row;
                            this.owner.UnselectAll();
                            this.owner.SelectRow(hit.Row);
                      //  }
                    }
                }
               // args.Handled = true;
            }

            internal CellHandler.Cell HitTest(Point position)
            {
                for (int row = 0; row < this.owner.rows.Count; row++)
                {
                    for (int n = 0; n < this.owner.views.Count; n++)
                    {
                        if (this.owner.rows[row][n].position.Contains(position))
                        {
                            return this.owner.rows[row][n];
                        }
                    }
                }
                return null;
            }

            internal void SyncSelection()
            {
                for (int row = 0; row < this.owner.rows.Count; row++)
                {
                    bool selected = owner.selectedRows.Contains(row);
                    for (int n = 0; n < this.owner.views.Count; n++)
                    {
                        if (this.owner.rows[row][n].Widget != null)
                        {
                            this.owner.rows[row][n].Widget.BackgroundColor = selected ? Colors.LightBlue : Colors.White;
                        }
                    }
                }
            }
        }
        abstract class CellHandler
        {
            internal abstract class Cell
            {
                public CellHandler Owner { get; }
                public int Row { get; }
                internal Rectangle position;
                private Widget widget;

                protected Cell(CellHandler owner, int row)
                {
                    this.Owner = owner;
                    this.Row = row;
                }
                internal virtual Widget Widget
                {
                    get => this.widget;
                    set
                    {
                        if (this.widget != null)
                        {
                            this.widget.ButtonPressed -= Widget_ButtonPressed;
                        }
                        this.widget = value;
                        if (this.widget != null)
                        {
                            this.widget.ButtonPressed += Widget_ButtonPressed;
                        }
                    }
                }

                private void Widget_ButtonPressed(object sender, ButtonEventArgs e)
                {
                }

                internal abstract bool NeedsPaint { get; }
                internal virtual void SetPosition(ItemCanvas canvas, int row, CellHandler cellHandler, Rectangle rectangle)
                {
                    this.position = rectangle;

                    if (this.widget != null)
                    {
                        canvas.SetChildBounds(this.widget, rectangle);
                    }
                }
                internal virtual Size OnGetPreferredSize(SizeConstraint widthconstraints, SizeConstraint heightconstraints)
                {
                    return this.widget?.GetBackend().GetPreferredSize(widthconstraints, heightconstraints) ?? Size.Zero;
                }
            }

            protected readonly ListBox2 owner;
            protected readonly CellView target;

            internal abstract void Initialize();
            internal abstract void Remove();
            internal abstract Cell CreateForRow(int row);
            internal abstract void InitialzeForRow(int row, Cell cell);
            internal abstract void DestroyForRow(int row, Cell cell);
            internal abstract void Sync(int row, Cell cell);


            protected CellHandler(ListBox2 owner, CellView cell)
            {
                this.owner = owner;
                this.target = cell;
            }

            internal virtual void Draw(Context ctx, int row, Cell cell)
            {
                ctx.SetColor(Colors.Yellow);
                ctx.Rectangle(cell.position);
                ctx.Fill();
            }
            /*    internal Size OnGetPreferredSize(SizeConstraint sizeConstraint, SizeConstraint unconstrained)
                {
                }
                */
        }
        class ImageCellHandler : CellHandler
        {
            internal class ImageCell : Cell
            {
                internal override bool NeedsPaint => this.image != null;

                private ImageCellHandler owner;
                internal Image image;

                public ImageCell(ImageCellHandler owner, int row)
                    : base(owner, row)
                {
                    this.owner = owner;
                }

                internal override Size OnGetPreferredSize(SizeConstraint widthConstraints, SizeConstraint heightConstraints)
                {
                    return new Size(widthConstraints.CalculateFor(this.image?.Width ?? 0), heightConstraints.CalculateFor(this.image?.Height ?? 0));
                }
            }
            public ImageCellHandler(ListBox2 owner, CellView cell)
                : base(owner, cell)
            {
            }

            internal override void Initialize()
            {
            }
            internal override void Remove()
            {
            }
            internal override Cell CreateForRow(int row)
            {
                return new ImageCell(this, row);
            }
            internal override void InitialzeForRow(int row, Cell cell)
            {
                Sync(row, cell);
            }
            internal override void DestroyForRow(int row, Cell cell)
            {
            }
            internal override void Sync(int row, Cell cell)
            {
                var imagecell = cell as ImageCell;

                if ((this.target.VisibleField?.Index ?? -1) != -1 ? (bool)owner.DataSource.GetValue(row, this.target.VisibleField.Index) : this.target.Visible)
                {
                    imagecell.image = (Image)this.owner.DataSource.GetValue(row, this.owner.views.IndexOf(this.target));
                }
            }
            internal override void Draw(Context ctx, int row, Cell cell)
            {
                base.Draw(ctx, row, cell);
                var imagecell = cell as ImageCell;

                if (imagecell.image != null)
                {
                    ctx.DrawImage(imagecell.image, cell.position);
                }
            }
        }
        class TextCellHandler : CellHandler
        {
            internal class TextCell : Cell
            {
                internal override bool NeedsPaint => !this.editable;

                private TextCellHandler owner;
                internal bool editable = false;

                public TextCell(TextCellHandler owner, int row)
                    : base(owner, row)
                {
                    this.owner = owner;
                }
            }
            public TextCellHandler(ListBox2 owner, CellView cell)
                : base(owner, cell)
            {
            }
            internal override void Initialize()
            {
            }
            internal override void Remove()
            {
            }
            internal override Cell CreateForRow(int row)
            {
                return new TextCell(this, row);
            }
            internal override void InitialzeForRow(int row, Cell cell)
            {
                if ((this.target.VisibleField?.Index ?? -1) != -1 ? (bool)owner.DataSource.GetValue(row, this.target.VisibleField.Index) : this.target.Visible)
                {
                    var txtcell = cell as TextCell;
                    if ((target as TextCellView).EditableField != null)
                    {
                        txtcell.editable = (bool)this.owner.DataSource.GetValue(row, (target as TextCellView).EditableField.Index);
                    }
                    else
                    {
                        txtcell.editable = (target as TextCellView).Editable;
                    }
                    if (txtcell.editable)
                    {
                        txtcell.Widget = new TextEntry() { };
                    }
                    else
                    {
                        txtcell.Widget = new Label() { };
                    }
                }
                Sync(row, cell);
            }
            internal override void DestroyForRow(int row, Cell cell)
            {
            }

            internal override void Sync(int row, Cell cell)
            {
                // check visible change ??

                var txtcell = cell as TextCell;

                if ((this.target.VisibleField?.Index ?? -1) != -1 ? (bool)owner.DataSource.GetValue(row, this.target.VisibleField.Index) : this.target.Visible)
                {
                    var txt = (string)this.owner.DataSource.GetValue(row, this.owner.views.IndexOf(this.target));

                    if (txtcell.Widget is TextEntry)
                    {
                        (txtcell.Widget as TextEntry).Text = txt;
                    }
                    else
                    {
                        (txtcell.Widget as Label).Text = txt;
                    }
                }
            }
        }

        CellViewCollection views;
        private readonly Canvas scrollplace;
        private ItemCanvas viewplace;
        IListDataSource source;
        SelectionMode mode;
        Dictionary<CellView, CellHandler> handlers = new Dictionary<CellView, CellHandler>();
        List<CellHandler.Cell[]> rows = new List<CellHandler.Cell[]>();

        EventHandler<ListViewRowEventArgs> rowActivated;
        //   private HBox hbox;

        public ListBox2()
        {
            Type t = Platform.GetType("Xwt.CellViewCollection");

            views = (CellViewCollection)Activator.CreateInstance(t, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { null }, null);
            //  views = new CellViewCollection(null/*BackendHost*/);

            this.scrollplace = new Canvas()
            {
                ExpandHorizontal = true,
                ExpandVertical = true,
                HorizontalPlacement = WidgetPlacement.Fill,
                VerticalPlacement = WidgetPlacement.Fill,
                BackgroundColor = Colors.Red
            };
            this.viewplace = new ItemCanvas(this);

            this.scrollplace.BoundsChanged += (s, a) => SyncRows();

            this.scrollplace.AddChild(this.viewplace);

            this.vscroll = new VScrollbar() { ExpandHorizontal = false, ExpandVertical = true };

            this.vscroll.ValueChanged += (s, a) =>
            {
                var r = new Rectangle(0, -this.vscroll.Value, this.viewplace.Bounds.Width, this.viewplace.Bounds.Height);
                this.scrollplace.SetChildBounds(this.viewplace, r);
            };

            this.hbox = new HBox2() { ExpandHorizontal = true, ExpandVertical = true };

            this.hbox.PackStart(this.scrollplace, true, vpos: WidgetPlacement.Fill, hpos: WidgetPlacement.Fill);
            this.hbox.PackStart(this.vscroll, false, vpos: WidgetPlacement.Fill, hpos: WidgetPlacement.Fill);

            this.vbox = new VBox2() { ExpandHorizontal = true, ExpandVertical = true };
            this.vbox.PackStart(this.hbox, true);

            base.AddChild(this.vbox);

            this.scrollplace.ClipToBounds();
        }
        private void sync_viewpos()
        {
        }

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            return new Size(100, 100);// base.OnGetPreferredSize(widthConstraint, heightConstraint);
        }
        protected override void OnBoundsChanged()
        {
            base.OnBoundsChanged();

            this.SetChildBounds(this.vbox, new Rectangle(Point.Zero, this.Bounds.Size));

            this.vbox.QueueForReallocate();

            //     var scrollsize = this.scroll.GetBackend().GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);

            //     base.SetChildBounds(this.viewplace,)

        }
        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);
        }
        /*       /// <summary>
               /// Creates the backend for this <see cref="Xwt.XwtObject"/>.
               /// </summary>
               protected override object OnCreateBackend()
               {
                   return base.OnCreateBackend();
               }
               */

        /// <summary>
        /// Views to be used to display the data of the items
        /// </summary>
        public CellViewCollection Views
        {
            get { return views; }
        }


        private bool SetViews()
        {
            if (!views.SequenceEqual(this.handlers.Keys))
            {
                ClearRows();

                foreach (CellView cell in views)
                {
                    Console.WriteLine($"{typeof(XwtComponent).GetPropertyValuePrivate(cell, "BackendHost")}");
                    //     Console.WriteLine($"{typeof(Xwt.CellView).GetPropertyValuePrivate(cell, "Backend")}");

                    if (!handlers.Keys.Contains(cell))
                    {
                        if (cell.GetType() == typeof(ImageCellView))
                        {
                            handlers[cell] = new ImageCellHandler(this, cell);
                            handlers[cell].Initialize();
                        }
                        if (cell.GetType() == typeof(TextCellView))
                        {
                            handlers[cell] = new TextCellHandler(this, cell);
                            handlers[cell].Initialize();
                        }
                    }
                }
                var old = handlers.Where(_c => !views.Contains(_c.Key)).ToArray();
                foreach (var kv in old)
                {
                    kv.Value.Remove();
                }
                FillRows();
                this.viewplace.lastrowhit = -1;
                return true;
            }
            return false;
        }

        private void ClearRows()
        {
            for (int row = 0; row < rows.Count; row++)
            {
                Debug.Assert(this.views.Count == rows[row].Length);
                for (int nit = 0; nit < rows[row].Length; nit++)
                {
                    if (this.rows[row][nit].Widget != null)
                    {
                        this.viewplace.RemoveChild(this.rows[row][nit].Widget);
                    }
                    handlers[views[nit]].DestroyForRow(row, rows[row][nit]);
                }
            }
            rows.Clear();
            this.viewplace.Clear();
        }

        private void FillRows()
        {
            ClearRows();
            if (!SetViews())
            {
                for (int row = 0; row < this.DataSource.RowCount; row++)
                {
                    rows.Add(this.views.Select(_v => handlers[_v].CreateForRow(row)).ToArray());

                    for (int n = 0; n < this.views.Count; n++)
                    {
                        handlers[views[n]].InitialzeForRow(row, rows[row][n]);
                    }
                }
                var ww = Enumerable.Repeat(0.0, this.views.Count).ToArray();
                var hh = 0.0;

                SizeConstraint heightconstraints = this.ItemHeight <= 0 ? SizeConstraint.Unconstrained : SizeConstraint.WithSize(this.ItemHeight);
                var rowh = new double[this.DataSource.RowCount];

                for (int row = 0; row < this.DataSource.RowCount; row++)
                {
                    double h2 = 0;
                    for (int n = 0; n < this.views.Count; n++)
                    {
                        Size cellprefsize = rows[row][n].OnGetPreferredSize(SizeConstraint.Unconstrained, heightconstraints);

                        h2 = Math.Max(h2, cellprefsize.Height);
                        ww[n] = Math.Max(ww[n], cellprefsize.Width);
                    }
                    if (this.ItemHeight <= 0)
                    {
                        rowh[row] = h2;
                        hh += h2;
                    }
                    else
                    {
                        hh = Math.Max(hh, h2);
                    }
                }
                if (this.ItemHeight <= 0)
                {
                    hh = 0;
                }
                else
                {
                    hh = Math.Min(this.ItemHeight, hh);
                }
                for (int row = 0; row < this.DataSource.RowCount; row++)
                {
                    bool selected = selectedRows.Contains(row);
                    for (int n = 0; n < this.views.Count; n++)
                    {
                        if (this.rows[row][n].Widget != null)
                        {
                            this.viewplace.AddChild(this.rows[row][n].Widget);
                            this.rows[row][n].Widget.BackgroundColor = selected ? Colors.LightBlue : Colors.White;
                        }
                        if (this.ItemHeight <= 0)
                        {
                            this.rows[row][n].SetPosition(this.viewplace, row, this.handlers[this.views[n]], new Rectangle(ww.Take(n).Sum(), hh, ww[n], rowh[row]));
                        }
                        else
                        {
                            rowh[row] = hh;
                            this.rows[row][n].SetPosition(this.viewplace, row, this.handlers[this.views[n]], new Rectangle(ww.Take(n).Sum(), hh * row, ww[n], rowh[row]));
                        }
                    }
                    if (this.ItemHeight <= 0)
                    {
                        hh += rowh[row];
                    }
                }
                var r = new Rectangle(0, -this.vscroll.Value, Math.Max(this.scrollplace.Bounds.Width, ww.Sum()), Math.Max(this.scrollplace.Bounds.Height, rowh.Sum()));
                this.scrollplace.SetChildBounds(this.viewplace, r);

                this.vscroll.PageIncrement = this.scrollplace.Bounds.Height;
                this.vscroll.UpperValue = Math.Max(0,rowh.Sum()- this.scrollplace.Bounds.Height);

                sync_viewpos();
            }
        }

        private void SyncRows()
        {
            if (!SetViews())
            {
                var ww = Enumerable.Repeat(0.0, this.views.Count).ToArray();
                var hh = 0.0;

                SizeConstraint heightconstraints = this.ItemHeight <= 0 ? SizeConstraint.Unconstrained : SizeConstraint.WithSize(this.ItemHeight);
                var rowh = new double[this.DataSource.RowCount];

                for (int row = 0; row < this.DataSource.RowCount; row++)
                {
                    double h2 = 0;
                    for (int n = 0; n < this.views.Count; n++)
                    {
                        handlers[this.views[n]].Sync(row, rows[row][n]);
                        Size cellprefsize = rows[row][n].OnGetPreferredSize(SizeConstraint.Unconstrained, heightconstraints);

                        h2 = Math.Max(h2, cellprefsize.Height);
                        ww[n] = Math.Max(ww[n], cellprefsize.Width);
                    }
                    if (this.ItemHeight <= 0)
                    {
                        rowh[row] = h2;
                        hh += h2;
                    }
                    else
                    {
                        hh = Math.Max(hh, h2);
                    }
                }
                if (this.ItemHeight <= 0)
                {
                    hh = 0;
                }
                else
                {
                    hh = Math.Min(this.ItemHeight, hh);
                }
                for (int row = 0; row < this.DataSource.RowCount; row++)
                {
                    for (int n = 0; n < this.views.Count; n++)
                    {
                        if (this.ItemHeight <= 0)
                        {
                            this.rows[row][n].SetPosition(this.viewplace, row, this.handlers[this.views[n]], new Rectangle(ww.Take(n).Sum(), hh, ww[n], rowh[row]));
                        }
                        else
                        {
                            rowh[row] = hh;
                            this.rows[row][n].SetPosition(this.viewplace, row, this.handlers[this.views[n]], new Rectangle(ww.Take(n).Sum(), hh * row, ww[n], rowh[row]));
                        }
                    }
                    if (this.ItemHeight <= 0)
                    {
                        hh += rowh[row];
                    }
                }
            }
        }
        /// <summary>
        /// Gets or sets the data source from which to get the data of the items
        /// </summary>
        /// <value>
        /// The data source.
        /// </value>
        /// <remarks>
        /// Then a DataSource is set, the Items collection can't be used.
        /// </remarks>
        public IListDataSource DataSource
        {
            get { return source; }
            set
            {
                BackendHost.ToolkitEngine.ValidateObject(value);
                if (source != null)
                {
                    source.RowChanged -= Source_RowChanged;
                    source.RowDeleted -= HandleModelChanged;
                    source.RowInserted -= HandleModelChanged;
                    source.RowsReordered -= HandleModelChanged;
                }

                source = value;
                //     Backend.SetSource(source, source is IFrontend ? (IBackend)Toolkit.GetBackend(source) : null);
                FillRows();
                this.viewplace.lastrowhit = -1;

                if (source != null)
                {
                    source.RowChanged += Source_RowChanged;
                    source.RowDeleted += HandleModelChanged;
                    source.RowInserted += HandleModelChanged;
                    source.RowsReordered += HandleModelChanged;
                }
            }
        }


        /*     /// <summary>
    /// Gets or sets the vertical scroll policy.
    /// </summary>
    /// <value>
    /// The vertical scroll policy.
    /// </value>
    public ScrollPolicy VerticalScrollPolicy
    {
        get { return Backend.VerticalScrollPolicy; }
        set { Backend.VerticalScrollPolicy = value; }
    }

    /// <summary>
    /// Gets or sets the horizontal scroll policy.
    /// </summary>
    /// <value>
    /// The horizontal scroll policy.
    /// </value>
    public ScrollPolicy HorizontalScrollPolicy
    {
        get { return Backend.HorizontalScrollPolicy; }
        set { Backend.HorizontalScrollPolicy = value; }
    }*/
        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        /// <value>
        /// The selection mode.
        /// </value>
        public SelectionMode SelectionMode
        {
            get
            {
                return mode;
            }
            set
            {
                mode = value;
                //     Backend.SetSelectionMode(mode);
            }
        }
        /// <summary>
        /// Gets the selected row.
        /// </summary>
        /// <value>
        /// The selected row.
        /// </value>
        public int SelectedRow
        {
            get
            {
                if (selectedRows.Count == 0)
                    return -1;
                else
                    return selectedRows[0];
            }
        }

       /* public object SelectedItem
        {
            get
            {
               if (SelectedRow == -1)
                return null;
                     return Items[SelectedRow];
            }
            set
            {
                if (SelectionMode == Xwt.SelectionMode.Multiple)
                    UnselectAll();
                var i = Items.IndexOf(value);
                if (i != -1)
                    SelectRow(i);
                else
                    UnselectAll();
            }
        }*/

        /// <summary>
        /// Gets the selected rows.
        /// </summary>
        /// <value>
        /// The selected rows.
        /// </value>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int[] SelectedRows
        {
            get
            {
                return this.selectedRows.ToArray();
            }
        }

        /// <summary>
        /// Gets or sets the focused row.
        /// </summary>
        /// <value>The row with the keyboard focus.</value>
        public int FocusedRow
        {
            get
            {
                return this.viewplace.lastrowhit;
            }
            set
            {
                this.viewplace.lastrowhit = value;
            }
        }

        public double ItemHeight { get; private set; } = -1;

        /// <summary>
        /// Selects a row.
        /// </summary>
        /// <param name='row'>
        /// a row.
        /// </param>
        /// <remarks>
        /// In single selection mode, the row will be selected and the previously selected row will be deselected.
        /// In multiple selection mode, the row will be added to the set of selected rows.
        /// </remarks>
        public void SelectRow(int row)
        {
            if (!this.selectedRows.Contains(row))
            {
                this.selectedRows.Add(row);

                this.viewplace.SyncSelection();
                OnSelectionChanged(EventArgs.Empty);
            }
            //   Backend.SelectRow(row);
        }

        /// <summary>
        /// Unselects a row.
        /// </summary>
        /// <param name='row'>
        /// A row
        /// </param>
        public void UnselectRow(int row)
        {
            if (this.selectedRows.Contains(row))
            {
                this.selectedRows.Remove(row);
                OnSelectionChanged(EventArgs.Empty);
                this.viewplace.SyncSelection();
            }
        }

        /// <summary>
        /// Selects all rows
        /// </summary>
        public void SelectAll()
        {
            this.selectedRows.Clear();
            this.selectedRows.AddRange(Enumerable.Range(0, this.rows.Count));
            this.viewplace.SyncSelection();
            OnSelectionChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Clears the selection
        /// </summary>
        public void UnselectAll()
        {
            this.selectedRows.Clear();
            this.viewplace.SyncSelection();
            OnSelectionChanged(EventArgs.Empty);
            //      Backend.UnselectAll();
        }

        public void ScrollToRow(int row)
        {
            //      Backend.ScrollToRow(row);
        }

        /// <summary>
        /// Returns the row at the given widget coordinates
        /// </summary>
        /// <returns>The row index</returns>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        public int GetRowAtPosition(double x, double y)
        {
            return GetRowAtPosition(new Point(x, y));
        }

        /// <summary>
        /// Returns the row at the given widget coordinates
        /// </summary>
        /// <returns>The row index</returns>
        /// <param name="p">A position, in widget coordinates</param>
        public int GetRowAtPosition(Point p)
        {
            return this.viewplace.HitTest(p.Offset(-this.viewplace.ParentBounds.Location.X,-this.viewplace.ParentBounds.Location.Y))?.Row ?? -1;

        }

   /*     /// <summary>
        /// Gets the bounds of the given row.
        /// </summary>
        /// <returns>The row bounds inside the widget, relative to the widget bounds.</returns>
        /// <param name="row">The row index.</param>
        /// <param name="includeMargin">If set to <c>true</c> include margin (the background of the row).</param>
        public Rectangle GetRowBounds(int row, bool includeMargin)
        {
            return Rectangle.Zero;
            //      return Backend.GetRowBounds(row, includeMargin);
        }
        */
        void Source_RowChanged(object sender, ListRowEventArgs e)
        {
            for (int nit = 0; nit < this.views.Count; nit++)
            {
                this.handlers[this.views[nit]].Sync(e.Row, this.rows[e.Row][nit]);
            }
        }
        void HandleModelChanged(object sender, ListRowEventArgs e)
        {
            FillRows();
            this.viewplace.lastrowhit = -1;

        }

        void OnCellChanged()
        {
            SyncRows();
            //      Backend.SetViews(views);
        }
        EventHandler selectionChanged;
        private VBox2 vbox;
        private VScrollbar vscroll;
        private HBox2 hbox;
        private List<int> selectedRows=new List<int>();

        /// <summary>
        /// Occurs when selection changes
        /// </summary>
        public event EventHandler SelectionChanged
        {
            add
            {
                //           BackendHost.OnBeforeEventAdd(TableViewEvent.SelectionChanged, selectionChanged);
                selectionChanged += value;
            }
            remove
            {
                selectionChanged -= value;
                //             BackendHost.OnAfterEventRemove(TableViewEvent.SelectionChanged, selectionChanged);
            }
        }

        /// <summary>
        /// Raises the selection changed event.
        /// </summary>
        /// <param name='args'>
        /// Arguments.
        /// </param>
        protected virtual void OnSelectionChanged(EventArgs args)
        {
            if (selectionChanged != null)
                selectionChanged(this, args);
        }

        /// <summary>
        /// Raises the row activated event.
        /// </summary>
        /// <param name="a">The alpha component.</param>
        protected virtual void OnRowActivated(ListViewRowEventArgs a)
        {
            if (rowActivated != null)
                rowActivated(this, a);
        }
        /// <summary>
        /// Occurs when the user double-clicks on a row
        /// </summary>
        public event EventHandler<ListViewRowEventArgs> RowActivated
        {
            add
            {
                //          BackendHost.OnBeforeEventAdd(ListViewEvent.RowActivated, rowActivated);
                rowActivated += value;
            }
            remove
            {
                rowActivated -= value;
                //          BackendHost.OnAfterEventRemove(ListViewEvent.RowActivated, rowActivated);
            }
        }
    }
}