using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BaseLib.Xwt.Interop
{
    static class Gtk
    {
        public static Type gtk_window = Platform.GetType("Gtk.Window");
        public static Type gtk_application = Platform.GetType("Gtk.Application");
        public static Type gtk_grab = Platform.GetType("Gtk.Grab");
    }
    static class Gdk
    {
        public static Type gdk_threads = Platform.GetType("Gdk.Threads");
    }
    static class GtkMac
    {
        const string libQuartz = "libgdk-win32-2.0-0.dll";

        [DllImport(libQuartz)]
        internal extern static IntPtr gdk_quartz_window_get_nswindow(IntPtr window);
    }
}
