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
    internal class xwtsamples : FrameBox, IDockToolbar
    {
        protected TreeStore store;
        protected Assembly a = null;
        private WindowFrame mainwwindowxwt;
        private TreeView samplesTree;
        
        public virtual string TabText => "Xwt.Samples";
        public IDockPane DockPane { get; set; }
        Widget IDockContent.Widget => this;

        public xwtsamples(WindowFrame mainwin)
        {
            this.MinWidth = this.MinHeight = 100;
            this.BackgroundColor = Colors.White;
            base.ExpandHorizontal = ExpandVertical = true;

            TryGetSamples(null);

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
                TryGetSamples((this as IDockContent).DockPane.DockPanel.ParentWindow);
            }
        }

        protected virtual void Wxtsamples_SelectionChanged(object sender, EventArgs e)
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
        private void TryGetSamples(WindowFrame mainwin)
        {
            if (this.store == null)
            {
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

                        // crate samples main window
                        this.mainwwindowxwt = (WindowFrame)Activator.CreateInstance(a.GetType("Samples.MainWindow"));
                        typeof(WindowFrame).GetField("closeRequested", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(mainwwindowxwt, null);

                        this.samplesTree = FillTree(this.mainwwindowxwt, out this.store);

                        samplesTree.SelectionChanged += Wxtsamples_SelectionChanged;

                        this.Content = samplesTree;
                    }
                    catch { }
                }
            }
        }

        protected virtual TreeView FillTree(WindowFrame mainwwindowxwt, out TreeStore store)
        {
            var content = (HPaned)mainwwindowxwt.GetType().GetProperty("Content", BindingFlags.Public | BindingFlags.Instance).GetValue(mainwwindowxwt, new object[0]);
            var samplesTree = content.Panel1.Content as TreeView;
            samplesTree.GetType().GetField("selectionChanged", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(samplesTree, null);
            content.Panel1.Content = null;
            store = samplesTree.DataSource as TreeStore;

            return samplesTree;
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
    internal class xwtsample : FrameBox, IDockDocument
    {
        Widget IDockContent.Widget => this;
        public virtual string TabText => "Xwt.Sample";

        public Widget Widget { get; private set; }
        IDockPane IDockContent.DockPane { get; set; }

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
}