using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Xwt;
using Xwt.Drawing;

namespace BaseLib.Xwt.Controls.DockPanel.Internals
{
    using Xwt = global::Xwt;

    internal class DockPane : Canvas, IDockPane, IDockNotify
    {
        #region DropTarget
        class DropTarget : Xwt.Canvas
        {
            private DockPane dockPane;
            public readonly DockPosition pos;

            public DropTarget(DockPane dockPane, DockPosition pos)
            {
                this.dockPane = dockPane;
                this.pos = pos;
                this.WidthRequest = dockPane.wh;
                this.HeightRequest = dockPane.wh;
                this.Opacity = .8f;
                this.BackgroundColor = DockPanel.DropTargetColor;
            }

            internal void SetHighLight(bool highlighted)
            {
                if (highlighted)
                {
                    this.BackgroundColor = DockPanel.DropTargetColorSelected;
                }
                else
                {
                    this.BackgroundColor = DockPanel.DropTargetColor;
                }
                this.QueueDraw();
            }
            protected override void OnDraw(Context ctx, Rectangle dirtyRect)
            {
                base.OnDraw(ctx, dirtyRect);

                ctx.SetColor(Colors.Black);
                ctx.Rectangle(this.Bounds);
                ctx.Stroke();
            }
        }
        #endregion
        
        private List<IDockContent> _docs = new List<IDockContent>();
        private IDockContent _activedoc;

        public DockPanel DockPanel { get; private set; }
        public IDockContent Document
        {
            get => _activedoc;
            private set
            {
                if (!object.ReferenceEquals(this._activedoc, value))
                {
                    if (this._activedoc != null)
                    {
                        Debug.Assert(this.DockPanel.MainDockPanel.onloadedfired);
                        (this as IDockNotify).OnUnloading();
                        this.RemoveChild(this._activedoc.Widget);
                    }
                    if ((this._activedoc = value) != null)
                    {
                        this.AddChild(this._activedoc.Widget);
                        this.SetChildBounds(this._activedoc.Widget, DocumentRectangle);

                        if ((this.ParentWindow?.Visible ?? false)  && this.DockPanel.MainDockPanel.onloadedfired)
                        {
                            (this as IDockNotify).OnLoaded(this);
                        }
                    }
                }
            }
        }

        public IEnumerable<IDockContent> Documents => this._docs;

        TitleBar topbar, bottombar;

        public Point Location
        {
            get
            {
                var b = this.DockPanel.GetChildBounds(this);
                return b.Location;//todo this.bounds??
            }
        }

        public Size WidgetSize { get; internal set; }

        public Size MinimumSize { get; private set; }

        public Size MaximumSize { get; private set; }

        public Canvas Widget => this;

        public Rectangle DocumentRectangle
        {
            get
            {
                var r = new Rectangle(Point.Zero, WidgetSize);

                if (this.topbar.Visible)
                {
                    r = new Rectangle(r.Left, r.Top + TitleBar.TitleBarHeight, r.Width, r.Height - TitleBar.TitleBarHeight);
                }
                if (this.bottombar.Visible)
                {
                    r = new Rectangle(r.Left, r.Top, r.Width, r.Height - TitleBar.TitleBarHeight);
                }
                if (r.Width < 0 || r.Height < 0) return Rectangle.Zero;
                return r;
            }
        }
        internal DockPane(DockPanel dockPanel, IDockContent[] docs)
        {
            this.MinWidth = this.MinHeight = 0;
            this.Margin = 0;
            this.DockPanel = dockPanel;
            this.BackgroundColor = DockPanel.MDIColor;
            this.DockPanel.ActiveContentChanged += DockPanel_ActiveContentChanged;
            //   base.BackgroundColor = Colors.Yellow;

            this.topbar = TitleBar.CreateHeader(this);
            this.AddChild(this.topbar);
            this.bottombar = TitleBar.CreateTabs(this);
            this.AddChild(this.bottombar);

            //  this._docs.AddRange(testdoc);


            //            this.Document = this._docs.FirstOrDefault();
            //          this.DockPanel.SetActive(this.Document ?? this.DockPanel.ActiveDocument ?? this.DockPanel.DefaultDocument);
            //  this.ActiveDocChanged(); 

            //    this.DockPanel.AddChild(this);

            SetOwner(docs);

            this._docs.AddRange(docs);
           // GetSize(true);
        }
        internal void MoveWindows()
        {
            if (this.WidgetSize.Width > 0 && this.WidgetSize.Height > 0)
            {
                if (this.topbar.Visible)
                {
                    this.SetChildBounds(this.topbar, new Rectangle(0, 0, this.WidgetSize.Width, TitleBar.TitleBarHeight));
                    this.topbar.CheckBounds();
                }
                if (this.bottombar.Visible)
                {
                    this.SetChildBounds(this.bottombar, new Rectangle(0, this.WidgetSize.Height - TitleBar.TitleBarHeight, this.WidgetSize.Width, TitleBar.TitleBarHeight));
                    this.bottombar.CheckBounds();
                }
                //  base.Bounds = new Rectangle(pos, size);
                if (_activedoc != null)
                {
                    this.SetChildBounds(this._activedoc.Widget, DocumentRectangle);
                }
            }
        }
        public void OnHidden()
        {
            this.DockPanel.ActiveContentChanged -= DockPanel_ActiveContentChanged;
        }
        protected override void Dispose(bool disposing)
        {
          //  this.Document?.Dispose();
            //   this.Document = null;

            base.Dispose(disposing);
        }
        private void DockPanel_ActiveContentChanged(object sender, EventArgs e)
        {
            this.topbar.SetDocuments(this._docs);
            this.bottombar.SetDocuments(this._docs);
         //   this.bottombar.Update();
        }

