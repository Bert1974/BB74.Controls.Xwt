using BaseLib.Xwt.Controls.DockPanel;
using System;
using Xwt;
using Xwt.Drawing;

namespace DockExample
{
    partial class mainwindow
    {
        class testwebitem : WebView, IDockDocument, IDockSerializable
        {
            public testwebitem()
            {
                base.Url = "http://www.google.com";
            }

            public testwebitem(DockPanel dockpanel, string data)
            {
                base.Url = data;
            }

            IDockPane IDockContent.DockPane { get; set ; }

            Widget IDockContent.Widget => this;
            string IDockContent.TabText => "Browser";

            string IDockSerializable.Serialize()
            {
                return base.Url;
            }
        }
        class testdockitem : Canvas, IDockDocument, IDockNotify
        {
            Widget IDockContent.Widget => this;
            string IDockContent.TabText => "testdoc";
            IDockPane IDockContent.DockPane { get; set; }

            public testdockitem()
            {
                this.MinWidth = this.MinHeight = 100;
                this.BackgroundColor = Colors.White;
            }
            private void queuedraw(object sender, EventArgs e)
            {
                base.QueueDraw();
            }
            protected override void OnDraw(Context ctx, Rectangle dirtyRect)
            {
                base.OnDraw(ctx, dirtyRect);

                ctx.SetColor(this.BackgroundColor);
                ctx.Rectangle(this.Bounds);
                ctx.Fill();

                var tl = new TextLayout(this) { Text = (this as IDockContent).DockPane?.DockPanel.Dump() };

                ctx.SetColor(Colors.Black);
                ctx.DrawTextLayout(tl, new Point(0, 0));
            }

            void IDockNotify.OnLoaded(IDockPane pane)
            {
                Console.WriteLine($"{base.GetHashCode()} doc onloaded");
            }

            void IDockNotify.OnUnloading()
            {
                Console.WriteLine($"{base.GetHashCode()} doc unloading");
            }
        }
    }
}