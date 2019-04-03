using BaseLib.DockIt_Xwt.Interop;
using BaseLib.XwtPlatForm;
using System;
using Xwt;
using Xwt.Backends;
using Xwt.Drawing;

namespace BaseLib.DockIt_Xwt
{
    static class DockItDragDrop
    {
        #region abstract class DragWindow
        internal abstract class DragWindow : Window
        {
            #region MyCancas
            protected class MyCanvas : Xwt.Canvas
            {
                private readonly DragWindow owner;
                private readonly bool checkmouse;

                public MyCanvas(DragWindow dragWindow, bool checkmouse)
                {
                    this.owner = dragWindow;
                    this.checkmouse = checkmouse;

                    this.Margin = 0;
                    this.MinWidth = this.MinHeight = 10;
                    this.ExpandHorizontal = true;
                    this.ExpandVertical = true;
                    this.CanGetFocus = true;
                    this.BackgroundColor = Colors.LightYellow;

                    /*    this.ButtonPressed += (s, e) => { };
                        this.ButtonReleased += (s, e) => { };
                        this.MouseMoved += (s, e) => { };*/
                }
                protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
                {
                    return new Size(32, 32);// base.OnGetPreferredSize(widthConstraint, heightConstraint);
                }
                protected override void OnDraw(Context ctx, Rectangle dirtyRect)
                {
                    base.OnDraw(ctx, dirtyRect);

                    ctx.SetColor(Colors.Black);
                    ctx.Rectangle(this.Bounds);
                    ctx.Stroke();
                }
                protected override void OnKeyPressed(KeyEventArgs args)
                {
                    if (args.Key == Key.Escape)
                    {
                        owner.doclose(false);
                        args.Handled = true;
                        return;
                    }
                    base.OnKeyPressed(args);
                }
                protected override void OnButtonPressed(ButtonEventArgs args)
                {
                    owner.doclose(false);
                    //    base.OnButtonPressed(args);
                }
                protected override void OnButtonReleased(ButtonEventArgs args)
                {
                    owner.doclose(true);
                    //     base.OnButtonReleased(args);
                }
                protected override void OnMouseMoved(MouseMovedEventArgs args)
                {
                    if (this.checkmouse)
                    {
                        var pt = this.ConvertToScreenCoordinates(args.Position);
                        owner.CheckMove(pt, true);
                    }
                }
            }
            #endregion

            public abstract void Show(out IDockPane dockpane, out DockPosition? dockat);

            internal void SetResult(out IDockPane dockpane, out DockPosition? dockat)
            {
                if (this.result)
                {
                    dockpane = this.droppane;
                    dockat = this.drophit;
                }
                else
                {
                    dockpane = null;
                    dockat = null;
                }
            }

            protected readonly IXwt xwt;
            internal bool result, doexit;
            internal DockPosition? drophit;
            internal IDockPane droppane;
            private readonly Size floatsize;

            protected DragWindow(IXwt wxt, Point position, Size size, bool checkmouse)
            {
                this.xwt = wxt;

                this.result = this.doexit = false;

                //  this.Resizable = false;
                this.Decorated = false;
                this.Location = position.Offset(-5, -5);
                this.Size = new Size(32, 32);
                this.Width = this.Height = 32;
                this.Opacity = 0.8;
                this.Padding = 0;
                this.Title = "dragform";

                this.floatsize = size;

                this.Content = new MyCanvas(this, checkmouse);
            }
            protected virtual void doclose(bool apply)
            {
                this.result = apply;
                this.doexit = true;
            }
            protected override bool OnCloseRequested()
            {
                if (!this.doexit)
                {
                    this.doclose(false);
                    return false;
                }
                return true;
            }
            protected void CheckMove(Point point, bool setpos)
            {
                DockItDragDrop.CheckMove(this, point, setpos, this.floatsize, ref this.droppane, ref this.drophit);
            }

            internal virtual void SetPosition(Rectangle rectangle)
            {
                (this.GetBackend() as IWindowFrameBackend).Bounds = rectangle;
            }
        }

        #endregion

