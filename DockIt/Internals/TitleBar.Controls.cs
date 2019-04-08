using BaseLib.XwtPlatForm;
using System;
using System.Collections.Generic;
using System.Linq;
using Xwt;
using Xwt.Drawing;

namespace BaseLib.DockIt_Xwt
{
    partial class TitleBar 
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
                this.ClipToBounds();
            }
            public void SetDocuments(IDockContent[] docs)
            {
                var m = new Menu();

                foreach(var doc in docs)
                {
                    var mi = new CheckBoxMenuItem(doc.TabText) { Checked = object.ReferenceEquals(this.titleBar.pane.Document, doc), Tag = doc };
                    mi.Clicked += Mi_Clicked;
                    m.Items.Add(mi);
                }
                this.menu.Menu = m;
            }

            private void Mi_Clicked(object sender, EventArgs e)
            {
                this.titleBar.pane.SetActive((sender as MenuItem).Tag as IDockContent);
            }

            protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
            {
                return menu.GetBackend().GetPreferredSize(widthConstraint, heightConstraint);
            }
            public double Width => this.Children.Select(_b => _b.Size.Width).Sum();
        }
        class Buttons : Canvas // document buttons
        {
            internal class DockButton : Label // document button
            {
                private Buttons buttons;
                internal IDockContent doc;
                private bool captured = false;
                private Point dragpt;
                private Button closebutton;
                private Font closebuttonfont;

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
                    this.closebuttonfont = Font.WithSize(closesize.Height - 6);
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

                protected override void OnButtonPressed(ButtonEventArgs args)
                {
                    args.Handled = true;
                    if (args.Button == PointerButton.Left)
                    {
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
                        ClrCapture();
                    }
                    //                    base.OnButtonPressed(args);
                }
                protected override void OnMouseMoved(MouseMovedEventArgs args)
                {
                    args.Handled = true;
                    if (captured)
                    {
                        args.Handled = true;
                        if (!DockPanel.DragRectangle.Contains(args.X - this.dragpt.X, args.Y - this.dragpt.Y))
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
                                closebutton = new Button("X")
                                {
                                    WidthRequest = this.closesize.Width,
                                    HeightRequest = this.closesize.Height,
                                    Font= this.closebuttonfont
                                };
                                closebutton.Clicked += Closebutton_Clicked;
                                closebutton.MouseExited += Closebutton_MouseExited;

                                this.buttons.AddChild(closebutton);
                                var b = this.buttons.GetChildBounds(this);
                                this.buttons.SetChildBounds(closebutton, new Rectangle(CloseRectangle.Location.X + b.Location.X, CloseRectangle.Location.Y + b.Location.Y, closesize.Width, closesize.Height));
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

                private void Closebutton_MouseExited(object sender, EventArgs e)
                {
                    this.RemoveCloseButton();
                }
                
                private void Closebutton_Clicked(object sender, EventArgs e)
                {
                    this.buttons.titlebar.pane.CloseDocument(this.doc);
                }

                internal void RemoveCloseButton()
                {
                    if (closebutton != null)
                    {
                        this.buttons.RemoveChild(closebutton);
                        this.closebutton.Dispose();
                        this.closebutton = null;
                    }
                }

                protected override void OnButtonReleased(ButtonEventArgs args)
                {
                    args.Handled = true;
                    if (captured)
                    {
                        ClrCapture();
                    }
                    //     base.OnButtonReleased(args);
                }
                private void ClrCapture()
                {
                    captured = false;
                    this.buttons.titlebar.pane.DockPanel.xwt.ReleaseCapture(this);
                }
                private Size closesize => new Size(14, 14);
                private Rectangle CloseRectangle
                {
                    get
                    {
                        Size s = this.GetBackend().GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);

                        return new Rectangle(s.Width - closesize.Width - 2, 2, closesize.Width, closesize.Height);
                    }
                }
            }
            double scrollpos = 0;
            private bool captured = false;
            private Point orgpt;
            private readonly TitleBar titlebar;

            public Buttons(TitleBar titlebar)
            {
                this.titlebar = titlebar;
                this.Margin = 0;
                this.VerticalPlacement = this.HorizontalPlacement = WidgetPlacement.Fill;

                this.Cursor = CursorType.Arrow;

                this.ClipToBounds();
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
