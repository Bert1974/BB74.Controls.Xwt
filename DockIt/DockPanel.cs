using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xwt;
using Xwt.Drawing;

namespace BaseLib.DockIt_Xwt
{
    public class DockPanel : Canvas
    {
        private IDockLayout _content;
        internal int busy;
        private Size _size;
        private IDockSplitter dragsplit;
        private Point dragpt;
        private int dragind;
        private bool capture;
        public IXwt xwt { get; }

        internal static readonly List<DockPanel> AllDockPanels = new List<DockPanel>();
        private static IDockPane droptarget = null;
        internal static void SetHighLight(DockPanel dockPanel, Point pt, out IDockPane target, out DockPosition? hit)
        {
            target = null;
            hit = null;
            foreach (var pane in dockPanel.AllLayouts.OfType<IDockPane>())
            {
                var bounds = dockPanel.GetChildBounds(pane.Widget);

                if (bounds.Contains(pt))
                {
                    hit = pane.HitTest(new Point(pt.X - bounds.X, pt.Y - bounds.Y));
                    target = pane;
                    break;
                }
            }

            if (!object.ReferenceEquals(target, DockPanel.droptarget))
            {
                if (DockPanel.droptarget != null)
                {
                    ClrHightlight();
                }
                if ((DockPanel.droptarget = target) != null)
                {
                    DockPanel.droptarget?.SetDrop(hit);
                }
            }
            else
            {
                DockPanel.droptarget?.Update(hit);
            }

            // ClrHightlight();

        }
        internal static void ClrHightlight()
        {
            DockPanel.droptarget?.ClearDrop();
            DockPanel.droptarget = null;
        }


        public IDockDocument ActiveDocument
        {
            get; set;
        }

        private IDockLayout Current
        {
            get => this._content;
            set
            {
                if (this._content != null)
                {
                }
                if ((this._content = value) != null)
                {
                }
            }
        }

        public IEnumerable<IDockContent> AllContent
        {
            get
            {
                return _AllLayouts(this.Current).OfType<IDockPane>().SelectMany(_p => _p.Documents);
            }
        }
        public IEnumerable<IDockLayout> AllLayouts
        {
            get
            {
                return _AllLayouts(this.Current);
            }
        }
        IEnumerable<IDockLayout> _AllLayouts(IDockLayout content)
        {
            if (content != null)
            {
                yield return content as IDockLayout;

                if (content is IDockSplitter)
                {
                    foreach (var c in (content as IDockSplitter).Layouts.SelectMany(_l => _AllLayouts(_l)))
                    {
                        yield return c;
                    }
                }
            }
        }

        public void OnLoaded()
        {
            foreach (var pane in this.AllLayouts.OfType<IDockPane>())
            {
                (pane as IDockNotify).OnLoaded(pane);
            }
        }

    /*    internal static DockPanel CheckHit(IntPtr hwnd, int x, int y)
        {
            return DockPanel.AllDockPanels.SingleOrDefault(
                        _dp =>
                        {
                            if(PlatForm.Instance.GetWindow())
                        });
        }*/

        public void OnUnloading()
        {
            foreach (var pane in this.AllLayouts.OfType<IDockPane>())
            {
                (pane as IDockNotify).OnUnloading();
            }
        }
        
        public DockPanel(IXwt xwt)
        {
            this.xwt = xwt;
            this.busy++;
            this.Margin = 0;
            base.BackgroundColor = Colors.Chocolate;
            base.ExpandHorizontal = base.ExpandVertical = true;
            base.SetDragDropTarget(new TransferDataType[] { TransferDataType.Text});

            this.Current = new DockPane(this, new IDockContent[0]);
            
            this.busy--;

            DockPanel.AllDockPanels.Add(this);
        }
        protected override void Dispose(bool disposing)
        {
            DockPanel.AllDockPanels.Remove(this);

            base.Dispose(disposing);
        }

        protected override void OnChildPreferredSizeChanged()
        {
            //   base.OnChildPreferredSizeChanged();
        }
        public new void QueueDraw()
        {
            foreach (var pane in this.AllLayouts.OfType<IDockPane>().Where(_p => _p.Document != null))
            {
                (pane.Document as Canvas)?.QueueDraw();
            }
            base.QueueDraw();
        }
        protected override void OnBoundsChanged()
        {
            this._size = this.Bounds.Size;
            this._DoLayout();
            // base.OnBoundsChanged();
        }
        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            Size r = new Size(widthConstraint.AvailableSize, heightConstraint.AvailableSize);//??base.OnGetPreferredSize(widthConstraint, heightConstraint);