        public void Add(IDockContent[] docs)
        {
            SetOwner(docs);
            this._docs.AddRange(docs);

            if (this.Document == null)
            {
                this.Document = docs.FirstOrDefault();
            }
            this.DockPanel.SetActive(this.DockPanel.ActiveDocument ?? this.Document ?? this.DockPanel.DefaultDocument);
            this.topbar.SetDocuments(this._docs);
            this.bottombar.SetDocuments(this._docs);
        }
        public bool Remove(IDockContent[] docs)
        {
            IDockDocument activedoc = this.DockPanel.ActiveDocument;
            if (docs.Any(_d => object.ReferenceEquals(_d, this.Document)))
            {
                this.Document = null;
            }
            if (docs.Any(_d => object.ReferenceEquals(_d, activedoc)))
            {
                activedoc = null;
            }
            ClrOwnwer(docs);
            foreach (var doc in docs)
            {
                this._docs.Remove(doc);
            }
            if (this.Document == null)
            {
                this.Document = this._docs.FirstOrDefault();
            }
            this.DockPanel.SetActive(activedoc ?? this.Document ?? this.DockPanel.DefaultDocument);
            this.topbar.SetDocuments(this._docs);
            this.bottombar.SetDocuments(this._docs);

            return !this.Documents.Any();
        }

        internal void CloseDocument(IDockContent doc)
        {
            this.DockPanel.CloseDocument(this, doc);
        }

        private void SetOwner(IEnumerable<IDockContent> docs)
        {
            foreach (var doc in docs)
            {
                doc.DockPane = this;
            }
        }
        private void ClrOwnwer(IEnumerable<IDockContent> docs)
        {
            foreach (var doc in docs)
            {
                doc.DockPane = null;
            }
        }

        public void Layout(Point pt, Size size)
        {
            this.WidgetSize = size;
            this.DockPanel.SetChildBounds(this, new Rectangle(pt, size));

            MoveWindows();
        }
        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            return new Size(widthConstraint.AvailableSize, heightConstraint.AvailableSize);
        }

        public void AddWidget()
        {
            this.DockPanel.AddChild(this);
            if (this.Document == null)
            {
                this.Document = _docs.FirstOrDefault();
            }
            else
            {
                this.AddChild(this.Document.Widget);
            }
            this.DockPanel.SetActive(this.DockPanel.ActiveDocument ?? this.Document ?? this.DockPanel.DefaultDocument);
            this.topbar.SetDocuments(this._docs);
            this.bottombar.SetDocuments(this._docs);
            MoveWindows();
        }
        public void RemoveWidget()
        {
            if (this.Parent != null)
            {
                if (this.Document?.Widget.Parent != null)
                {
                    this.RemoveChild(this.Document.Widget);
                }
                this.DockPanel.RemoveChild(this);
            }
        }
        void IDockNotify.OnLoaded(IDockPane pane)
        {
            (this._activedoc as IDockNotify)?.OnLoaded(this);
        }

