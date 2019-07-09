using System;
using System.Collections.Generic;
using System.Linq;
using Xwt;
using Xwt.Drawing;

namespace BaseLib.Xwt.Controls.DockPanel.Internals
{
    partial class TitleBar
    { 
        public static Font SmallFont { get; private set; }

        class ScrollButtons : Canvas // dropdown for multiple documents
        {
            int dx = 3;
            private TitleBar titleBar;
            private Label buttonpopup;
            private Menu documentslistwindow;

            public static MenuItem NewMenuItem(string text, EventHandler click)
            {
                var r = new MenuItem(text);
                r.Clicked += click;
                return r;
            }
            public static Button NewButton(string text, EventHandler click)
            {
                var r = new Button(text);
                r.Clicked += click;
                return r;
            }
            public ScrollButtons(TitleBar titleBar)
            {
                this.Margin = 0;
                this.titleBar = titleBar;
                this.buttonpopup = new Label() { Text = "*", BackgroundColor = DockPanel.ToolbarInactiveColor };
                this.buttonpopup.ButtonPressed += showmenuclick;
                base.AddChild(buttonpopup);
                this.ButtonSize = new TextLayout(this) { Text = this.buttonpopup.Text }.GetSize();
                base.SetChildBounds(buttonpopup, new Rectangle(new Point(dx, 0), ButtonSize));
                this.ClipToBounds();
               
                this.documentslistwindow = new Menu();
            }
            private void showmenuclick(object sender, ButtonEventArgs a)
            {
                if (a.Button == PointerButton.Left)
                {
                    this.documentslistwindow.Popup(this, 0, 0);
                }
            }
            public void SetDocuments(IDockContent[] docs)
            {
                this.documentslistwindow.Items.Clear();
                foreach (var doc in docs)
                {
                    var mi = new CheckBoxMenuItem(doc.TabText) { Checked = object.ReferenceEquals(this.titleBar.pane.Document, doc), Tag = doc };
                    mi.Clicked += Mi_Clicked;
                    this.documentslistwindow.Items.Add(mi);
                }
            }

            private void Mi_Clicked(object sender, EventArgs e)
            {
                this.titleBar.pane.SetActive((sender as MenuItem).Tag as IDockContent);
            }

            protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
            {
                return new Size(Width, ButtonSize.Height);
            }
            public double Width => ButtonSize.Width + dx * 2;
            public Size ButtonSize { get; }
            public Size WindowSize => new Size(Width, ButtonSize.Height);
        }
        class Buttons : Canvas // document buttons
        {
            internal class DockButton : Label // document button
            {
                private Buttons buttons;
                internal IDockContent doc;
                private bool captured = false;
                private Point dragpt;
                private Label closebutton;