        #region class DragWindowWpf
        class DragWindowWpf : DragWindow
        {
            public DragWindowWpf(IXwt xwt, Point position, Size size)
                : base(xwt, position, size, false)
            {
                var wpfwin = (this.GetBackend() as IWindowFrameBackend).Window;
                wpfwin.GetType().SetPropertyValue(wpfwin, "AllowsTransparency", true);

                var t = PlatForm.GetType("System.Windows.ResizeMode");
                wpfwin.GetType().SetPropertyValue(wpfwin, "ResizeMode", Enum.Parse(t, "NoResize"));
                wpfwin.GetType().SetPropertyValue(wpfwin, "MaxWidth", 32);

                base.Size = new Size(32, 32);
            }
            public override void Show(out IDockPane dockpane, out DockPosition? dockat)
            {
                this.doexit = false;

                this.Show();
                this.Content.SetFocus();

                this.xwt.SetCapture(this.Content);

                while (!this.doexit)
                {
                    var pt = new Win32.POINT();
                    Win32.GetCursorPos(ref pt);

                    this.CheckMove(pt, true);

                    this.xwt.DoEvents();
                }
                this.xwt.ReleaseCapture(this.Content);

                DockPanel.ClrHightlight();

                base.Close();

                base.SetResult(out dockpane, out dockat);
            }
            internal override void SetPosition(Rectangle rectangle)
            {
                var wpfwin = (this.GetBackend() as IWindowFrameBackend).Window;
                wpfwin.GetType().SetPropertyValue(wpfwin, "MaxWidth", rectangle.Width);
                base.SetPosition(rectangle);
            }
        }
        #endregion

        #region class DragWindowXamMac
        class DragWindowXamMac : DragWindow
        {
            public DragWindowXamMac(IXwt wxt, Point position, Size size)
                : base(wxt, position, size, false)
            {
                var backend = Toolkit.CurrentEngine.GetSafeBackend(this);
                (backend as IWindowFrameBackend).ShowInTaskbar = false;
            }
            public override void Show(out IDockPane dockpane, out DockPosition? dockat)
            {
                this.doexit = false;
                this.result = true;

                (this as Window).Show();

                this.Content.SetFocus();
                //  this.xwt.SetCapture(this.Content);

                while (!this.doexit)
                {
                    Type t = PlatForm.GetType("AppKit.NSEvent");
                    var pt = t.GetPropertyValueStatic("CurrentMouseLocation");
                    var mask = t.GetPropertyValueStatic("CurrentPressedMouseButtons");

                    var x = (int)Convert.ToDouble(pt.GetType().GetPropertyValue(pt, "X"));
                    var y = (int)Convert.ToDouble(pt.GetType().GetPropertyValue(pt, "Y"));

                    this.doexit = (Convert.ToUInt32(mask) & 1/*button1mask*/) == 0;

                    var xwtmacbackend = PlatForm.GetType("Xwt.Mac.MacDesktopBackend");
                    var cgsizetype = PlatForm.GetType("CoreGraphics.CGPoint");

                    var cgpt = Activator.CreateInstance(cgsizetype, new object[] { (double)x, (double)y });
                    var pt2 = (Xwt.Point)xwtmacbackend.InvokeStatic("ToDesktopPoint", cgpt);

                    this.CheckMove(pt2, true);

                    this.xwt.DoEvents();
                }

                //      this.xwt.ReleaseCapture(this.Content);
                DockPanel.ClrHightlight();
                this.Close();

                base.SetResult(out dockpane, out dockat);
            }
        }
        #endregion

