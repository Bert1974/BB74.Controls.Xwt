using BaseLib.XwtPlatForm;
using System;
using System.Runtime.InteropServices;
using Xwt;
using Xwt.Backends;

namespace BaseLib.DockIt_Xwt
{
    partial class XwtImpl
    {
        class GTK3 : IXwtImpl
        {
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