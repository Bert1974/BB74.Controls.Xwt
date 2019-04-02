using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.XwtPlatForm;
using Xwt;
using Xwt.Drawing;

namespace BaseLib.DockIt_Xwt
{
    class TitleBar : Canvas
    {
        class ScrollButtons : Canvas // dropdown for multiple documents
        {
            private TitleBar titleBar;
            private MenuButton menu;

            public ScrollButtons(TitleBar titleBar)
            {
                this.Margin = 0;
                this.VerticalPlacement = this.HorizontalPlacement = WidgetPlacement.Fill;
                this.titleBar = titleBar;
                this.menu = new MenuButton(".");
                base.AddChild(menu);
                var size = menu.GetBackend().GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);
                base.SetChildBounds(menu, new Rectangle(Point.Zero, size));
            }

            public void SetDocuments(IDockContent[] docs)
            {
                var m = new Menu();

                foreach(var doc in docs)
                {
                    m.Items.Add(new MenuItem(doc.TabText));
                }
                this.menu.Menu = m;
            }
            protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
            {
                return menu.GetBackend().GetPreferredSize(widthConstraint, heightConstraint);
            }
            public double Width => this.Children.Select(_b => _b.GetBackend().GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained).Width).Sum();
        }
        class Buttons : Canvas // document buttons
        {
            internal class DockButton : Label // document button
            {
                private Buttons buttons;
                internal IDockContent doc;
                private bool captured;
                private Point dragpt;

                public bool Active
                {
                    set
                    {
                        if (value)
                        {
                            this.BackgroundColor = Colors.Red;
                        }
                        else
                        {
                            this.BackgroundColor = Colors.Transparent;
                        }
                    }
                }

                public DockButton(Buttons buttons, IDockContent doc)
                    : base()
                {
                    this.buttons = buttons;
                    this.doc = doc;
                    this.Text = doc.TabText;

                    this.Margin = 0;
                    this.VerticalPlacement = this.HorizontalPlacement = WidgetPlacement.Fill;

                    this.Update();
                }

                internal void Update()
                {
                    this.Text = doc.TabText;
  
                    if (object.ReferenceEquals(doc, buttons.titlebar.pane.Document))
                    {
                        if (doc is IDockToolbar)
                        {
                            this.Active = true;
                        }
                        else
                        {
                            this.Active = object.ReferenceEquals(doc, buttons.titlebar.pane.DockPanel.ActiveDocument);
                        }
                    }
                    else
                    {
                        this.Active = false;
                    }
                }

                protected override void OnButtonPressed(ButtonEventArgs args)
                {
                    if (args.Button == PointerButton.Left)
                    {
                        this.buttons.titlebar.pane.SetActive(this.doc);
                        //this.buttons.Active = this.doc;
                        this.captured = true;
                        this.dragpt = args.Position;
                       this.buttons.titlebar.pane.DockPanel.xwt.SetCapture(this);
                        return;
                    }
                    base.OnButtonPressed(args);
                }
                protected override void OnMouseMoved(MouseMovedEventArgs args)
                {
                    if (captured)
                    {
                        int dd = 3;
                        if (args.X < this.dragpt.X - dd || args.X >= this.dragpt.X + dd || args.Y < this.dragpt.Y - dd || args.Y >= this.dragpt.Y + dd)
                        {
                            this.captured = false;

                            this.buttons.titlebar.pane.DockPanel.xwt.ReleaseCapture(this);

                            // start drag

                            BaseLib.DockIt_Xwt.DragDrop.StartDrag(this.buttons.titlebar.pane, new IDockContent[] { this.doc }, this.ConvertToScreenCoordinates(args.Position));
                            return;
                        }
                    }
                    base.OnMouseMoved(args);
                }
                protected override void OnButtonReleased(ButtonEventArgs args)
                {
                    if (captured)
                    {
                        this.buttons.titlebar.pane.DockPanel.xwt.ReleaseCapture(this);
                        captured = false;
                    }
                    base.OnButtonReleased(args);
                }
            }
            double scrollpos = 0;
            private readonly TitleBar titlebar;

            public Buttons(TitleBar titlebar)
            {
                this.titlebar = titlebar;
                this.Margin = 0;
                this.VerticalPlacement = this.HorizontalPlacement = WidgetPlacement.Fill;

                var nativectl= this.GetBackend().NativeWidget;

                Type t = PlatForm.GetType("System.Windows.Controls.Panel");

                if (nativectl.GetType().IsDerived(t))
                {
                    nativectl.GetType().SetPropertyValue(nativectl, "ClipToBounds", true);
                }
            }
            public void SetDocuments(IDockContent[] docs)
            {
                var current = this.Children.OfType<DockButton>().ToArray();

                //   var tl = new TextLayout(this);

                double x = 0;
                foreach (var doc in docs)
                {
                    var o = current.FirstOrDefault(_b => object.ReferenceEquals(_b.doc, doc));

                    if (o == null)
                    {
                        var b = new DockButton(this, doc)
                        {
                        };
                        this.AddChild(b, x, 0);

                        o = b;
                    }
                    o.Update();
                    var ox = x;
                    var size = o.GetBackend().GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);
                    var w = Math.Max(size.Width, 10);
                    this.SetChildBounds(o, new Rectangle(scrollpos + x, 2, w, 18));
                    x += w + 8;
                }
                var tr = current.Where(_b => !docs.Any(_d => object.ReferenceEquals(_b.doc, _d))).ToArray();

                foreach (var o in tr)
                {
                    this.RemoveChild(o);
                    o.Dispose();
                }
                this.QueueDraw();
            }
            public double Width => this.Children.OfType<DockButton>().Select(_b => _b.GetBackend().GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained).Width).Sum();

           /* public IDockContent Active
            {
                get => this.titlebar.pane.Document;
                set => this.titlebar.pane.SetActive(value);
            }
            */
            internal void Update()
            {
                double x = 0;
                foreach (var b in this.Children.OfType<Buttons.DockButton>())
                {
                    b.Update();
                    var size = b.GetBackend().GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);
                    var w = Math.Max(size.Width, 10);
                    this.SetChildBounds(b, new Rectangle(scrollpos + x, 2, w, 18));
                    x += w + 8;
                }
                this.QueueDraw();
            }
        }
 
        public static TitleBar CreateHeader(DockPane dockPane)
        {
            return new TitleBar(dockPane, true);
        }

        public static TitleBar CreateTabs(DockPane dockPane)
        {
            return new TitleBar(dockPane, false);
        }


        internal void Update()
        {
            this.buttons.Update();
        }
        private IEnumerable<IDockContent> docsvis
        {
            get
            {
                if (!IsInfo && IsHeader)
                {
                    if (this.pane.Document is IDockToolbar)
                    {
                        yield return this.pane.Document;
                    }
                    foreach (var doc in this.pane.Documents.OfType<IDockDocument>())
                    {
                        yield return doc;
                    }
                }
                else
                {
                    foreach (var doc in this.pane.Documents.OfType<IDockToolbar>())
                    {
                        yield return doc;
                    }
                }
            }
        }
        private DockPane pane;
        private bool IsInfo = false, IsHeader;
        private Buttons buttons;
        private ScrollButtons scrollwindow;
        private IDockContent[] docs=new IDockContent[0];

        private TitleBar(DockPane pane, bool isheader)
        {
            this.IsHeader = isheader;
            this.pane = pane;
            this.MinHeight = HeightRequest = 22;
            this.BackgroundColor = DockPanel.TitlebarColor;

            this.buttons = new Buttons(this);
    //        this.buttons.ExpandHorizontal = false;
      //      this.buttons.ExpandVertical = true;
            this.AddChild(this.buttons);

            this.scrollwindow = new ScrollButtons(this);
            this.AddChild(this.scrollwindow);
        }
        protected override void OnBoundsChanged()
        {
            base.OnBoundsChanged();
            CheckBounds();
        }
        internal void CheckBounds()
        { 
            var buttonsize = this.buttons.Width;// GetBackend().GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);
            var scrollsize = this.scrollwindow.Width;

            bool overflow = buttonsize > this.Bounds.Width;

            if (overflow)
            {
                double x = this.Bounds.Width - scrollsize;

                if (x < 0) // only dropdown menu
                {
                    this.buttons.Visible = false;
                    this.SetChildBounds(this.scrollwindow, new Rectangle(Point.Zero, this.Bounds.Size));
                    this.scrollwindow.Visible = true;
                }
                else // buttons and menu
                {
                    this.SetChildBounds(this.buttons, new Rectangle(Point.Zero, new Size(x, 22)));
                    this.SetChildBounds(this.scrollwindow, new Rectangle(new Point(x, 0), new Size(scrollsize, 22)));
                    this.buttons.Visible = true;
                    this.scrollwindow.Visible = true;
                }
            }
            else // buttons only
            {
                this.scrollwindow.Visible = false;
                this.SetChildBounds(this.buttons, new Rectangle(Point.Zero,this.Bounds.Size));
                this.buttons.Visible = true;
            }
        }
        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);

            ctx.SetColor(this.BackgroundColor);
            ctx.Rectangle(this.Bounds);
            ctx.Fill();

      //      ctx.DrawTextLayout(new TextLayout(this) { Text = pane.Document?.TabText ?? "-" }, 0, 0);
        }

        internal void SetDocuments(List<IDockContent> docs)
        {
         //   this.QueueDraw();

            this.docs = docs.ToArray();
            this.scrollwindow.SetDocuments(this.docs);
            this.buttons.SetDocuments(this.docsvis.ToArray());

            if (this.Visible != this.docsvis.Any())
            {
                this.Visible = this.docsvis.Any();
                this.pane.MoveWindows();
            }
        }
    }
}
