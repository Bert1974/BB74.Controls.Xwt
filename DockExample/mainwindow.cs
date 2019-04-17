using BaseLib.Xwt;
using BaseLib.Xwt.DockPanel;
using System;
using System.IO;
using System.Linq;
using Xwt;

namespace DockExample
{
    partial class mainwindow : Window
    {
        DockPanel dock;
        bool closing = false;
        private readonly string settingsfile=Path.Combine(Path.GetDirectoryName(new Uri(typeof(mainwindow).Assembly.CodeBase).AbsolutePath),"positions.xml");


        private IDockContent Deserialize(DockPanel dockpanel, Type type, string data)
        {
            /*if (type == typeof(testdockitem))
            {
                return new testdockitem();
            }*/
            if (type == typeof(testwebitem))
            {
                return new testwebitem(dockpanel, data);
            }
            if (type == typeof(testtoolitem))
            {
                return new testtoolitem(data);
            }
            return null;
        }
        
        public mainwindow(IXwt xwt)
        {
            this.Title = $"Xwt Demo Application {Xwt.Toolkit.CurrentEngine.Type}";
            this.Width = 150; this.Height = 150;
            this.Padding = 0;

            this.CloseRequested += (s, e) => { if (!closing) { e.AllowClose = this.close(); } };

            var menu = new Menu();
            var file = new MenuItem("_File");
            file.SubMenu = new Menu();
            file.SubMenu.Items.Add(UIHelpers.NewMenuItem("New window", new_mainwindow));
            file.SubMenu.Items.Add(UIHelpers.NewMenuItem("New testdoc", new_testdoc));
            file.SubMenu.Items.Add(UIHelpers.NewMenuItem("New toolbar", new_toolbar));
            file.SubMenu.Items.Add(UIHelpers.NewMenuItem("New google", new_webview));
            file.SubMenu.Items.Add(UIHelpers.NewMenuItem("New properties", new_properties));
            //   file.SubMenu.Items.Add(new MenuItem("_Open"));
            //    file.SubMenu.Items.Add(new MenuItem("_New"));
            var mi = new MenuItem("_Close");
            mi.Clicked += (s, e) => { if (this.close()) { base.Close(); }; };
            file.SubMenu.Items.Add(mi);
            menu.Items.Add(file);

            /*      var edit = new MenuItem("_Edit");
                  edit.SubMenu = new Menu();
                  edit.SubMenu.Items.Add(new MenuItem("_Copy"));
                  edit.SubMenu.Items.Add(new MenuItem("Cu_t"));
                  edit.SubMenu.Items.Add(new MenuItem("_Paste"));
                  menu.Items.Add(edit);*/

            var dockmenu = new MenuItem("Dock") { SubMenu = new Menu() };
            dockmenu.SubMenu.Items.Add(UIHelpers.NewMenuItem("save layout to disk", save_layout));
            dockmenu.SubMenu.Items.Add(UIHelpers.NewMenuItem("load layout from disk", load_layout));
            menu.Items.Add(dockmenu);

            this.MainMenu = menu;
            this.Content = dock = new DockPanel(this, xwt);

            try
            {
                dock.LoadXml(settingsfile, true, Deserialize);
            }
            catch
            {
                dock.Dock(new testdockitem());
                dock.Dock(new testtoolitem(this), DockPosition.Top);
                dock.Dock(new IDockContent[] { new testtoolitem(this), new testtoolitem(this), new testtoolitem(this), new testtoolitem(this), new testtoolitem(this) }, DockPosition.Bottom);
            }
        }
        protected override void OnShown()
        {
            base.OnShown();
            var backend = this.BackendHost.Backend as Xwt.Backends.IWindowFrameBackend;
            var gtkkwin = backend.Window;
            //     var gdkwin = gtkkwin.GetType().GetPropertyValue(gtkkwin, "GdkWindow");
            //          dock.OnLoaded();
        }
        protected override void OnClosed()
        {
            base.OnClosed();

            if (Program.RemoveWindow(this))
            {
                Application.Exit();
            }
        }
        bool close()
        {
            this.closing = true;
            this.SaveDock(settingsfile);
            return true;
        }

        void new_mainwindow(object sender, EventArgs e)
        {
            UIHelpers.NewWindow();
        }
        void new_testdoc(object sender, EventArgs e)
        {
            dock.Dock(new testdockitem());
        }
        void new_toolbar(object sender, EventArgs e)
        {
            dock.Dock(new testtoolitem(this), DockPosition.Left);
        }
        void new_webview(object sender, EventArgs e)
        {
            dock.Dock(new testwebitem());
        }
        void new_properties(object sender, EventArgs e)
        {
            dock.Dock(new testpropertiesitem(), DockPosition.Left);
        }
        void save_layout(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Save dockk layout";
                Init(dialog);

                if (dialog.Run(this))
                {
                    SaveDock(dialog.FileName);
                }
            }
        }


        void load_layout(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Save dockk layout";
                Init(dialog);

                if (dialog.Run(this))
                {
                    LoadDock(dialog.FileName);
                }
            }
        }

        private void LoadDock(string fileName)
        {
            dock.LoadXml(fileName, false, Deserialize);
        }

        private void SaveDock(string fileName)
        {
            dock.SaveXml(fileName, false);
        }

        void Init(FileDialog dialog)
        {
            dialog.Multiselect = false;
            dialog.Filters.Add(new FileDialogFilter("xml files", "*.xml"));
            dialog.Filters.Add(new FileDialogFilter("all files", "*.*"));
            dialog.ActiveFilter = dialog.Filters.First();
        }
    }
}