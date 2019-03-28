using BaseLib.DockIt_Xwt;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xwt;
using Xwt.Drawing;

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
            var mainWindow = new mainwindow()
            {
            };
            Program.AddWindow(mainWindow);
            mainWindow.Show();
        }
    }

    class Program
    {
        static readonly List<mainwindow> openwindows = new List<mainwindow>();

        [STAThread()]
        static void Main(string[] args)
        {
#if (__MACOS__)
            InitToolkit(ToolkitType.XamMac);
#else
            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                InitToolkit(ToolkitType.Gtk3);
            }
            else
            {
                InitToolkit(ToolkitType.Wpf);
            }
#endif
            UIHelpers.NewWindow();
            Application.Run();
        }
        public static void InitToolkit(ToolkitType type)
        {
            switch (type)
            {
                case ToolkitType.Gtk3:
                    LoadDlls("3.0");
                    break;
                case ToolkitType.Gtk:
                    LoadDlls("2.0");
                    break;
            }
            Application.Initialize(type);
        }

        private static void LoadDlls(string dllversion)
        {
            LoadDll("gdk-sharp", dllversion);
            LoadDll("glib-sharp", dllversion);
            LoadDll("pango-sharp", dllversion);
            LoadDll("gdk-sharp", dllversion);
            LoadDll("gtk-sharp", dllversion);
            LoadDll("atk-sharp", dllversion);
        }
        private static void LoadDll(string name, string dllversion)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                Assembly.LoadFile($"/usr/lib/cli/{name}-{dllversion}/{name}.dll");
            }
            else
            {
                Assembly.Load(new AssemblyName($"{name}, Version={dllversion}"));
            }
        }
        private static void AssemblyLoad(string assembly)
        {
            var name = new AssemblyName(assembly);
            Assembly.Load(name);
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