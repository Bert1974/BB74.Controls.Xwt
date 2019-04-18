using System;
using System.Collections.Generic;
using System.Linq;
using Xwt;
using Xwt.Drawing;

namespace BaseLib.Xwt.Controls.DockPanel.Internals
{
    using Xwt = global::Xwt;

    partial class TitleBar : Canvas
    {
        public static int TitleBarHeight { get; set; } = 24;
        private static int TitleBarButtonSpacing { get; } = 8;
        
        public static TitleBar CreateHeader(DockPane dockPane)
        {
            return new TitleBar(dockPane, true);
        }
        public static TitleBar CreateTabs(DockPane dockPane)
        {
            return new TitleBar(dockPane, false);
        }

        private IEnumerable<IDockContent> docsvis
        {
            get
            {
                if (!IsInfo && IsHeader)
                {
                    if (this.pane.Document is IDockToolbar)
                    {
                        yield return this.pane.Document;
                    }
                    foreach (var doc in this.pane.Documents.OfType<IDockDocument>())
                    {
                        yield return doc;
                    }
                }
                else
                {
                    foreach (var doc in this.pane.Documents.OfType<IDockToolbar>())
                    {
                        yield return doc;
                    }
                }
            }
        }
        private DockPane pane;
        private bool IsInfo = false, IsHeader;
        private Buttons buttons;
        private ScrollButtons scrollwindow;
        private IDockContent[] docs=new IDockContent[0];

        private TitleBar(DockPane pane, bool isheader)
        {
            TitleBar.SmallFont = TitleBar.SmallFont ?? Xwt.Drawing.Font.SystemFont.WithSize(6);

            this.IsHeader = isheader;
            this.pane = pane;
            this.MinHeight = HeightRequest = TitleBar.TitleBarHeight;
            this.BackgroundColor = DockPanel.TitlebarColor;

            this.buttons = new Buttons(this);
    //        this.buttons.ExpandHorizontal = false;
      //      this.buttons.ExpandVertical = true;
            this.AddChild(this.buttons);

            this.scrollwindow = new ScrollButtons(this);
            this.AddChild(this.scrollwindow);
        }
        protected override void OnBoundsChanged()
        {
            base.OnBoundsChanged();
            CheckBounds();
        }
        internal void CheckBounds()
        { 
            var buttonsize = this.buttons.Width;// GetBackend().GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);
            var scrollsize = this.scrollwindow.WindowSize;//.GetBackend().GetPreferredSize(SizeConstraint.Unconstrained,SizeConstraint.Unconstrained);

            bool overflow = buttonsize + scrollsize.Width > this.Bounds.Width && this.docs.Count() > 1;

            if (overflow)
            {
                double x = this.Bounds.Width - scrollsize.Width;

                if (x < 0) // only dropdown menu
                {
                    this.buttons.Visible = false;
                    this.SetChildBounds(this.scrollwindow, new Rectangle(Point.Zero, this.Bounds.Size));
                    this.scrollwindow.Visible = true;
                }
                else // buttons and menu
                {
                    this.SetChildBounds(this.buttons, new Rectangle(Point.Zero, new Size(x, TitleBar.TitleBarHeight)));
                    this.SetChildBounds(this.scrollwindow, new Rectangle(new Point(x, (TitleBar.TitleBarHeight - scrollsize.Height)/2), scrollsize));
                    this.buttons.Visible = true;
                    this.scrollwindow.Visible = true;
                }
            }
            else // buttons only
            {
                this.scrollwindow.Visible = false;
                this.SetChildBounds(this.buttons, new Rectangle(Point.Zero,this.Bounds.Size));
                this.buttons.Visible = true;
            }
        }
        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);

            ctx.SetColor(this.BackgroundColor);
            ctx.Rectangle(this.Bounds);
            ctx.Fill();

      //      ctx.DrawTextLayout(new TextLayout(this) { Text = pane.Document?.TabText ?? "-" }, 0, 0);
        }

        internal void SetDocuments(List<IDockContent> docs)
        {
         //   this.QueueDraw();

            this.docs = docs.ToArray();
            this.scrollwindow.SetDocuments(this.docs);
            this.buttons.SetDocuments(this.docsvis.ToArray());

            if (Bounds.Width > 1 && Bounds.Height > 1)
            {
                CheckBounds();
            }
            if (this.Visible != this.docsvis.Any())
            {
                this.Visible = this.docsvis.Any();
                if (Bounds.Width > 1 && Bounds.Height > 1)
                {
                    this.pane.MoveWindows();
                }
            }
        }
    }
}
