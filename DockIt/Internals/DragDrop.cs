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
                    this.BackgroundColor = DockPanel.DropTargetColor;
                    this.GetBackend().CanGetFocus = true;

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
                    args.Handled = true;
                    owner.doclose(false);
                    //    base.OnButtonPressed(args);
                }
                protected override void OnButtonReleased(ButtonEventArgs args)
                {
                    args.Handled = true;
                    owner.doclose(true);
                    //     base.OnButtonReleased(args);
                }
                protected override void OnMouseMoved(MouseMovedEventArgs args)
                {
                    args.Handled = true;
                    if (this.checkmouse)
                    {
                        var pt = this.ConvertToScreenCoordinates(args.Position);
                        owner.CheckMove(pt, true);
                    }
                }
            }
            #endregion

            public delegate void DragResultFunction(bool result, IDockPane pane, DockPosition? pos, Point floatpos);
            public abstract void Show(DragResultFunction resultfunction);

            internal void SetResult(DragResultFunction resultfunction)
            {
                resultfunction?.Invoke(this.result, this.droppane, this.drophit, this.Location);
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
                : base(xwt, position, size, true)
            {
                var wpfwin = (this.GetBackend() as IWindowFrameBackend).Window;
                wpfwin.GetType().SetPropertyValue(wpfwin, "AllowsTransparency", true);

                SetMaxWidth(32);

                base.Size = new Size(32, 32);
            }

            public override void Show(DragResultFunction resultfunction)
            {
                this.doexit = false;

                this.Show();
                this.Content.SetFocus();

                this.xwt.SetCapture(this.Content);

                while (!this.doexit)
                {
                 /*   var pt = new Win32.POINT();
                    Win32.GetCursorPos(ref pt);

                    this.CheckMove(pt, true);*/

                    this.xwt.DoEvents();
                }
                this.xwt.ReleaseCapture(this.Content);

                DockPanel.ClrHightlight();

                base.Close();

                base.SetResult(resultfunction);
            }
            internal override void SetPosition(Rectangle rectangle)
            {
                SetMaxWidth(Convert.ToInt32(rectangle.Width));
                base.SetPosition(rectangle);
            }
            private void SetMaxWidth(int width)
            {
                var wpfwin = (this.GetBackend() as IWindowFrameBackend).Window;
                //  var t = PlatForm.GetType("System.Windows.ResizeMode");
               // wpfwin.GetType().SetPropertyValue(wpfwin, "ResizeMode", Enum.Parse(t, "NoResize"));
                  wpfwin.GetType().SetPropertyValue(wpfwin, "MaxWidth", width);
            }
        }
        #endregion

        #region class DragWindowXamMac
        class DragWindowXamMac : DragWindow
        {
            public DragWindowXamMac(IXwt wxt, Point position, Size size)
                : base(wxt, position, size, true)
            {
                var backend = Toolkit.CurrentEngine.GetSafeBackend(this);
                (backend as IWindowFrameBackend).ShowInTaskbar = false;
            }
            public override void Show(DragResultFunction resultfunction)
            {
                this.doexit = false;
                this.result = true;

                (this as Window).Show();

                this.Content.SetFocus();
                //  this.xwt.SetCapture(this.Content);

                this.xwt.SetCapture(this.Content);

                while (!doexit)
                {
                    xwt.DoEvents();
                }

                //      object o = XamMac.appkit_nsapplication.GetPropertyValueStatic("SharedApplication");
                //        o.GetType().Invoke(o, "RunModalForWindow", (this.GetBackend() as IWindowFrameBackend).Window);
#if (false)
                 while (!this.doexit)
                {
               // this.xwt.DoEvents(this.Content);
                    object o = XamMac.appkit_nsapplication.GetPropertyValueStatic("SharedApplication");



                    object e;
                    object mask = Enum.ToObject(XamMac.appkit_nseventmask, (ulong)18446744073709551615L/*0x44*/);
                     object now = PlatForm.GetType("Foundation.NSDate").GetPropertyValueStatic("DistantFuture");
                    object mode = Enum.Parse(XamMac.found_nsrunloopmode, "EventTracking");
                    //     object[] args = { mask, 0, mode, true };
                    //     var mi = o.GetType().GetMethod("NextEvent", args.Select(_a => _a.GetType()).ToArray());
                    do
                    {
                    //    var mask = Activator.CreateInstance(PlatForm.GetType("System.nuint"), new object[] { (ulong)0x44 }); // leftup,leftdrag

                        try
                        {
                            e = XamMac.mi_nsapp_nextevent.Invoke(o, new object[] { mask, now, mode, true });
                           //    e = XamMac.mi_nswindow_nextevent.Invoke((this.GetBackend() as IWindowFrameBackend).Window, new object[] { mask/*, now, mode.ToString(), true*/ });
                          //  var et = GetType().GetPropertyValue(e, "Type");
                            //      e = mi.Invoke(o, args);
                        }
                        catch (Exception ee)
                        {
                            throw;
                        }
                    }
                    while (e != null);
                    /*    var pt = XamMac.appkit_nsevent.GetPropertyValueStatic("CurrentMouseLocation");
                        var mask = XamMac.appkit_nsevent.GetPropertyValueStatic("CurrentPressedMouseButtons");

                        var x = (int)Convert.ToDouble(pt.GetType().GetPropertyValue(pt, "X"));
                        var y = (int)Convert.ToDouble(pt.GetType().GetPropertyValue(pt, "Y"));

                        this.doexit = (Convert.ToUInt32(mask) & 1) == 0;

                        var cgsizetype = PlatForm.GetType("CoreGraphics.CGPoint");

                        var cgpt = Activator.CreateInstance(cgsizetype, new object[] { (double)x, (double)y });
                        var pt2 = (Xwt.Point)XamMac.xwtmacbackend.InvokeStatic("ToDesktopPoint", cgpt);

                        this.CheckMove(pt2, true);*/

                    /*    object sharednsapp = XamMac.appkit_nsapplication.GetPropertyValueStatic("SharedApplication");
                         object e;
                         object mask = Enum.Parse(XamMac.appkit_nseventmask, "AnyEvent");
                         //  object now = PlatForm.GetType("Foundation.NSDate").GetPropertyValueStatic("Now");
                         object mode = Enum.Parse(XamMac.found_nsrunloopmode, "EventTracking");
                         //     object[] args = { mask, 0, mode, true };
                         //     var mi = o.GetType().GetMethod("NextEvent", args.Select(_a => _a.GetType()).ToArray());
                        // do
                         {
                             e = XamMac.mi_nswindow_nextevent.Invoke(this, new object[] { mask, 0, mode, true });

                         //    var et = XamMac.appkit_nsevent.GetPropertyValue(e, "Type");
                             if(
                             //      e = mi.Invoke(o, args);
                         }
                         while (e != null);*/

                 //   this.xwt.DoEvents(this.Content);
                }
#endif
                //      this.xwt.ReleaseCapture(this.Content);
                DockPanel.ClrHightlight();
                this.Close();

                base.SetResult(resultfunction);
            }
            protected override void doclose(bool apply)
            {
                base.doclose(apply);

                this.xwt.ReleaseCapture(this.Content);
            }
        }
        #endregion

        #region class DragWindowGTK2
        class DragWindowGTK2 : DragWindow
        {
            public DragWindowGTK2(IXwt xwt, Point position, Size size)
                : base(xwt, position, size, true)
            {
            }
            public override void Show(DragResultFunction resultfunction)
            {
                try
                {
                    this.doexit = false;
                    this.result = true;

                    (this as Window).Show();
                    this.Content.SetFocus();

                    this.xwt.SetCapture(this.Content);

                    while (!this.doexit)
                    {
                   /*     var gtkwin = (this.GetBackend() as IWindowFrameBackend).Window;
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

                        this.CheckMove(new Point(x, y), true);*/

                        this.xwt.DoEvents();
                    }

                    this.xwt.ReleaseCapture(this.Content);
                    DockPanel.ClrHightlight();
                    this.Close();

                    base.SetResult(resultfunction);
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
                : base(xwt, position, size, true)
            {
                var backend = Toolkit.CurrentEngine.GetSafeBackend(this);
                (backend as IWindowFrameBackend).ShowInTaskbar = false;
            }
            public override void Show(DragResultFunction resultfunction)
            {
                this.doexit = false;
                this.result = true;

                (this as Window).Show();

                this.Content.SetFocus();
                this.xwt.SetCapture(this.Content);

                while (!this.doexit)
                {
                    /*      var gtkwin = (this.GetBackend() as IWindowFrameBackend).Window;
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

                          CheckMove(new Point(x, y), true);*/
                    this.xwt.DoEvents();
                }
                this.xwt.ReleaseCapture(this.Content);
                DockPanel.ClrHightlight();
                this.Close();

                base.SetResult(resultfunction);
            }
        }

        #endregion

        internal static void StartDrag(FloatWindow floatform, Point position)
        {
            floatform.Visible = false;
            DragWindow dragwin = CreateDragWin(floatform.DockPanel.xwt, position, floatform.DockPanel.Current.WidgetSize);

            //  floatform.DockPanel.xwt.QueueOnUI(() => {
            dragwin.Show((result, droppane, drophit, pt) =>
            {
                if (result && droppane != null && drophit.HasValue)
                {
                    floatform.DockPanel.DockFloatform(floatform, droppane, drophit.Value);
                }
                else if (result)
                {
                    (floatform.GetBackend() as IWindowFrameBackend).Bounds = new Rectangle(pt, (floatform.GetBackend() as IWindowFrameBackend).Bounds.Size);
                    floatform.Visible = true;
                }
                dragwin.Dispose();
            });
        //    });
        }
        public static void StartDrag(IDockPane pane, IDockContent[] documents, Point position)
        {
            DragWindow dragwin = CreateDragWin(pane.DockPanel.xwt, position, pane.WidgetSize);

            //  pane.DockPanel.xwt.QueueOnUI(() => {

            dragwin.Show((result, droppane, drophit, pt) =>
            {
                if (result && droppane != null && drophit.HasValue)
                {
                    pane.DockPanel.MovePane(pane, documents, droppane, drophit.Value);
                }
                else if (result)
                {
                    pane.DockPanel.FloatPane(pane, documents, pt, pane.WidgetSize);
                }
                dragwin.Dispose();
            });
       //     });
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