        #region class DragWindowGTK2
        class DragWindowGTK2 : DragWindow
        {
            public DragWindowGTK2(IXwt xwt, Point position, Size size)
                : base(xwt, position, size, false)
            {
            }
            public override void Show(out IDockPane dockpane, out DockPosition? dockat)
            {
                try
                {
                    this.doexit = false;
                    this.result = true;

                    (this as Window).Show();
                    this.Content.SetFocus();

                    while (!this.doexit)
                    {
                        var gtkwin = (this.GetBackend() as IWindowFrameBackend).Window;
                        var display = gtkwin.GetType().GetPropertyValue(gtkwin, "Display");
                        var screen = display.GetType().GetPropertyValue(display, "DefaultScreen");

                        Type t = PlatForm.GetType("Gdk.ModifierType");

                        var parms = new object[] { 0, 0, Enum.ToObject(t, 0) };
                        var mi = display.GetType().GetMethod("GetPointer", new Type[] { Type.GetType("System.Int32&"), Type.GetType("System.Int32&"), PlatForm.GetType("Gdk.ModifierType&") });
                        mi.Invoke(display, parms);
                        //                        display.GetType().Invoke(display, "GetPointer", parms);
                        //   display.GetPointer(out int x, out int y, out Gdk.ModifierType mask);
                        int x = (int)parms[0];
                        int y = (int)parms[1];
                        int mask = (int)parms[2];

                        this.doexit = (mask & 0x100) == 0;

                        this.Location = new Point(x, y).Offset(-5, -5);

                        this.CheckMove(new Point(x, y), true);

                        this.xwt.DoEvents();
                    }

                    this.xwt.ReleaseCapture(this.Content);
                    DockPanel.ClrHightlight();
                    this.Close();

                    base.SetResult(out dockpane, out dockat);
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }
        #endregion

        #region class DragWindowGTK3
        class DragWindowGTK3 : DragWindow
        {
            public DragWindowGTK3(IXwt xwt, Point position, Size size)
                : base(xwt, position, size, false)
            {
                var backend = Toolkit.CurrentEngine.GetSafeBackend(this);
                (backend as IWindowFrameBackend).ShowInTaskbar = false;
            }
            public override void Show(out IDockPane dockpane, out DockPosition? dockat)
            {
                this.doexit = false;
                this.result = true;

                (this as Window).Show();

                this.Content.SetFocus();
                this.xwt.SetCapture(this.Content);

                while (!this.doexit)
                {
                    var gtkwin = (this.GetBackend() as IWindowFrameBackend).Window;
                    var display = gtkwin.GetType().GetPropertyValue(gtkwin, "Display");
                    var screen = display.GetType().GetPropertyValue(display, "DefaultScreen");

                    Type t = PlatForm.GetType("Gdk.ModifierType");

                    var parms = new object[] { 0, 0, Enum.ToObject(t, 0) };
                    var mi = display.GetType().GetMethod("GetPointer", new Type[] { Type.GetType("System.Int32&"), Type.GetType("System.Int32&"), PlatForm.GetType("Gdk.ModifierType&") });
                    mi.Invoke(display, parms);
                    //                        display.GetType().Invoke(display, "GetPointer", parms);
                    //   display.GetPointer(out int x, out int y, out Gdk.ModifierType mask);
                    int x = (int)parms[0];
                    int y = (int)parms[1];
                    int mask = (int)parms[2];


                    this.doexit = (mask & 0x100) == 0;

                    this.Location = new Point(x, y).Offset(-5, -5);
                    this.Content.SetFocus();

                    CheckMove(new Point(x, y), true);
                    this.xwt.DoEvents();


                    /*    var dp = DockPanel.GetHits(x, y);

                        if (dp.Any())
                        {
                            var rootwin = screen.GetType().GetPropertyValue(screen, "RootWindow");

                            var wins = (Array)rootwin.GetType().GetPropertyValue(rootwin, "Children");

                            var allwin = wins.OfType<object>().Where(_gdkwin => DockPanel.AllDockPanels.Any(_dp =>
                            {
                                var w = (_dp.ParentWindow?.GetBackend() as IWindowFrameBackend)?.Window;
                                return object.ReferenceEquals(w.GetType().GetPropertyValue(w, "GdkWindow"), _dp);
                            })).ToList();

                            var hit = dp.OrderBy(_dp =>
                            {
                                var w = (_dp.ParentWindow?.GetBackend() as IWindowFrameBackend)?.Window;
                                var w2 = w.GetType().GetPropertyValue(w, "GdkWindow");
                                return allwin.IndexOf(w2);
                            }).First();

                            var wp = hit.ConvertToScreenCoordinates(hit.Bounds.Location);

                            DockPanel.SetHighLight(hit, new Point(x - wp.X, y - wp.Y), out this.droppane, out this.drophit);
                        }
                        else
                        {
                            DockPanel.ClrHightlight();
                        }*/
                }

                this.xwt.ReleaseCapture(this.Content);
                DockPanel.ClrHightlight();
                this.Close();

                base.SetResult(out dockpane, out dockat);
            }
        }

        #endregion

        internal static void StartDrag(FloatWindow owner, Point position)
        {
            owner.Visible = false;
            DragWindow dragwin = CreateDragWin(owner.DockPanel.xwt, position, owner.DockPanel.Current.WidgetSize);

            Application.InvokeAsync(() =>
            {
                dragwin.Show(out IDockPane droppane, out DockPosition? drophit);

                if (dragwin.result && droppane != null && drophit.HasValue)
                {
                    owner.DockPanel.DockFloatform(owner, droppane, drophit.Value);
                }
                else if (dragwin.result)
                {
                    owner.Location = dragwin.Location.Offset(5, 5);
                    owner.Visible = true;
                }
                dragwin.Dispose();
            });
        }
        public static void StartDrag(IDockPane pane, IDockContent[] documents, Point position)
        {
            DragWindow dragwin = CreateDragWin(pane.DockPanel.xwt, position, pane.WidgetSize);

            Application.InvokeAsync(() =>
            {
                dragwin.Show(out IDockPane droppane, out DockPosition? drophit);

                if (dragwin.result && droppane != null && drophit.HasValue)
                {
                    pane.DockPanel.MovePane(pane, documents, droppane, drophit.Value);
                }
                else if (dragwin.result)
                {
                    pane.DockPanel.FloatPane(pane, documents, dragwin.Location.Offset(5, 5), pane.WidgetSize);
                }
                dragwin.Dispose();
            });
        }

        private static DragWindow CreateDragWin(IXwt xwt, Point position, Size size)
        {
            switch (Toolkit.CurrentEngine.Type)
            {
                case ToolkitType.Wpf: return new DragWindowWpf(xwt, position, size);
                case ToolkitType.XamMac: return new DragWindowXamMac(xwt, position, size);
                case ToolkitType.Gtk: return new DragWindowGTK2(xwt, position, size);
                case ToolkitType.Gtk3: return new DragWindowGTK3(xwt, position, size);
                default: throw new NotImplementedException();
            }
        }
        internal static void CheckMove(DragWindow window, Point pt, bool setpos, Size floatsize, ref IDockPane droppane, ref DockPosition? drophit)
        {
            try
            {
                var hits = BaseLib.XwtPlatForm.PlatForm.Instance.Search(window, pt); // all hit window-handle son system

                foreach (var w in hits)
                {
                    if (object.ReferenceEquals((window.GetBackend() as IWindowBackend).Window, w.Item2))
                    {
                        continue;// hit through dragwindow
                    }
                    var hit = DockPanel.CheckHit(w.Item2, pt.X, pt.Y);

                    if (hit != null)
                    {
                        var b = hit.ConvertToScreenCoordinates(hit.Bounds.Location);

                        DockPanel.SetHighLight(hit, new Point(pt.X - b.X, pt.Y - b.Y), out droppane, out drophit);
                        return;
                    }
                    if (Toolkit.CurrentEngine.Type == ToolkitType.Wpf)
                    {
                        if (w.Item2.GetType().FullName != "Microsoft.VisualStudio.DesignTools.WpfTap.WpfVisualTreeService.Adorners.AdornerLayerWindow")
                        {
                            break; // window in front
                        }
                    }
                    else
                    {
                        break;// window in front
                    }
                }
                droppane = null; drophit = null;
                DockPanel.ClrHightlight();
            }
            catch { throw; }
            finally
            {
                if (setpos)
                {
                    if (droppane == null || !drophit.HasValue)
                    {
                        window.SetPosition(new Rectangle(pt.Offset(-5, -5), floatsize));
                    }
                    else
                    {
                        window.SetPosition(new Rectangle(pt.Offset(-5, -5), new Size(32, 32)));
                    }
                }
            }
        }
    }
}