        void IDockNotify.OnUnloading()
        {
            (this._activedoc as IDockNotify)?.OnUnloading();
        }

        public void GetSize(bool setsize)
        {
            double miw = 32, mih = 0;

            if (_docs.Any())
            {
                foreach (var doc in this._docs)
                {
                    miw = Math.Max(miw, doc.Widget.MinWidth);
                    mih = Math.Max(miw, doc.Widget.MinHeight);
                }
            }
            bool v1 = this.topbar.Visible;
            bool v2 = this.bottombar.Visible;

            this.MinimumSize = new Size(miw, mih + (v1 ? TitleBar.TitleBarHeight : 0) + (v2 ? TitleBar.TitleBarHeight : 0));
            //   (this as Canvas).MinWidth = miw; // fails with WPF
            //   (this as Canvas).MinHeight =mih;

            if (setsize)
            {
                this.WidgetSize = this.MinimumSize;
            }
            else
            {
                this.WidgetSize = new Size(Math.Max(this.WidgetSize.Width, this.MinimumSize.Width), Math.Max(this.WidgetSize.Height, this.MinimumSize.Height));
            }
        }

        public bool HitTest(Point position, out IDockSplitter splitter, out int ind)
        {
            splitter = null;
            ind = -1;


            if (position.X >= this.Location.X && position.X < this.Location.X + this.WidgetSize.Width &&
                position.Y >= this.Location.Y && position.Y < this.Location.Y + this.WidgetSize.Height)
            {
                return true;
            }
            return false;
        }

        internal void SetActive(IDockContent value)
        {
            this.Document = value;
            this.DockPanel.SetActive(value);
        }
      /*  public void ActiveDocChanged()
        {
            this.topbar.SetDocuments(this._docs);
            this.bottombar.Update();
        }*/

        int wh = 16;

        public void SetDrop(DockPosition? hit)
        {
            var r = new Rectangle(
                0,
                this.topbar.Visible ? TitleBar.TitleBarHeight : 0,
                this.Bounds.Width,
                this.Bounds.Height - (this.topbar.Visible ?TitleBar.TitleBarHeight : 0) - (this.bottombar.Visible ? TitleBar.TitleBarHeight : 0));

            if ((this.Document as IDockCustomize)?.HideWhenDocking ?? false)
            {
                this.Document?.Widget.Hide();
            }
            AddDrop((r.Width - wh) / 2, r.Top, DockPosition.Top);
            AddDrop((r.Width - wh) / 2, r.Top + r.Height - wh, DockPosition.Bottom);
            AddDrop(0, r.Top + (r.Height - wh) / 2, DockPosition.Left);
            AddDrop(r.Right - wh, r.Top + (r.Height - wh) / 2, DockPosition.Right);
            AddDrop((r.Width - wh) / 2, r.Top + (r.Height - wh) / 2, DockPosition.Center);

            Console.WriteLine($"added drops");
        }

        private void AddDrop(double x, double y, DockPosition pos)
        {
            var widget = new DropTarget(this, pos);
            this.AddChild(widget);
            this.SetChildBounds(widget, new Rectangle(x, y, wh, wh));
        }

        public void ClearDrop()
        {
            var toremove = this.Children.OfType<DropTarget>().ToArray();

            foreach (var dt in toremove)
            {
                this.RemoveChild(dt);
                dt.Dispose();
            }
            this.Document?.Widget.Show();
            Console.WriteLine($"cleared drops");
        }

        public DockPosition? HitTest(Point position)
        {
            foreach (var ctl in this.Children.OfType<DropTarget>().Reverse())
            {
                if (this.GetChildBounds(ctl).Contains(position))
                {
                    return ctl.pos;
                }
            }
            return null;
        }

        public void Update(DockPosition? hit)
        {
            foreach (var ctl2 in this.Children.OfType<DropTarget>())
            {
                ctl2.SetHighLight(hit.HasValue&&ctl2.pos == hit.Value);
            }
        }

        public void NewDockPanel(DockPanel dockpanel)
        {
            this.DockPanel.ActiveContentChanged -= DockPanel_ActiveContentChanged;
            this.DockPanel = dockpanel;
            this.SetOwner(this._docs);
            this.DockPanel.ActiveContentChanged += DockPanel_ActiveContentChanged;
        }
    }
}