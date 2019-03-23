using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xwt;
using Xwt.Backends;

namespace BaseLib.DockIt_Xwt
{
    partial class XwtImpl
    {
        class GTK : IXwtImpl
        {
            class DragWindow : XwtImpl.DragWindow
            {
                private bool doexit;

                class MyCanvas : Canvas
                {
                    private readonly DragWindow owner;

                    public MyCanvas(DragWindow dragWindow)
                    {
                        this.owner = dragWindow;

                        ExpandHorizontal = true;
                        ExpandVertical = true;
                        CanGetFocus = true;
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
                        base.OnButtonPressed(args);
                    }
                    protected override void OnButtonReleased(ButtonEventArgs args)
                    {
                        base.OnButtonReleased(args);
                    }
                }
                public DragWindow(IXwt xwt, Canvas widget, Point position)
                    : base(xwt, widget,position)
                {
                    var backend = Toolkit.CurrentEngine.GetSafeBackend(this);
                    (backend as IWindowFrameBackend).ShowInTaskbar = false;

                    this.Content = new MyCanvas(this);
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
             /*   private void Content_KeyPressed(object sender, KeyEventArgs e)
                {
                    if (e.Key == Key.Escape)
                    {
                        this.doclose(false);
                        e.Handled = true;
                    }
                }

                private void Content_ButtonReleased(object sender, ButtonEventArgs e)
                {
                    this.doclose(e.Button == PointerButton.Left);
                    e.Handled = true;
                }*/

                private void doclose(bool apply)
                {
                    this.result = apply;
                    this.doexit = true;
                }

                public override void Show()
                {
                    this.doexit = false;
                    this.result = true;

                    (this as Window).Show();

                    this.Content.SetFocus();
                    this.xwt.SetCapture(this.Content);

                    while (!this.doexit)
                    {
                        var gtkwin = /*(global::Gtk.Window)*/(this.GetBackend() as IWindowFrameBackend).Window;
                        var display = gtkwin.GetType().GetPropertyValue(gtkwin, "Display");
                        var screen = display.GetType().GetPropertyValue(display, "DefaultScreen");

                        Type t = XwtImpl.GetType("Gdk.ModifierType");
                        
                        var parms = new object[] { 0, 0, Enum.ToObject(t,0) };
                        var mi = display.GetType().GetMethod("GetPointer", new Type[] { Type.GetType("System.Int32&"), Type.GetType("System.Int32&"), XwtImpl.GetType("Gdk.ModifierType&") });
                        mi.Invoke(display, parms);
//                        display.GetType().Invoke(display, "GetPointer", parms);
                        //   display.GetPointer(out int x, out int y, out Gdk.ModifierType mask);
                        int x = (int)parms[0];
                        int y = (int)parms[1];
                        int mask = (int)parms[2];


                        this.doexit = (mask & 0x100/*button1mask*/) == 0;

                        this.Location = new Point(x, y).Offset(-5, -5);
                        this.Content.SetFocus();

                        DoEvents();

                        var dp = DockPanel.GetHits(x, y);

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

                            DockPanel.SetHighLight(hit, new Point(x - wp.X, y - wp.Y), out this.droppane,  out this.drophit);
                        }
                        else
                        {
                            DockPanel.ClrHightlight();
                        }
                    }
                    
                    this.xwt.ReleaseCapture(this.Content);
                    DockPanel.ClrHightlight();
                    this.Close();

                    if (this.result)
                    {
                    }
                }

                private static void DoEvents()
                {
                    Type tctx = XwtImpl.GetType("GLib.MainContext");

                    var mi_iteration = tctx.GetMethod("Iteration", new Type[0]);

                    while ((bool)mi_iteration.Invoke(null, new object[0])) { }
                }
            }

            [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "gdk_pointer_grab")]
            private static extern int x11_gdk_pointer_grab(IntPtr gdkwindow, bool owner_events, int mask, IntPtr confine_to_gdkwin, IntPtr cursor, int time);
            [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "gdk_pointer_grab", CallingConvention = CallingConvention.Cdecl)]
            private static extern int win_gdk_pointer_grab(IntPtr gdkwindow, bool owner_events, int mask, IntPtr confine_to_gdkwin, IntPtr cursor, int time);

            [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "gdk_pointer_ungrab")]
            private static extern void x11_gdk_pointer_ungrab(int time);
            [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "gdk_pointer_ungrab", CallingConvention = CallingConvention.Cdecl)]
            private static extern void win_gdk_pointer_ungrab(int time);

            public void ReleaseCapture(Widget widget)
            {
                var backend = Toolkit.CurrentEngine.GetSafeBackend(widget);
                var tn = backend.GetType().FullName;
                var w = backend.GetType().GetPropertyValue(backend, "Widget");
                var gdk = w.GetType().GetPropertyValue(w, "GdkWindow");
                var h = (IntPtr)gdk.GetType().GetPropertyValue(gdk, "Handle");

                if (System.Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    x11_gdk_pointer_ungrab(0);
                }
                else if (System.Environment.OSVersion.Platform == PlatformID.MacOSX)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    win_gdk_pointer_ungrab(0);
                    //   Gdk.Pointer.Ungrab(0);
                }
            }

            public void SetCapture(Widget widget)
            {
                var backend = Toolkit.CurrentEngine.GetSafeBackend(widget);
                var tn = backend.GetType().FullName;
                var w = backend.GetType().GetPropertyValue(backend, "Widget");
                var gdk = w.GetType().GetPropertyValue(w, "GdkWindow");
                var h = (IntPtr)gdk.GetType().GetPropertyValue(gdk, "Handle");

                if (System.Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    x11_gdk_pointer_grab(h, true, 0, IntPtr.Zero, IntPtr.Zero, 0);
                }
                else if (System.Environment.OSVersion.Platform == PlatformID.MacOSX)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    win_gdk_pointer_grab(h, true, 0, IntPtr.Zero, IntPtr.Zero, 0);
                    //   Gdk.Pointer.Grab((backend as global::Xwt.GtkBackend.CanvasBackend).Widget.GdkWindow, true, Gdk.EventMask.AllEventsMask, null, null, 0);
                }
            }

            public void StartDrag(Canvas widget, Point position)
            {
                throw new NotImplementedException();
            }

            public XwtImpl.DragWindow Create(Canvas widget, Point position)
            {
                return new DragWindow(this, widget, position);
            }
        }
    }
    }
