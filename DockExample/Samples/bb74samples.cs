using BaseLib.Xwt.Controls;
using BaseLib.Xwt.Controls.DockPanel;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Xwt;
using Xwt.Drawing;

namespace DockExample
{
    internal class bb74xwtsamples : xwtsamples
    {
        public override string TabText => "BB74.Demo";

        private readonly DataField<string> nameCol = new DataField<string>();
        private readonly DataField<sampleinfo> widgetCol = new DataField<sampleinfo>();
        private readonly DataField<Image> iconCol = new DataField<Image>();
        private new TreeStore store;
        private TreeView samplesTree;

        private struct sampleinfo
        {
            public Type Type, OriginalType;
        }
        public bb74xwtsamples(WindowFrame mainwin)
            : base(mainwin)
        {

        }
        private TreePosition AddSample(TreePosition pos, string name, Type sampletype, string orgsampletype)
        {
            var t = !string.IsNullOrEmpty(orgsampletype) ? base.a.GetType(orgsampletype) : null;
            var sampleinfo = new sampleinfo() { Type = sampletype, OriginalType = t };
            return store.AddNode(pos).SetValue(nameCol, name).SetValue(iconCol, icon).SetValue(widgetCol, sampleinfo).CurrentPosition;
        }
        protected override TreeView FillTree(WindowFrame mainwwindowxwt, out TreeStore store)
        {
            Debug.Assert(this.store == null);
            this.store = new TreeStore(nameCol, iconCol, widgetCol);

            var bb = AddSample(null, "BB74", null, null);
            AddSample(bb, "PropertyGrid", typeof(Samples.PropertyGrid), null);

            var w = AddSample(null, "Widgets", null, null);
            AddSample(w, "Boxes", typeof(Samples.Boxes), "Samples.Boxes");
            //     var listView = AddSample(w, "ListView", typeof(ListView1), "Samples.ListView1");
            //     AddSample(listView, "Editable Checkboxes", typeof(ListView2), "Samples.ListView2");
            AddSample(w, "Scroll View", typeof(Samples.ScrollWindowSample), "Samples.ScrollWindowSample");

            samplesTree = new TreeView() { ExpandHorizontal = true, ExpandVertical = true };
            samplesTree.Columns.Add("Name", iconCol, nameCol);

            samplesTree.DataSource = this.store;

            store = this.store;
            return this.samplesTree;
        }
        protected override void Wxtsamples_SelectionChanged(object sender, EventArgs e)
        {
            if (samplesTree.SelectedRow != null)
            {
                var nav = store.GetNavigatorAt(samplesTree.SelectedRow);
                var sampleinfo = nav.GetValue(widgetCol);

              /*   Type widgettype = null;

               if (sampleinfo.Type != null)
                {
                    if (sampleinfo.OriginalType != null)
                    {
               */         var docwin = DockPane.DockPanel.AllContent.OfType<bb74sample>().FirstOrDefault();
                // dual view
                var w1 = (Widget)Activator.CreateInstance(sampleinfo.Type);
                var w2 = sampleinfo.OriginalType != null ? (Widget)Activator.CreateInstance(sampleinfo.OriginalType) : (Widget)Activator.CreateInstance(sampleinfo.Type);

                        if (docwin == null)
                        {
                            DockPane.DockPanel.Dock(new bb74sample(this, w1, w2));
                        }
                        else
                        {
                            docwin.Set(w1, w2);
                        }
                        return;
              /*      }
                    else
                    {
                        widgettype = sampleinfo.Type;
                    }
                }
                else if (sampleinfo.OriginalType != null)
                {
                    widgettype = sampleinfo.OriginalType;
                }
                if (widgettype != null)
                {
                    var docwin = DockPane.DockPanel.AllContent.OfType<xwtsample>().FirstOrDefault();

                    if (docwin != null)
                    {
                        if (docwin.Widget.GetType() != widgettype)
                        {
                            var widget = (Widget)Activator.CreateInstance(widgettype);
                            docwin.Set(widget);
                            return;
                        }
                        else
                        {
                            //todo bring to front
                            return;
                        }
                    }
                    else
                    {
                        var widget = (Widget)Activator.CreateInstance(widgettype);
                        DockPane.DockPanel.Dock(new xwtsample(this, widget));
                        return;
                    }
                }*/
            }
        }
    }
    internal class bb74sample : HPaned, IDockDocument
    {
        Widget IDockContent.Widget => this;
        public virtual string TabText => "BB74-.Sample";

        public Widget Widget1 { get; private set; }
        public Widget Widget2 { get; private set; }
        IDockPane IDockContent.DockPane { get; set; }
        
        public bb74sample(xwtsamples toolbar, Widget w1, Widget w2)
        {
            this.Margin = 0;

            this.Panel1.Content = CreateView();
            this.Panel2.Content = CreateView();

            Set(w1, w2);
        }

        private Widget CreateView()
        {
            var  r =new VBox2();

            r.PackStart(new Label() { ExpandHorizontal = true }, false, true);
            r.PackStart(new FrameBox(), true, true);
            return r;
        }

        internal void Set(Widget w1, Widget w2)
        {
            IDisposable o1 = this.Widget1, o2 = this.Widget2;

            var l1 = (Label)(this.Panel1.Content as VBox2).Children.First();
            var l2 = (Label)(this.Panel2.Content as VBox2).Children.First();
            var f1 = (FrameBox)(this.Panel1.Content as VBox2).Children.Skip(1).First();
            var f2 = (FrameBox)(this.Panel2.Content as VBox2).Children.Skip(1).First();

            l1.Text = "BB74";
            l2.Text = w2.GetType().FullName.StartsWith("Samples") ? "Xwt" : "BB74";

            f1.Content = null;
            f2.Content = null;

            this.Widget1 = w1;
            this.Widget2 = w2;
            f1.Content = w1;
            f2.Content = w2;

            o1?.Dispose();
            o2?.Dispose();
        }
    }
}