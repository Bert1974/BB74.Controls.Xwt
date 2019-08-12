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
    public class ListBox2 : FrameBox, ICellHandlerContainer
    {
        class ItemCanvas : Canvas
        {
            ListBox2 owner;
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
                        Rectangle? painted = null;
                        for (int n = 0; n < this.owner.views.Count; n++)
                        {
                            if (this.owner.rows[row][n].NeedsPaint)
                            {
                                if (!painted.HasValue)
                                {
                                    painted = this.owner.rows[row][n].position;
                                }
                                else
                                {
                                    painted = painted.Value.Union(this.owner.rows[row][n].position);
                                }
                                if (dirtyRect.IntersectsWith(this.owner.rows[row][n].position))
                                {
                                    ctx.Save();

                                    this.owner.handlers[this.owner.views[n]].Draw(ctx, row, this.owner.rows[row][n]);

                                    ctx.Restore();
                                }
                            }
                        }
                        if (painted.HasValue && this.owner.FullRowSelect)
                        {
                            var linerec = RowFromCellRect(painted);
                            if (linerec.Width > 0 && linerec.IntersectsWith(dirtyRect))
                            {
                                ctx.SetColor((this.owner as ICellHandlerContainer).Selected(row) ? Colors.LightBlue : Colors.White);
                                ctx.Rectangle(linerec);
                                ctx.Fill();
                            }
                        }
                    }
                }
            }

            private Rectangle RowFromCellRect(Rectangle? painted)
            {
                return new Rectangle(painted.Value.Right, painted.Value.Top, this.Size.Width - painted.Value.Right, painted.Value.Height).WithPositiveSize();
            }
            protected override void OnButtonPressed(ButtonEventArgs args)
            {
                //    base.OnButtonPressed(args);

                var hit = HitTest(args.Position,out int hitrow);

                if (hitrow != -1)
                {
                    if (this.lastrowhit != -1 && (Keyboard.CurrentModifiers & ModifierKeys.Shift) != 0) // range select
                    {
                        if (this.lastrowhit != hitrow)
                        {
                            this.owner.UnselectAll();
                            for (int nit = Math.Min(this.lastrowhit, hitrow); nit <= Math.Max(this.lastrowhit, hitrow); nit++)
                            {
                                this.owner.SelectRow(nit);
                            }
                            this.lastrowhit = hitrow;
                        }
                    }
                    else if ((Keyboard.CurrentModifiers & ModifierKeys.Control) != 0)
                    {
                        if (this.owner.selectedRows.Contains(hitrow))
                        {
                            this.owner.UnselectRow(hitrow);
                            this.lastrowhit = -1;
                        }
                        else
                        {
                            this.owner.SelectRow(hitrow);
                            this.lastrowhit = hitrow;
                        }
                    }
                    else
                    {
                        /*   if (this.lastrowhit == hitrow && args.MultiplePress == 2 && this.lastclick + .25f * 10000000 > DateTime.Now.Ticks)
                           {
                               abc
                           }
                           else
                           {
                               this.lastclick = DateTime.Now.Ticks;*/
                        this.lastrowhit = hitrow;
                        this.owner.UnselectAll();
                        this.owner.SelectRow(hitrow);
                        //  }
                    }
                }
                // args.Handled = true;
            }
            internal CellHandler.Cell HitTest(Point position, out int hitrow)
            {
                for (int row = 0; row < this.owner.rows.Count; row++)
                {
                    Rectangle? painted = null;
                    for (int n = 0; n < this.owner.views.Count; n++)
                    {
                        if (!painted.HasValue)
                        {
                            painted = this.owner.rows[row][n].position;
                        }
                        else
                        {
                            painted = painted.Value.Union(this.owner.rows[row][n].position);
                        }
                        if (this.owner.rows[row][n].position.Contains(position))
                        {
                            hitrow = row;
                            return this.owner.rows[row][n];
                        }
                    }
                    if (this.owner.FullRowSelect && painted.HasValue)
                    {
                        if (RowFromCellRect(painted.Value).Contains(position))
                        {
                            hitrow = row;
                            return null;
                        }
                    }
                }
                hitrow = -1;
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
                base.QueueDraw();
            }

            protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
            {
                var s = this.GetMaxSize();

            /*    if (widthConstraint.IsConstrained)
                {
                    s.Width = Math.Max(s.Width, widthConstraint.AvailableSize);
                }
                if (heightConstraint.IsConstrained)
                {
                    s.Height = Math.Max(s.Height, heightConstraint.AvailableSize);
                }*/
                return s;
            }
        }
    
        public new bool ExpandVertical { get => base.ExpandVertical; set => base.ExpandVertical = value; }
        public new bool ExpandHorizontal { get => base.ExpandHorizontal; set => base.ExpandHorizontal = value; }

    CellViewCollection views;
        private readonly ScrollControl2 scroller;
        private ItemCanvas viewplace;
        IListDataSource source;
        SelectionMode mode;
        Dictionary<CellView, CellHandler> handlers = new Dictionary<CellView, CellHandler>();
        List<CellHandler.Cell[]> rows = new List<CellHandler.Cell[]>();
        private List<int> selectedRows = new List<int>();

        EventHandler selectionChanged;
        EventHandler<ListViewRowEventArgs> rowActivated;
        private int busy;
        private bool dirty;

        //   private HBox hbox;

        public ListBox2()
        {
            Type t = Platform.GetType("Xwt.CellViewCollection");

            views = (CellViewCollection)Activator.CreateInstance(t, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { null }, null);
            //  views = new CellViewCollection(null/*BackendHost*/);

            this.BorderWidth = this.Padding = 0;

            this.scroller = new ScrollControl2();
            this.scroller.ExpandHorizontal = true;
            this.scroller.ExpandVertical = true;
            this.scroller.HorizontalPlacement = WidgetPlacement.Fill;
            this.scroller.VerticalPlacement = WidgetPlacement.Fill;

            this.viewplace = new ItemCanvas(this);

            this.scroller.Content = this.viewplace;
            
            base.Content=this.scroller;
        }

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
                        handlers[cell] = CellHandler.CreateFor(this, cell);
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
                    handlers[views[nit]].DestroyForRow(rows[row][nit]);
                }
            }
            rows.Clear();
            selectedRows.Clear();
            this.viewplace.Clear();
        }

        private void FillRows()
        {
            ClearRows();
            if (!SetViews())
            {
                for (int row = 0; row < this.DataSource.RowCount; row++)
                {
                    rows.Add(this.views.Select((_v, _n) => handlers[_v].CreateForRow(row, _n)).ToArray());

                    for (int n = 0; n < this.views.Count; n++)
                    {
                        handlers[views[n]].InitialzeForRow(rows[row][n]);
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
                //     SetScroll(ww, rowh);

                //   sync_viewpos();
            }
            this.scroller.Refresh();
            this.viewplace.QueueDraw();
        }

        private void SyncRows()
        {
            if (!SetViews())
            {
                if (this.DataSource != null)
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
                            handlers[this.views[n]].Sync(rows[row][n]);
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
                    //       SetScroll(ww, rowh);
                    //          sync_viewpos();
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
                    source.RowDeleted -= Source_RowDeleted;
                    source.RowInserted -= Source_RowInserted;
                    source.RowsReordered -= HandleModelChanged;
                }

                source = value;
                //     Backend.SetSource(source, source is IFrontend ? (IBackend)Toolkit.GetBackend(source) : null);
                FillRows();
                this.viewplace.lastrowhit = -1;

                if (source != null)
                {
                    source.RowChanged += Source_RowChanged;
                    source.RowDeleted += Source_RowDeleted;
                    source.RowInserted += Source_RowInserted;
                    source.RowsReordered += HandleModelChanged;
                }
            }
        }

        public void SuspendLayout()
        {
            this.busy++;
        }
        public void ResumeLayout()
        {
            if (--this.busy == 0)
            {
                if (this.dirty)
                {
                    FillRows();
                    this.viewplace.lastrowhit = -1;
                }
                this.dirty = false;
            }
        }
        void Source_RowChanged(object sender, ListRowEventArgs e)
        {
            if (this.busy > 0)
            {
                this.dirty = true;
                return;
            }
            for (int nit = 0; nit < this.views.Count; nit++)
            {
                this.handlers[this.views[nit]].Sync(this.rows[e.Row][nit]);
            }
            this.viewplace.QueueDraw();
        }
        private void Source_RowInserted(object sender, ListRowEventArgs e)
        {
            if (this.busy > 0)
            {
                this.dirty = true;
                return;
            }
            if (!SetViews())
            {
                rows.Insert(e.Row, this.views.Select((_v,_n) => handlers[_v].CreateForRow(e.Row,_n)).ToArray());

                for (int n = 0; n < this.views.Count; n++)
                {
                    handlers[views[n]].InitialzeForRow(rows[e.Row][n]);
                }
                for (int n = 0; n < this.views.Count; n++)
                {
                    if (this.rows[e.Row][n].Widget != null)
                    {
                        this.viewplace.AddChild(this.rows[e.Row][n].Widget);
                    }
                }
                this.SyncRows();
            }
            this.viewplace.QueueDraw();
        }

        private void Source_RowDeleted(object sender, ListRowEventArgs e)
        {
            if (this.busy > 0)
            {
                this.dirty = true;
                return;
            }
            if (this.DataSource.RowCount == 0)
            {
                if (this.rows.Count > 0)
                {
                    for (int row = 0; row < this.rows.Count; row++)
                    {
                        for (int n = 0; n < this.views.Count; n++)
                        {
                            if (this.rows[row][n].Widget != null)
                            {
                                this.viewplace.RemoveChild(this.rows[row][n].Widget);
                            }
                        }
                    }
                    this.rows.Clear();
                    this.selectedRows.Clear();
                }
            }
            else
            {
                int ind = this.rows.IndexOf(this.rows.FirstOrDefault(_r => _r.First().Row == e.Row));

                for (int n = 0; n < this.views.Count; n++)
                {
                    if (this.rows[ind][n].Widget != null)
                    {
                        this.viewplace.RemoveChild(this.rows[ind][n].Widget);
                    }
                }
                for (int nit = e.Row; nit < rows.Count; nit++)
                {
                    foreach (var c in this.rows[nit])
                    {
                        c.Row--;
                    }
                }
                rows.RemoveAt(ind);
                if (this.selectedRows.Contains(ind))
                {
                    this.selectedRows.Remove(ind);
                }
                if (this.DataSource.RowCount > 0)
                {
                    this.SyncRows();
                }
            }
            this.viewplace.QueueDraw();
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

        private bool fullrowselect = false;

        public double ItemHeight { get; private set; } = -1;
        public bool FullRowSelect { get => fullrowselect; set { this.fullrowselect = value; this.viewplace.QueueDraw(); } }

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
                this.viewplace.SyncSelection();
                OnSelectionChanged(EventArgs.Empty);
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
            this.viewplace.HitTest(p.Offset(-this.viewplace.ParentBounds.Location.X,-this.viewplace.ParentBounds.Location.Y),out int hitrow);
            return hitrow;
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
        void HandleModelChanged(object sender, ListRowEventArgs e)
        {
            if (this.busy > 0)
            {
                this.dirty = true;
                return;
            }
            FillRows();
            this.viewplace.lastrowhit = -1;

        }

      /*  void OnCellChanged()
        {
            SyncRows();
            //      Backend.SetViews(views);
        }*/

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

        bool ICellHandlerContainer.Selected(int row)
        {
            return this.selectedRows.Contains(row);
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
