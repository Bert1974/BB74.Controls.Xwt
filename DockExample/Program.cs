using System;
using System.Linq;
using System.Collections.Generic;
using BaseLib.Xwt;
using Xwt;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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

   [Serializable()]
   public class MainSettings
    {
        public string xmlsampledllpath;
    }

    class Program
    {
        static readonly List<mainwindow> openwindows = new List<mainwindow>();
        public static IXwt Xwt { get; private set; }

        public static MainSettings MainSettings { get; private set; }
        [STAThread()]
        static void Main(string[] args)
        {
            try
            {
                using (var s = File.OpenRead("mainsettings.xml"))
                {
                    Program.MainSettings = (MainSettings)new BinaryFormatter().Deserialize(s);
                }
            }
            catch
            {
                Program.MainSettings = new MainSettings()
                {
                    xmlsampledllpath = "",
                };
            }
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

                try
                {
                    using (var s = File.OpenWrite("mainsettings.xml"))
                    {
                        new BinaryFormatter().Serialize(s, Program.MainSettings);
                    }
                }
                catch { }
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