                public bool Active
                {
                    set
                    {
                        if (value)
                        {
                            this.BackgroundColor = (this.doc is IDockDocument) ? DockPanel.DocumentActiveColor : DockPanel.ToolbarActiveColor;
                        }
                        else
                        {
                            this.BackgroundColor = (this.doc is IDockDocument) ? DockPanel.DocumentInactiveColor : DockPanel.ToolbarInactiveColor;
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
                    //this.VerticalPlacement = this.HorizontalPlacement = WidgetPlacement.Fill;

                    var size = new TextLayout() { Font = TitleBar.SmallFont, Text = "X" }.GetSize();
                    this.closesize = new Size(size.Width + 2, size.Height + 2);

                    this.Update();
                }
                internal void Update()
                {
                    this.Text = doc.TabText;

                    if (doc is IDockDocument) // highlight active document
                    {
                        this.Active = object.ReferenceEquals(doc, buttons.titlebar.pane.DockPanel.ActiveDocument);
                    }
                    else if (object.ReferenceEquals(doc, buttons.titlebar.pane.Document)) // active in pane?
                    {
                        this.Active = true;
                    }
                    else
                    {
                        this.Active = false;
                    }
                }

                private void Closebutton_ButtonPressed(object sender, ButtonEventArgs e)
                {
                    var pt = new Point(e.X + CloseRectangle.Left, e.Y + CloseRectangle.Top);
                    var ee = new ButtonEventArgs() { Button = e.Button, IsContextMenuTrigger = e.IsContextMenuTrigger, MultiplePress = e.MultiplePress, X = pt.X, Y = pt.Y };
                    OnButtonPressed(ee);
                    e.Handled = ee.Handled;
                }
                protected override void OnButtonPressed(ButtonEventArgs args)
                {
                    args.Handled = true;
                    if (args.Button == PointerButton.Left)
                    {
                        if (this.closebutton != null && this.CloseRectangle.Contains(args.Position))
                        {
                            this.dragpt = args.Position;
                            this.captured = true;
                            this.buttons.titlebar.pane.DockPanel.xwt.SetCapture(this);
                            return;
                        }
                        var opos = this.buttons.GetChildBounds(this);
                        this.buttons.titlebar.pane.SetActive(this.doc);

                        if (opos.Equals(this.buttons.GetChildBounds(this)))
                        {
                            //this.buttons.Active = this.doc;
                            this.captured = true;
                            this.dragpt = args.Position;
                            this.buttons.titlebar.pane.DockPanel.xwt.SetCapture(this);
                        }
                        return;
                    }
                    else if (captured)
                    {
                        if (this.CloseRectangle.Contains(this.dragpt))
                        {
                            ClrCapture();
                            Closebutton_Clicked(null, null);
                        }
                        else
                        {
                            ClrCapture();
                        }
                    }
                    //                    base.OnButtonPressed(args);
                }
                private void Closebutton_MouseMoved(object sender, MouseMovedEventArgs e)
                {
                    var pt = new Point(e.X + this.CloseRectangle.Left, e.Y + this.CloseRectangle.Top);
                    var ee = new MouseMovedEventArgs(0, pt.X, pt.Y);
                    this.OnMouseMoved(ee);
                    e.Handled = ee.Handled;
                }
                protected override void OnMouseMoved(MouseMovedEventArgs args)
                {
                    args.Handled = true;
                    if (captured)
                    {
                        args.Handled = true;
                        if (this.closebutton != null && this.CloseRectangle.Contains(this.dragpt))
                        {
                            if (!this.CloseRectangle.Contains(args.X, args.Y))
                            {
                                // pressed & outside
                                this.closebutton.BackgroundColor = DockPanel.ButtonHighlightDim;
                            }
                            else
                            {
                                // pressed & inside
                                this.closebutton.BackgroundColor = DockPanel.ButtonHighlight;
                            }
                        }
                        else if (!DockPanel.DragRectangle.Contains(args.X - this.dragpt.X, args.Y - this.dragpt.Y))
                        {
                            ClrCapture();

                            // start drag

                            var pt = this.ConvertToScreenCoordinates(args.Position);

                            DockItDragDrop.StartDrag(this.buttons.titlebar.pane, new IDockContent[] { this.doc }, pt);
                        }
                        return;
                    }
                    else
                    {
                        if (((this.doc as IDockCustomize)?.CanClose ?? true) && this.CloseRectangle.Contains(args.Position))
                        {
                            if (closebutton == null)
                            {
                                this.closebutton = new Label("X")
                                {
                                    Font = TitleBar.SmallFont,
                                    Visible = false,
                                    BackgroundColor = DockPanel.ButtonHighlight,
                                    
                                };
                                this.closebutton.ButtonPressed += Closebutton_ButtonPressed;
                                this.closebutton.MouseMoved += Closebutton_MouseMoved;
                                this.closebutton.ButtonReleased += Closebutton_ButtonReleased;
                                this.closebutton.MouseExited += Closebutton_MouseExited;

                                this.buttons.AddChild(closebutton);
                                var b = this.buttons.GetChildBounds(this);
                                this.buttons.SetChildBounds(closebutton, new Rectangle(CloseRectangle.Location.X + b.Location.X, CloseRectangle.Location.Y + b.Location.Y, CloseRectangle.Width, CloseRectangle.Height));
                                this.closebutton.Visible = true;
                            }
                            args.Handled = true;
                        }
                        else
                        {
                            RemoveCloseButton();
                        }
                    }
                    if (!args.Handled)
                    {
                        base.OnMouseMoved(args);
                    }
                }
                protected void Closebutton_MouseExited(object sender, EventArgs args)
                {
                    if (!this.captured)
                    {
                        this.RemoveCloseButton();
                    }
                }
                private void Closebutton_Clicked(object sender, EventArgs e)
                {
                    this.buttons.titlebar.pane.CloseDocument(this.doc);
                }

                internal void RemoveCloseButton()
                {
                    if (this.closebutton != null)
                    {
                        this.buttons.RemoveChild(this.closebutton);
                        this.closebutton.Dispose();
                        this.closebutton = null;
                    }
                }
                private void Closebutton_ButtonReleased(object sender, ButtonEventArgs e)
                {
                    var pt = new Point(e.X + this.CloseRectangle.Left, e.Y + this.CloseRectangle.Top);
                    var ee = new ButtonEventArgs() { Button = e.Button, MultiplePress = e.MultiplePress, IsContextMenuTrigger = e.IsContextMenuTrigger, X = pt.X, Y = pt.Y };
                    this.OnButtonReleased(ee);
                    e.Handled = ee.Handled;
                }

                protected override void OnButtonReleased(ButtonEventArgs args)
                {
                    args.Handled = true;

                    if (captured)
                    {
                        var pt = args.Position;
                        ClrCapture();

                        if (this.closebutton != null && this.CloseRectangle.Contains(pt))
                        {
                            if (this.CloseRectangle.Contains(pt))
                            {
                                Closebutton_Clicked(null, null);
                            }
                        }
                    }
                    //     base.OnButtonReleased(args);
                }
                private void ClrCapture()
                {
                    if (captured)
                    {
                        captured = false;
                        this.buttons.titlebar.pane.DockPanel.xwt.ReleaseCapture(this);
                    }
                }
                private Size closesize { get; set; }
                private Rectangle CloseRectangle
                {
                    get
                    {
                        var s = this.buttons.GetChildBounds(this).Size;
                    //    Size s = this.Surface.GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);

                        return new Rectangle(s.Width - closesize.Width, 0, closesize.Width, closesize.Height);
                    }
                }
            }
            double scrollpos = 0;
            private bool captured = false;
            private Point orgpt;
            private IDockContent[] docs;
            private readonly TitleBar titlebar;

            public Buttons(TitleBar titlebar)
            {
                this.titlebar = titlebar;
                this.Margin = 0;
                this.VerticalPlacement = this.HorizontalPlacement = WidgetPlacement.Fill;

                this.Cursor = CursorType.Arrow;

                this.ClipToBounds();
            }
            protected override void OnBoundsChanged()
            {
                base.OnBoundsChanged();

                if (this.docs != null)
                {
                    this.SetDocuments(this.docs);
                }
            }
            public void SetDocuments(IDockContent[] docs)
            {
                this.docs = docs;

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
                    this.SetChildBounds(o, new Rectangle(scrollpos + x, (this.Bounds.Height-size.Height)/2, w, size.Height));
                    x += w + TitleBarButtonSpacing;
                }
                var tr = current.Where(_b => !docs.Any(_d => object.ReferenceEquals(_b.doc, _d))).ToArray();

                foreach (var o in tr)
                {
                    o.RemoveCloseButton();
                    this.RemoveChild(o);
                    o.Dispose();
                }
                this.QueueDraw();
            }
            public double Width => this.Children.OfType<DockButton>().Select(_b => _b.Size.Width).Sum() + (this.Children.Count() - 1) * TitleBarButtonSpacing;

