using System;
using System.Linq;
using System.Runtime.InteropServices;
using BaseLib.XwtPlatForm;
using Xwt;
using Xwt.Backends;

namespace BaseLib.DockIt_Xwt
{
    partial class XwtImpl
    {
        class GTK3 : IXwtImpl
        {
            class DragWindow : XwtImpl.DragWindow
            {
                public DragWindow(IXwt xwt, Canvas widget, Point position)
                    : base(xwt, widget, position, false)
                {
                    var backend = Toolkit.CurrentEngine.GetSafeBackend(this);
                    (backend as IWindowFrameBackend).ShowInTaskbar = false;
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

                    if (this.result)
                    {
                    }
                }
            }

            [DllImport("libgdk-3-0.dll", EntryPoint = "gdk_device_grab")]
            private static extern int x11_gdk_device_grab(IntPtr device, IntPtr gdkwindow, int ownerevents, bool owner_events, int mask, IntPtr cursor, int time);
            [DllImport("libgdk-3-0.dll", EntryPoint = "gdk_device_ungrab")]
            private static extern int x11_gdk_device_ungrab(IntPtr device, int time);

            [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "gdk_pointer_grab")]
            private static extern int x11_gdk_pointer_grab(IntPtr gdkwindow, bool owner_events, int mask, IntPtr confine_to_gdkwin, IntPtr cursor, int time);/*     [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "gdk_pointer_grab", CallingConvention = CallingConvention.Cdecl)]
                  private static extern int win_gdk_pointer_grab(IntPtr gdkwindow, bool owner_events, int mask, IntPtr confine_to_gdkwin, IntPtr cursor, int time);
                  */
            [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "gdk_pointer_ungrab")]
            private static extern void x11_gdk_pointer_ungrab(int time);
            /*       [DllImport("libgdk-win32-2.0-0.dll", EntryPoint = "gdk_pointer_ungrab", CallingConvention = CallingConvention.Cdecl)]
                    private static extern void win_gdk_pointer_ungrab(int time);*/

            public void ReleaseCapture(Widget widget)
            {
                var backend = Toolkit.CurrentEngine.GetSafeBackend(widget);
                var tn = backend.GetType().FullName;
                var w = backend.GetType().GetPropertyValue(backend, "Widget");
                var gdk = w.GetType().GetPropertyValue(w, "GdkWindow");
                var h = (IntPtr)gdk.GetType().GetPropertyValue(gdk, "Handle");
                var disp = gdk.GetType().GetPropertyValue(gdk, "Display");
                var devs = disp.GetType().GetPropertyValue(disp, "DeviceManager");
                var dev = devs.GetType().GetPropertyValue(devs, "ClientPointer");

                //   if (System.Environment.OSVersion.Platform == PlatformID.Unix)
                {

                    x11_gdk_device_ungrab((IntPtr)dev.GetType().GetPropertyValue(dev, "Handle"), 0);

                    //      x11_gdk_pointer_ungrab(0);
                }
                /*    else if (System.Environment.OSVersion.Platform == PlatformID.MacOSX)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        win_gdk_pointer_ungrab(0);
                        //   Gdk.Pointer.Ungrab(0);
                    }*/
            }

            public void SetCapture(Widget widget)
            {
                var backend = Toolkit.CurrentEngine.GetSafeBackend(widget);
                //    var tn = backend.GetType().FullName;
                var w = backend.GetType().GetPropertyValue(backend, "Widget");
                var gdk = w.GetType().GetPropertyValue(w, "GdkWindow");
                var h = (IntPtr)gdk.GetType().GetPropertyValue(gdk, "Handle");
                var disp = gdk.GetType().GetPropertyValue(gdk, "Display");
                var devs = disp.GetType().GetPropertyValue(disp, "DeviceManager");
                var dev = devs.GetType().GetPropertyValue(devs, "ClientPointer");

                // if (System.Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    x11_gdk_device_grab((IntPtr)dev.GetType().GetPropertyValue(dev, "Handle"), h, 2, true, (1 << 5), IntPtr.Zero, 0);
                    //     x11_gdk_pointer_grab(h, true,(1<<5), IntPtr.Zero, IntPtr.Zero, 0);
                }
                /*         else if (System.Environment.OSVersion.Platform == PlatformID.MacOSX)
                         {
                             throw new NotImplementedException();
                         }
                         else
                         {
                             win_gdk_pointer_grab(h, true, 0, IntPtr.Zero, IntPtr.Zero, 0);
                             //   Gdk.Pointer.Grab((backend as global::Xwt.GtkBackend.CanvasBackend).Widget.GdkWindow, true, Gdk.EventMask.AllEventsMask, null, null, 0);
                         }*/
            }

            public XwtImpl.DragWindow Create(Canvas widget, Point position)
            {
                return new DragWindow(this, widget, position);
            }
            public void DoEvents()
            {
                Type tctx = PlatForm.GetType("GLib.MainContext");

                var mi_iteration = tctx.GetMethod("Iteration", new Type[0]);

                while ((bool)mi_iteration.Invoke(null, new object[0])) { }
            }
            void IXwt.SetParent(WindowFrame r, WindowFrame parentWindow)
            {
            }
        }
    }
}