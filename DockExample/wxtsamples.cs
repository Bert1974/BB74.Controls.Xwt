using BaseLib.Xwt.Controls;
using BaseLib.Xwt.Controls.DockPanel;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Xwt;
using Xwt.Drawing;

namespace DockExample
{
    internal class xwtsample : FrameBox, IDockDocument
    {
        Widget IDockContent.Widget => this;
        string IDockContent.TabText => "XwtSamples";
        IDockPane IDockContent.DockPane { get; set; }
        public Widget Widget { get; private set; }

        public xwtsample(xwtsamples toolbar, Widget widget)
        {
            this.Widget = widget;
            this.Content = widget;
            widget.ExpandHorizontal = widget.ExpandVertical = true;
        }

        internal void Set(Widget widget)
        {
            this.Content = null;
            this.Widget.Dispose();
            this.Content = this.Widget = widget;
        }
    }
    internal class xwtsamples : FrameBox, IDockToolbar
    {
            private TreeStore store;
        private WindowFrame mainwwindowxwt;
        private TreeView samplesTree;

        public Widget Widget => this;
        public string TabText => "XwtSamples";
        public IDockPane DockPane { get; set; }


        public xwtsamples(WindowFrame mainwin)
        {
            this.MinWidth = this.MinHeight = 100;
            this.BackgroundColor = Colors.White;
            base.ExpandHorizontal = ExpandVertical = true;

            TryeGetSamples(null);

            if (this.store == null)
            {
                base.Content = new Label("no dll loaded") { HorizontalPlacement=VerticalPlacement=WidgetPlacement.Center};
                base.Content.ButtonPressed += Wxtsamples_ButtonPressed;
            }
        }
        protected override void Dispose(bool disposing)
        {
            this.mainwwindowxwt?.Dispose();
            base.Dispose(disposing);
        }

        private void Wxtsamples_ButtonPressed(object sender, ButtonEventArgs e)
        {
            if (this.store == null)
            {
                TryeGetSamples((this as IDockContent).DockPane.DockPanel.ParentWindow);
            }
        }

        private void Wxtsamples_SelectionChanged(object sender, EventArgs e)
        {
            if (samplesTree.SelectedRow != null)
            {
                var docwin = DockPane.DockPanel.AllContent.OfType<xwtsample>().FirstOrDefault();

                var nav = store.GetNavigatorAt(samplesTree.SelectedRow);
                var navbackend = (Xwt.Backends.ITreeStoreBackend)nav.GetType().GetField("backend", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(nav);
                //   if (currentSample != null)
                //           sampleBox.Remove(currentSample);
                var sample = navbackend.GetValue(nav.CurrentPosition, 2);


                Widget widget;
                var widgettype = (Type)sample.GetType().GetField("Type").GetValue(sample);

                if (widgettype != null)
                {
                    if (docwin != null)
                    {
                        if (docwin.Widget.GetType() != widgettype)
                        {
                            widget = (Widget)Activator.CreateInstance(widgettype);
                            docwin.Set(widget);
                        }
                        else
                        {
                            //todo bring to front
                            return;
                        }
                    }
                    else
                    {
                        widget = (Widget)Activator.CreateInstance(widgettype);

                        DockPane.DockPanel.Dock(new xwtsample(this, widget));
                        //      sample.GetType().GetProperty("Widget").SetValue(sample, widget, new object[0]);
                    }
                }
            }
        }        
        private void TryeGetSamples(WindowFrame mainwin)
        {
            if (this.store == null)
            {
                Assembly a = null;
                try
                {
                    a = Assembly.LoadFile(Program.MainSettings.xmlsampledllpath);
                }
                catch { }
                if (a == null && mainwin != null)
                {
                    using (var ofd = new OpenFileDialog("Selext Xwt.Samples"))
                    {
                        if (ofd.Run(mainwin))
                        {
                            try
                            {
                                a = Assembly.LoadFile(ofd.FileName);
                                Program.MainSettings.xmlsampledllpath = ofd.FileName;
                            }
                            catch (Exception e)
                            {
                            }
                        }
                    }
                }
                if (a != null)
                {try
                    {
                        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

                        this.mainwwindowxwt = (WindowFrame)Activator.CreateInstance(a.GetType("Samples.MainWindow"));
                        var content = (HPaned)mainwwindowxwt.GetType().GetProperty("Content", BindingFlags.Public | BindingFlags.Instance).GetValue(mainwwindowxwt, new object[0]);
                        this.samplesTree = content.Panel1.Content as TreeView;
                        content.Panel1.Content = null;
                        this.Content = samplesTree;
                        this.store = samplesTree.DataSource as TreeStore;

                       samplesTree.GetType().GetField("selectionChanged", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(samplesTree, null);
                        typeof(WindowFrame).GetField("closeRequested", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(mainwwindowxwt, null);
                        
                        samplesTree.SelectionChanged += Wxtsamples_SelectionChanged;
                    }
                    catch { }
                }
            }
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("Xwt,"))
            {
                return typeof(global::Xwt.Widget).Assembly;
            }
            return null;
        }
    }
}