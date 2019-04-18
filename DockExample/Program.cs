using System;
using System.Collections.Generic;
using BaseLib.Xwt;
using Xwt;

namespace DockExample
{
    static class UIHelpers
    {
        public static MenuItem NewMenuItem(string text, EventHandler click)
        {
            var r = new MenuItem(text);
            r.Clicked += click;
            return r;
        }
        public static void NewWindow()
        {
            var mainWindow = new mainwindow(Program.Xwt)
            {
            };
            Program.AddWindow(mainWindow);
            mainWindow.Show();
        }
    }

    class Program
    {
        static readonly List<mainwindow> openwindows = new List<mainwindow>();
        public static IXwt Xwt { get; private set; }

        [STAThread()]
        static void Main(string[] args)
        {
#if (__MACOS__)
            Application.Initialize(ToolkitType.XamMac);
#else
            if (System.Environment.OSVersion.Platform == PlatformID.Unix || System.Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                BaseLib.Xwt.PlatForm.Initialize(args.Contains("gtk3")? ToolkitType.Gtk3 : ToolkitType.Gtk);
            }
            else
            {
                Application.Initialize(ToolkitType.Wpf);
            }
#endif
            Program.Xwt = (IXwt)XwtImpl.Create();

            UIHelpers.NewWindow();
            Application.Run();
        }
        public static void AddWindow(mainwindow window)
        {
            openwindows.Add(window);
        }
        public static bool RemoveWindow(mainwindow window)
        {
            openwindows.Remove(window);
            return openwindows.Count == 0;
        }
    }
}