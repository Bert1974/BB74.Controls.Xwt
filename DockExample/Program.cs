﻿using System;
using System.Linq;
using System.Collections.Generic;
using BaseLib.Xwt;
using Xwt;
using System.Reflection;
using System.IO;

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
            try
            {
#if (__MACOS__)
                if (args.Contains("gtk"))
                {
                    try { BaseLib.Xwt.Platform.Initialize(ToolkitType.Gtk); }
                    catch { Application.Initialize(ToolkitType.XamMac); }
                }
                else
                {
                    Application.Initialize(ToolkitType.XamMac);
                }
#else
                if (BaseLib.Xwt.Platform.OSPlatform == PlatformID.MacOSX)
                {
                    BaseLib.Xwt.Platform.Initialize(args.Contains("-gtk") ? ToolkitType.Gtk : ToolkitType.XamMac);
                }
                else if (System.Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    BaseLib.Xwt.Platform.Initialize(args.Contains("-gtk3") ? ToolkitType.Gtk3 : ToolkitType.Gtk);
                }
                else
                {
                    if (args.Contains("-gtk"))
                    {
                        try { Application.Initialize(ToolkitType.Gtk); }
                        catch { Application.Initialize(ToolkitType.Wpf); }
                    }
                    else
                    {
                        Application.Initialize(ToolkitType.Wpf);
                    }
                }
#endif
                Program.Xwt = (IXwt)XwtImpl.Create();

                UIHelpers.NewWindow();
                Application.Run();
            }
            catch(Exception e)
            {
            }
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