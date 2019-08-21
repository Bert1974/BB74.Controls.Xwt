using BaseLib.Xwt.Controls;
using BaseLib.Xwt.Controls.DockPanel;
using System;
using System.ComponentModel;
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
        private new readonly TreeStore store;
        private readonly TreeView samplesTree;
        private readonly Image icon;

        private struct sampleinfo
        {
            public Type Type, OriginalType;
        }
        public bb74xwtsamples(WindowFrame mainwin)
            : base(mainwin)
        {
            using (var ms = new MemoryStream(DockExample.Properties.Resources.document_generic))
            {
                icon = Image.FromStream(ms);
            }
            store = new TreeStore(nameCol, iconCol, widgetCol);
            samplesTree = new TreeView() { ExpandHorizontal = true, ExpandVertical = true };
            samplesTree.Columns.Add("Name", iconCol, nameCol);

        }
        private TreePosition AddSample(TreePosition pos, string name, Type sampletype, string orgsampletype)
        {
            var t = !string.IsNullOrEmpty(orgsampletype) ? base.a.GetType(orgsampletype) : null;
            var sampleinfo = new sampleinfo() { Type = sampletype, OriginalType = t };
            return store.AddNode(pos).SetValue(nameCol, name).SetValue(iconCol, icon).SetValue(widgetCol, sampleinfo).CurrentPosition;
        }
        protected override TreeView FillTree(WindowFrame mainwwindowxwt, out TreeStore store)
        {
            var w = AddSample(null, "Widgets", null, null);
            AddSample(w, "Scroll View", typeof(ScrollWindowSample), "Samples.ScrollWindowSample");

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

                Type widgettype = null;

                if (sampleinfo.Type != null)
                {
                    if (sampleinfo.OriginalType != null)
                    {
                        var docwin = DockPane.DockPanel.AllContent.OfType<bb74sample>().FirstOrDefault();
                        // dual view
                        var w1 = (Widget)Activator.CreateInstance(sampleinfo.Type);
                        var w2 = (Widget)Activator.CreateInstance(sampleinfo.OriginalType);

                        if (docwin == null)
                        {
                            DockPane.DockPanel.Dock(new bb74sample(this, w1, w2));
                        }
                        else
                        {
                            docwin.Set(w1, w2);
                        }
                        return;
                    }
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
                }
            }
        }
    }
    internal class bb74sample : HBox2, IDockDocument
    {
        Widget IDockContent.Widget => this;
        public virtual string TabText => "BB74-.Sample";

        public Widget Widget1 { get; private set; }
        public Widget Widget2 { get; private set; }
        IDockPane IDockContent.DockPane { get; set; }
        
        public bb74sample(xwtsamples toolbar, Widget w1, Widget w2)
        {
            this.Spacing = 0;
            Set(w1, w2);
        }

        internal void Set(Widget w1, Widget w2)
        {
            IDisposable o1 = this.Widget1, o2 = this.Widget2;
            this.Clear();
            this.Widget1 = w1;
            this.Widget2 = w2;
            this.PackStart(w1, true);
            this.PackStart(w2, true);
            o1?.Dispose();
            o2?.Dispose();
        }
    }
}