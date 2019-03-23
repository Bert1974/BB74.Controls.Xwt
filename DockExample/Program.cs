using BaseLib.DockIt_Xwt;
using System;
using Xwt;
using Xwt.Drawing;

namespace DockExample
{
    class Program
    {
        static DockPanel dock;

        [STAThread()]
        static void Main(string[] args)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                Application.Initialize(ToolkitType.Gtk);
            }
            else
            {
                Application.Initialize(ToolkitType.Wpf);
            }
            var mainWindow = new Window()
            {
                Title = $"Xwt Demo Application {Xwt.Toolkit.CurrentEngine.Type}",
                Width = 400,
                Height = 400,
                Content = dock = new DockPanel()
            };

            dock.Dock(new testdockitem());
            dock.Dock(new testtoolitem(), DockPosition.Top);

            mainWindow.Show();
            Application.Run();
            mainWindow.Dispose();
        }
    }
    class testdockitem : Canvas, IDockDocument
    {
        Widget IDockContent.Widget => this;
        string IDockContent.TabText => "testdoc";

        public testdockitem()
        {
            this.MinWidth = this.MinHeight = 100;
            this.BackgroundColor = Colors.Aquamarine;
        }
    }
    class testtoolitem : Canvas, IDockToolbar
    {
        Widget IDockContent.Widget => this;
        string IDockContent.TabText => "tool";

        public testtoolitem()
        {
            this.MinWidth = this.MinHeight = 100;
            this.BackgroundColor = Colors.Aquamarine;
        }
    }
}