            this._size = r;
            return r;
        }

        internal static DockPanel[] GetHits(int x, int y)
        {
            return DockPanel.AllDockPanels.Where(_panel =>
            {
                if (_panel.ParentWindow != null)
                {
                    var wp = _panel.ConvertToScreenCoordinates(_panel.Bounds.Location);

                    if (x >= wp.X && x < wp.X + _panel.Size.Width &&
                        y >= wp.Y && y < wp.Y + _panel.Size.Height)
                    {
                        return true;
                    }
                }
                return false;
            }).ToArray();
        }
        internal static DockPanel CheckHit(IntPtr handle, double x, double y)
        {
            return DockPanel.AllDockPanels.FirstOrDefault(_dp =>
            {
                var backend = typeof(Xwt.Window).GetPropertyValuePrivate(_dp.ParentWindow, "Backend") as Xwt.Backends.IWindowFrameBackend;
                var hwsrctype = XwtImpl.GetType("System.Windows.Interop.WindowInteropHelper");
                var hwsrc = Activator.CreateInstance(hwsrctype, new object[] { backend.Window });

             //   var h = (IntPtr)backend.GetType().GetPropertyValue(backend, "NativeHandle");
                if (handle/*backend.NativeHandle*/ == (IntPtr)hwsrc.GetType().GetPropertyValue(hwsrc,"Handle")) // check if window to check
                {
                    var wp = _dp.ConvertToScreenCoordinates(_dp.Bounds.Location);

                    if (x >= wp.X && x < wp.X + _dp.Size.Width &&
                        y >= wp.Y && y < wp.Y + _dp.Size.Height)
                    {
                        return true; // hit within dockpane
                    }
                }
                return false;
            });
        }


        protected override void OnPreferredSizeChanged()
        {
            base.OnPreferredSizeChanged();
        }

        public IDockPane Dock(IDockContent testdoc, DockPosition pos, IDockPane destination)
        {
            return Dock(new IDockContent[] { testdoc }, pos, destination);
        }
        public IDockPane Dock(IDockContent[] testdoc, DockPosition pos, IDockPane destination)
        {
            IDockPane result;

            BeginLayout();

            if (this.Current == null)
            {
                this.Current = new DockPane(this, testdoc);
            }
            if (destination == null)
            {
                destination = AllLayouts.OfType<IDockPane>().FirstOrDefault(_l => _l.Documents.OfType<IDockDocument>().Any()) ??
                              AllLayouts.OfType<IDockPane>().FirstOrDefault(_l => _l.Documents.OfType<IDockContent>().Count() == 0) ??
                              AllLayouts.OfType<IDockPane>().FirstOrDefault();

                if (destination == null)// first pane?
                {
                    Debug.Assert(this.Current == null);

                    this.Current = destination = new DockPane(this, testdoc.Cast<IDockDocument>().ToArray());
                    testdoc = testdoc.OfType<IDockToolbar>().Cast<IDockContent>().ToArray();

                    if (!testdoc.Any())
                    {
                        result = destination;
                        goto done;
                    }
                }
            }
            Debug.Assert(destination != null);

            if (pos == DockPosition.Center)
            {
                destination.Add(testdoc);
                result = destination;
            }
            else
            {
                var split = FindSplitter(destination, out int ind);// this dw.Parent as DockSplitter;

                var r = (IDockPane)new DockPane(this, testdoc);
                result = r;
     
                if (object.ReferenceEquals(destination, this.Current))// object.ReferenceEquals(this.Current, this.DocumentPane))
                {
                    var oldpane = this.Current as IDockLayout;

                    this.Current = null;

                    switch (pos)
                    {
                        case DockPosition.Top:
                            this.Current = new DockSplitter(this, r, oldpane, Orientation.Vertical);
                            break;
                        case DockPosition.Bottom:
                            this.Current = new DockSplitter(this, oldpane, r, Orientation.Vertical);
                            break;
                        case DockPosition.Left:
                            this.Current = new DockSplitter(this, r, oldpane, Orientation.Horizontal);
                            break;
                        case DockPosition.Right:
                            this.Current = new DockSplitter(this, oldpane, r, Orientation.Horizontal);
                            break;
                    }
                }
                else
                {
                    var dw = destination as IDockLayout;//.Control.Parent as DockWindow;
                    switch (pos)
                    {
                        case DockPosition.Left:
                        case DockPosition.Right:
                            {
                                if (split.Orientation == Orientation.Horizontal)
                                {
                                    switch (pos)
                                    {
                                        case DockPosition.Left:
                                            split.Insert(ind, r as IDockLayout);
                                            break;
                                        case DockPosition.Right:
                                            split.Insert(ind + 1, r as IDockLayout);
                                            break;
                                    }
                                }
                                else
                                {
                                    split.Remove(destination, false);
                                    switch (pos)
                                    {
                                        case DockPosition.Left:
                                            split.Insert(ind, new DockSplitter(this, r, dw, Orientation.Horizontal));
                                            break;
                                        case DockPosition.Right:
                                            split.Insert(ind, new DockSplitter(this, dw, r, Orientation.Horizontal));
                                            break;
                                    }
                                }
                            }
                            break;
                        case DockPosition.Top:
                        case DockPosition.Bottom:
                            {
                                if (split.Orientation == Orientation.Vertical)
                                {
                                    switch (pos)
                                    {
                                        case DockPosition.Top:
                                            split.Insert(ind, r as IDockLayout);
                                            break;
                                        case DockPosition.Bottom:
                                            split.Insert(ind + 1, r as IDockLayout);
                                            break;
                                    }
                                }
                                else
                                {
                                    split.Remove(destination, false);
                                    switch (pos)
                                    {
                                        case DockPosition.Top:
                                            split.Insert(ind, new DockSplitter(this, r as IDockLayout, dw, Orientation.Vertical));
                                            break;
                                        case DockPosition.Bottom:
                                            split.Insert(ind, new DockSplitter(this, dw, r as IDockLayout, Orientation.Vertical));
                                            break;
                                    }
                                }
                            }
                            break;
                    }
                }
            }
        done:
            EndLayout();

            return result;
        }

