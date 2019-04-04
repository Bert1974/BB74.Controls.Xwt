using System;
using System.Reflection;
using BaseLib.XwtPlatForm;

namespace BaseLib.DockIt_Xwt.Interop
{
    static class XamMac
    {
        public static Type appkit_nsapplication = PlatForm.GetType("AppKit.NSApplication");
        public static Type appkit_nswindow = PlatForm.GetType("AppKit.NSWindow");
        public static Type appkit_nsevent = PlatForm.GetType("AppKit.NSEvent");
        public static Type appkit_nseventmask = PlatForm.GetType("AppKit.NSEventMask");
        public static Type appkit_nsrunningapp = PlatForm.GetType("AppKit.NSRunningApplication");
        public static Type xwtmacbackend = PlatForm.GetType("Xwt.Mac.MacDesktopBackend");
        public static Type found_nsrunloopmode = PlatForm.GetType("Foundation.NSRunLoopMode");
        public static Type found_nsdate = PlatForm.GetType("Foundation.NSDate");
        public static Type cg_cgrect = PlatForm.GetType("CoreGraphics.CGRect");


        public static MethodInfo mi_nsapp_nextevent = appkit_nsapplication.GetMethod("NextEvent", new Type[] { appkit_nseventmask, found_nsdate, found_nsrunloopmode, typeof(bool) });
        public static MethodInfo mi_nswindow_nextevent = appkit_nswindow.GetMethod("NextEventMatchingMask", new Type[] { appkit_nseventmask/*, found_nsdate, typeof(string), typeof(bool)*/ });
    }
}