            /* public IDockContent Active
             {
                 get => this.titlebar.pane.Document;
                 set => this.titlebar.pane.SetActive(value);
             }
             */
            /*    internal void Update()
                {
                    double x = 0;
                    foreach (var b in this.Children.OfType<Buttons.DockButton>())
                    {
                        b.Update();
                        var size = b.GetBackend().GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);
                        var w = Math.Max(size.Width, 10);
                        this.SetChildBounds(b, new Rectangle(scrollpos + x, 2, w, 18));
                        x += w + TitleBarButtonSpacing;
                    }
                    this.QueueDraw();
                }*/

            protected override void OnButtonPressed(ButtonEventArgs args)
            {
                args.Handled = true;
                if (!this.titlebar.IsInfo && this.titlebar.IsHeader)
                {
                    if (args.Button == PointerButton.Left)
                    {
                        this.captured = true;
                        this.orgpt = args.Position;
                        this.titlebar.pane.DockPanel.xwt.SetCapture(this);
                    }
                }
            //    base.OnButtonPressed(args); 
            }
            protected override void OnButtonReleased(ButtonEventArgs args)
            {
                args.Handled = true;
                if (captured)
                {
                    ClrCapture();
                }
            //    base.OnButtonReleased(args);
            }
            protected override void OnMouseMoved(MouseMovedEventArgs args)
            {
                args.Handled = true;
                if (captured)
                {
                    if (!DockPanel.DragRectangle.Contains(args.X - this.orgpt.X, args.Y - orgpt.Y))
                    {
                        ClrCapture();

                        var docs = this.titlebar.pane.Documents.ToArray();

                        DockItDragDrop.StartDrag(this.titlebar.pane, docs, ConvertToScreenCoordinates(args.Position));
                        return;
                    }
                }
             //   base.OnMouseMoved(args);
            }

            private void ClrCapture()
            { 
                this.titlebar.pane.DockPanel.xwt.ReleaseCapture(this);
                this.captured = false;
            }
        }
    }
}