        private void EndLayout()
        {
            if (--this.busy == 0)
            {
                this._DoLayout();
            }
        }
        private void BeginLayout()
        {
            this.busy++;
        }

        internal void SetActive(IDockContent value)
        {
            if (value is IDockDocument)
            {
                this.ActiveDocument = value as IDockDocument;
            }
            foreach (var l in this.AllLayouts.OfType<IDockPane>())
            {
                l.ActiveDocChanged();
            }
        }
        private IDockSplitter FindSplitter(IDockLayout searchfor, out int ind)
        {
            ind = -1;
            return FindSplitter(this.Current, searchfor, ref ind);
        }

        private IDockSplitter FindSplitter(IDockLayout content, IDockLayout searchfor, ref int ind)
        {
            if (content is IDockSplitter)
            {
                int cnt = 0;
                foreach (var l in (content as IDockSplitter).Layouts)
                {
                    if (object.ReferenceEquals(searchfor, l))
                    {
                        ind = cnt;
                        return content as IDockSplitter;
                    }
                    var r = FindSplitter(l, searchfor, ref ind);
                    if (r != null)
                    {
                        return r;
                    }
                    cnt++;
                }
            }
            return null;
        }

        private void _DoLayout()
        {
            if (this.busy == 0 && !Size.Zero.Equals(_size))
            {
                this.Current?.GetSize(false);
                this.Current?.Layout(this.Bounds.Location, this.Bounds.Size);
            }
        }

        protected override void OnButtonPressed(ButtonEventArgs args)
        {
            if (args.Button == PointerButton.Left)
            {
                if (this.Current.HitTest(args.Position, out IDockSplitter splitter, out int ind) && splitter != null)
                {
               //    var dragOper = this.CreateDragOperation();
             //       dragOper.Data.AddValue($"drag:{ind}");
            //        var img =  Image.FromFile("empty.png");
          //         dragOper.SetDragImage(img, 0, 0);
           //         dragOper.AllowedActions = DragDropAction.Move;
                    this.dragsplit = splitter;
                    this.dragpt = args.Position;
                    this.dragind = ind;

                    this.capture = true;

                    this.xwt.SetCapture(this);
          //          dragOper.Start();
                    return;
                }
            }
            base.OnButtonPressed(args);
        }
        
        /* protected override void OnDragOver(DragOverEventArgs args)
         {
             if (this.dragsplit != null)
             {
                 switch (this.dragsplit.Orientation)
                 {
                     case Orientation.Horizontal:
                         this.DragSplit(this.dragsplit, this.dragind, args.Position.X - this.dragpt.X );
                         this.dragpt = args.Position;
                         break;
                     case Orientation.Vertical:
                         this.DragSplit(this.dragsplit, this.dragind, args.Position.Y- this.dragpt.Y);
                         this.dragpt = args.Position;
                         break;
                 }
                 return;
             }
             base.OnDragOver(args);
         }*/

        private void DragSplit(IDockSplitter dragsplit, int ind, double d)
        {
            if (d == 0) { return; }
            Size s;
            Point pt;
            int e = 4;
            var panes = dragsplit.Layouts.ToArray();

            busy++;

            switch (dragsplit.Orientation)
            {
                case Orientation.Horizontal:
                    {

                        //      if ((Control.ModifierKeys & Keys.Shift) != 0)
                        {
                            s = panes[ind + 0].Size;
                            pt = panes[ind + 0].Location;
                            panes[ind + 0].Layout(pt, new Size(s.Width + d, s.Height));

                            pt.Offset(s.Width-d+e, 0);
                            s = panes[ind + 1].Size;
                            panes[ind + 1].Layout(pt, new Size(s.Width - d, s.Height));
                        }
                        /*     else
                             {
                                 var mi = panes.Take(ind + 1).Select(_p => (_p as Control).MinimumSize.Width).ToArray();
                                 var ci = panes.Take(ind + 1).Select(_p => (_p as Control).Width).ToArray();

                                 int e = 4;

                                 int ct = ci.Sum() - mi.Sum() + (ind + 1) * e;
                                 int nt = ct + d;

                                 double ee = 0;

                                 for (int nit = 0; nit <= ind; nit++)
                                 {
                                     var ne = ee + e + mi[nit] + (ci[nit] - mi[nit]) * nt / (double)ct;
                                     int w = (int)ne - (int)ee;
                                     s = (panes[ind] as Control).Size;
                                     (panes[ind] as Control).Location = new Point((int)ee, 0);
                                     (panes[ind] as Control).Size = new Size(w - e, s.Height);
                                     panes[ind].DoLayout();

                                     ee = ne;
                                 }
                                 mi = panes.Skip(ind + 1).Select(_p => (_p as Control).MinimumSize.Width).ToArray();
                                 ci = panes.Skip(ind + 1).Select(_p => (_p as Control).Width).ToArray();

                                 ct = ci.Sum()-mi.Sum()+((panes.Length-(ind+1))-1)*e;
                                 nt = ct - d;

                                 for (int nit = ind+1; nit <= ind; nit++)
                                 {
                                     var ne = ee + e + mi[nit] + (ci[nit] - mi[nit]) * nt / (double)ct;
                                     int w = (int)ne - (int)ee;
                                     s = (panes[ind] as Control).Size;
                                     (panes[ind] as Control).Location = new Point((int)ee, 0);
                                     (panes[ind] as Control).Size = new Size(w - e, s.Height);
                                     panes[ind].DoLayout();

                                     ee = ne;
                                 }
                             }*/

                    }
                    break;
                case Orientation.Vertical:
                    {
                        s = panes[ind + 0].Size;
                        pt = panes[ind + 0].Location;
                        panes[ind + 0].Layout(pt, new Size(s.Width, s.Height + d));

                        pt.Offset( 0, s.Height - d + e);
                        s = panes[ind + 1].Size;
                        panes[ind + 1].Layout(pt, new Size(s.Width, s.Height - d));
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
            busy--;
            _DoLayout();
        }

        protected override void OnDragStarted(DragStartedEventArgs args)
        {
       //     base.OnDragStarted(args);
        }
        protected override void OnButtonReleased(ButtonEventArgs e)
        {
            if (capture)
            {
                switch (this.dragsplit.Orientation)
                {
                    case Orientation.Horizontal:
                        this.DragSplit(this.dragsplit, this.dragind, e.Position.X - this.dragpt.X);
                        this.dragpt = e.Position;
                        break;
                    case Orientation.Vertical:
                        this.DragSplit(this.dragsplit, this.dragind, e.Position.Y - this.dragpt.Y);
                        this.dragpt = e.Position;
                        break;
                }
                this.capture = false;
                this.xwt.ReleaseCapture(this);
            }
            else
            {
                base.OnButtonReleased(e);
            }
        }
        protected override void OnGotFocus(EventArgs args)
        {
            base.OnGotFocus(args);
        }
        protected override void OnKeyPressed(KeyEventArgs args)
        {
            base.OnKeyPressed(args);
        }
        protected override void OnMouseEntered(EventArgs args)
        {
           base.OnMouseEntered(args);
        }
        protected override void OnMouseExited(EventArgs args)
        {
            base.OnMouseExited(args);
        }
        protected override void OnMouseMoved(MouseMovedEventArgs e)
        {
            if (this.capture)
            {
                switch (this.dragsplit.Orientation)
                {
                    case Orientation.Horizontal:
                        this.DragSplit(this.dragsplit, this.dragind, e.Position.X - this.dragpt.X);
                        this.dragpt = e.Position;
                        break;
                    case Orientation.Vertical:
                        this.DragSplit(this.dragsplit, this.dragind, e.Position.Y - this.dragpt.Y);
                        this.dragpt = e.Position;
                        break;
                }
            }
            else
            {
                CheckCursor(e.Position);
            }
        }

        private void CheckCursor(Point position)
        {
            if (!this.Current.HitTest(position, out IDockSplitter splitter, out int ind) || splitter == null)
            {
                base.Cursor = CursorType.Arrow;
            }
            else
            {
                switch (splitter.Orientation)
                {
                    case Orientation.Horizontal:
                        base.Cursor = CursorType.ResizeLeftRight;
                        return;
                    case Orientation.Vertical:
                        base.Cursor = CursorType.ResizeUpDown;
                        return;
                }
            }
        }
        internal void RemovePane(IDockLayout pane)
        {
            // could be mdi-child-window
            var split = FindSplitter(pane, out int ind);

            if (split != null) // should be there if not top level
            {
                pane = split.Layouts.Skip(ind).First() as IDockPane;

                RemovePane(split, pane);
            }
            else if (object.ReferenceEquals(pane, this.Current))
            {
                ClearContent();
            }
            else
            {
                Debug.Assert(false);
            }
        }
        private void ClearContent()
        {
             (this.Current as IDockPane)?.RemoveWidget();
            this.Current = null;
        }
        internal void RemovePane(IDockSplitter split, IDockLayout panesrc)
        {
            // !toplevel? -> remove from splitter
            if (!object.ReferenceEquals(this.Current, panesrc))
            {
                //   int ind = 0;
                //   while (!object.ReferenceEquals(panesrc, split.Layouts.Skip(ind).First())) { ind++; }
                split.Remove(panesrc, true);
            }
            else // reset toplevel
            {
                split = null;
                this.ClearContent();

              /*  if (this.FloatForm != null)
                {
                    this.FloatForm.Close();
                }*/
           ////     OnDocumentsChange(EventArgs.Empty);
                return;
            }
            // splitter now empty?
            if (split.Layouts.Count() == 0)
            {
                var split2 = FindSplitter(split, out int ind2);

                if (!(split2 is DockPanel))
                {
                    split2.Remove(split, true);
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            else if (split.Layouts.Count() == 1) // splitter now containg 1 item?  ->cleanup
            {
                //find splitter containing split
                var split2 = FindSplitter(split, out int ind2);

                if (split2 == null) // if split is toplevel, add 
                {
                    Debug.Assert(object.ReferenceEquals(this.Current, split));
                    this.Current = split.Layouts.First();
                }
                else if (split2 is DockSplitter) // should be
                {
                    //replace split in split2 with the item in split
                    var oldcontent = split.Layouts.First();

                    split.Remove(oldcontent, false);

                    split2.Insert(ind2, oldcontent);
                    split2.Remove(split, true);
                }
                else
                {
                    Debug.Assert(false);
                }
         ////       OnDocumentsChange(EventArgs.Empty);
            }
            else
            {
                split.GetSize(false);
        ////        OnDocumentsChange(EventArgs.Empty);
            }
        }
        public void MovePane(IDockPane panesrc, IDockContent[] doc, IDockPane panedst, DockPosition dockat)
        {
            BeginLayout();

            //move docnum (-1=all) in pansrc to panedst at dockat

            // could be mdi-child-window
            var split = FindSplitter(panesrc, out int ind);
            if (split != null)
            {
                panesrc = split.Layouts.Skip(ind).First() as IDockPane;
            }
            else // should be toplevel if not in splitter
            {
                Debug.Assert(object.ReferenceEquals(panesrc, this.Current));
            }
            // check if not moving to same place
            if (!object.ReferenceEquals(panesrc, panedst) || dockat != DockPosition.Center)
            {
                // get document to move
             /*   if (panedst?.DockPanel.FloatForm != null)
                {
                    doc = doc.Where(_doc => (_doc as IDockCustomize)?.CanFloat ?? DockPanel.DefaultFloat).ToArray();
                }*/
                // remove
                bool delsrc = panesrc.Remove(doc);

                //add
                if (dockat == DockPosition.Center)
                {
                    panedst.Add(doc);
                }
                else
                {
                    panedst = panedst.DockPanel.Dock(doc, dockat, panedst);
                }
                // if empty check remove pane
                if (delsrc)
                {
                    //  split = FindSplitter(_panesrc, out ind);
                    _CheckRemovePane(panesrc, panedst);
                    //      RemovePane(split, _panesrc);
                }
   ////             this.ActiveControl = doc.FirstOrDefault();
            }
                  EndLayout();

            //     OnDocumentsChange(EventArgs.Empty);
        }
        internal void _CheckRemovePane(IDockLayout pane, IDockPane panedst)
        {
            bool checkdocs = false;
          //  Debug.Assert(!pane.Controls.Any());

            DockPanel dp = this;

          /*  if (this.FloatForm != null)
            {
                dp = this.FloatForm.MainDockPanel;

                this.RemovePane(pane);
                pane = null;

                if (this.Content == null)
                {
                    this.FloatForm.Close();
                }
                if (panedst != null && panedst.DockPanel.FloatForm == null)
                {
                    checkdocs = true;
                }
            }
            else */
            {
                dp = this;
                if (panedst != null )//&& panedst.DockPanel.FloatForm == null)
                {
                //    Debug.Assert(panedst.GetDocuments(-1).Any());

                  /*  if (pane.DockPanel.FloatForm == null)
                    {
                        checkdocs = true; // main->main
                    }
                    else */if (panedst.Documents.Any())
                    {
                        this.RemovePane(pane);
                    }
                    else
                    {
                        if (this.AllLayouts.OfType<IDockPane>().Where(_t => !object.ReferenceEquals(pane, _t) && !_t.Documents.OfType<IDockDocument>().Any()).Count() > 1)
                        {
                            this.RemovePane(pane);
                        }
                    }
                }
                else
                {
                    /*  if (this.AllLayouts.OfType<Internals.IDockTarget>().Where(_t => !object.ReferenceEquals(pane, _t) && !_t.GetDocuments(-1).OfType<IDockDocument>().Any()).Count() > 1) // not the last 'mdidocpane'
                      {
                          this.RemovePane(pane);
                      }*/
                    checkdocs = true;
                }
            }
            if (checkdocs)
            {
                var empty = dp.AllLayouts.OfType<IDockPane>().Where(_t => !_t.Documents.Any());
                var docs = dp.AllLayouts.OfType<IDockPane>().Where(_t => _t.Documents.OfType<IDockDocument>().Any());

                if (!docs.Any()) // -> no docs, keep 1
                {
                    if (empty.Count() > 1)
                    {
                        if (empty.Any(_p => object.ReferenceEquals(_p, pane)))
                        {
                            RemovePane(pane);
                        }
                        if (empty.Count() > 1) // query, actual value
                        {
                            empty = dp.AllLayouts.OfType<IDockPane>().Where(_t => !_t.Documents.Any()).OrderBy(_p => _p.Size.Width * _p.Size.Height);

                            if (empty.Any())
                            {
                                foreach (var l in empty.Skip(1).ToArray())
                                {
                                    dp.RemovePane(l);
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (var l in empty.ToArray())
                    {
                        dp.RemovePane(l);
                    }
                }
            }
        }
    }
}
