using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xwt;
using Xwt.Backends;
using Xwt.Drawing;

namespace BaseLib.Xwt.Controls
{
    public class ScrollControl2 : Table
    {
        protected sealed class ScrollCanvas : Canvas
        {
            private Widget content = null;
            private readonly ScrollControl2 owner;

            public new Widget Content
            {
                get => this.content;
                set
                {
                    if (!object.ReferenceEquals(this.content, value))
                    {
                        if (this.content != null)
                        {
                            this.content.BoundsChanged -= Content_BoundsChanged;
                            this.content.MouseScrolled -= Content_MouseScrolled;
                            this.RemoveChild(this.content);
                        }
                        if ((this.content = value) != null)
                        {
                            this.AddChild(this.content);
                            this.content.BoundsChanged += Content_BoundsChanged;
                            this.content.MouseScrolled += Content_MouseScrolled;
                            this.owner.CheckContentPos();

                            this.owner.container.CalcMinSize();
                        }
                    }
                }
            }

            private void Content_MouseScrolled(object sender, MouseScrolledEventArgs e)
            {
                this.owner.DoScroll(e.Direction);
                e.Handled = true;
            }

            private void Content_BoundsChanged(object sender, EventArgs e)
            {
                this.MoveContent();
                //   this.owner.frame.CheckContentPos();
                //this.owner.OnViewSizeChanged();
            }
            public ScrollCanvas(ScrollControl2 owner)
            {
                base.BackgroundColor = Colors.White;
                this.owner = owner;
                this.MouseScrolled += (s, a) => { this.owner.DoScroll(a.Direction); a.Handled = true; };
                this.ClipToBounds();
            }
            protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
            {
                return this.owner.ViewSize;
            }
            protected override bool SupportsCustomScrolling => base.SupportsCustomScrolling;
            internal void MoveContent()
            {
                var scrollpt = new Point(-this.owner.HScroll.Scrollbar.Value, -this.owner.VScroll.Scrollbar.Value);
                var s = (Size)this.content.GetType().InvokePrivate(this.content, "OnGetPreferredSize", new object[] { (SizeConstraint)this.Size.Width, (SizeConstraint)this.Size.Height });
                var vs = this.owner.ViewSize;

                if (s.Width < vs.Width && this.content.ExpandHorizontal)
                {
                    s.Width = vs.Width;
                }
                if (s.Height < vs.Height && this.content.ExpandVertical)
                {
                    s.Height = vs.Height;
                }

                var r = new Rectangle(scrollpt.X, scrollpt.Y, s.Width, s.Height);
                this.SetChildBounds(this.content, r);
            }
        }

        protected void OnViewSizeChanged()
        {
            this.ViewSizeChanged?.Invoke(this, EventArgs.Empty);
        }

        protected sealed class ContainerCanvas : Canvas
        {
            private readonly ScrollControl2 owner;

            public ContainerCanvas(ScrollControl2 owner)
            {
                this.owner = owner;
                this.HorizontalPlacement = this.VerticalPlacement = WidgetPlacement.Fill;
                this.ClipToBounds();
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.Content = null;
                }
                base.Dispose(disposing);
            }
            protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
            {
                return new Size(0, 0);
            }
            protected override void OnBoundsChanged()
            {
                base.OnBoundsChanged();
                base.SetChildBounds(this.Children.First(), base.Bounds);
                //    this.owner.CheckContentPos();
                //  this.owner.CheckScroll();
            }

            internal void CalcMinSize()
            {

            }
        }

        public abstract class ScrollInfo
        {
            protected ScrollControl2 owner;

            public abstract Orientation Directon { get; }
            internal Scrollbar Scrollbar { get; }

            public virtual bool Visible
            {
                get => this.Scrollbar.Visible;
                set => this.Scrollbar.Visible = value;
            }
            public /*virtual*/ bool AutoHide { get; set; } = true;

            protected ScrollInfo(ScrollControl2 owner, Scrollbar scrollbar)
            {
                this.owner = owner;
                this.Scrollbar = scrollbar;
                this.Scrollbar.ValueChanged += Scrollbar_ValueChanged;
                this.Scrollbar.MouseScrolled += MouseScrolled;
                this.AutoHide = true;
            }

            private void MouseScrolled(object s, MouseScrolledEventArgs e)
            {
                this.owner.DoScroll(e.Direction);
            }

            private void Scrollbar_ValueChanged(object sender, EventArgs e)
            {
                this.owner.ScrollChanged();
            }

            public static implicit operator Widget(ScrollInfo scroll) => scroll.Scrollbar;

            internal void Update(double v, double m)
            {
                this.Scrollbar.PageSize = 1;
                this.Scrollbar.LowerValue = 0;
                this.Scrollbar.PageIncrement = m;
                this.Scrollbar.StepIncrement = 16;
                if (v <= m)
                {
                    this.Scrollbar.UpperValue = 0;
                    if (this.AutoHide && this.Visible)
                    {
                        this.Visible = false;
                    }
                }
                else
                {
                    this.Scrollbar.UpperValue = v - m;
                    if (this.AutoHide && !this.Visible)
                    {
                        this.Visible = true;
                    }
                }
                this.Scrollbar.ClampPage(this.Scrollbar.LowerValue, this.Scrollbar.UpperValue);
            }
            internal void Clear()
            {
                this.Scrollbar.UpperValue = 0;
                this.Scrollbar.ClampPage(this.Scrollbar.LowerValue, this.Scrollbar.UpperValue);
                if (this.AutoHide && this.Visible)
                {
                    this.Visible = false;
                }
            }
        }

        public sealed class VScrollInfo : ScrollInfo
        {
            public VScrollInfo(ScrollControl2 owner)
                : base(owner, new VScrollbar())
            {
            }
            public override Orientation Directon => Orientation.Vertical;
            public double Width => this.owner.scrollsize.Width;

        }
        public sealed class HScrollInfo : ScrollInfo
        {
            public HScrollInfo(ScrollControl2 owner)
                : base(owner, new HScrollbar())
            {
            }
            public override Orientation Directon => Orientation.Horizontal;
            public double Height => this.owner.scrollsize.Height;
        }

        public new Widget Content
        {
            get => this.contentholder.Content;
            set
            {
                this.contentholder.Content = value; //SupportsCustomScrolling
            }
        }
        public Size ViewSize
        {
            get
            {
                if (this.Content != null)
                {
                    // var contentsize = this.contentholder.GetChildBounds(this.Content);
                    var contentsize = this.Content.Surface.GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);
                  //  var contentsize2 = this.Content.GetType().InvokePrivate(this.Content, "OnGetPreferredSize", new object[] { SizeConstraint.Unconstrained, SizeConstraint.Unconstrained });

                    var nv = new double[] { this.Size.Width, this.Size.Height };
                    var ovis = new bool[] { this.HScroll.Visible, this.VScroll.Visible };

                    if (contentsize.Height > nv[1] && (ovis[1] || this.VScroll.AutoHide)) // vertical scroll needed?
                    {
                        nv[0] -= scrollsize.Width;

                        if (contentsize.Width > nv[0]) // horizontal scroll needed?
                        {
                            if (ovis[0] || this.HScroll.AutoHide) // horizontal scroll can show?
                            {
                                nv[1] -= scrollsize.Height;
                            }
                        }
                        else // no horizontal scroll
                        {
                            if (ovis[0] && !this.HScroll.AutoHide) // can hscroll hide?
                            {
                                nv[1] -= scrollsize.Height;
                            }
                        }
                    }
                    else if (contentsize.Width > nv[0] && (ovis[0] || this.HScroll.AutoHide)) // horizontal scroll needed?
                    {
                        nv[1] -= scrollsize.Height;

                        if (contentsize.Height > nv[1]) // vertical scroll needed?
                        {
                            if (ovis[1] || this.VScroll.AutoHide) // can vscroll show?
                            {
                                nv[0] -= scrollsize.Width;
                            }
                        }
                        else
                        {
                            if (ovis[1] && !this.VScroll.AutoHide)
                            {
                                nv[0] -= scrollsize.Width;
                            }
                        }
                    }
                    else // no scrollbars
                    {
                        if (this.HScroll.Visible && !this.HScroll.AutoHide)
                        {
                            nv[1] -= scrollsize.Height;
                        }
                        if (this.VScroll.Visible && !this.VScroll.AutoHide)
                        {
                            nv[0] -= scrollsize.Width;
                        }
                    }
                    return new Size(Math.Max(0, nv[0]), Math.Max(0, nv[1]));
                }
                return Size.Zero;
            }
        }
        public HScrollInfo HScroll { get; }
        public VScrollInfo VScroll { get; }

        public ScrollPolicy HorizontalScrollPolicy
        {
            get
            {
                return GetScrollPolicy(this.HScroll);
            }
            set
            {
                SetScrollPolicy(this.HScroll, value);
            }
        }
        public ScrollPolicy VerticalScrollPolicy
        {
            get
            {
                return GetScrollPolicy(this.VScroll);
            }
            set
            {
                SetScrollPolicy(this.VScroll, value);
            }
        }

        public bool BorderVisible { get; set; } // unimplemnted

        private ScrollPolicy GetScrollPolicy(ScrollInfo c)
        {
            if (c.AutoHide)
            {
                return ScrollPolicy.Automatic;
            }
            else if (c.Visible)
            {
                return ScrollPolicy.Always;
            }
            return ScrollPolicy.Never;
        }

        private void SetScrollPolicy(ScrollInfo c, ScrollPolicy p)
        {
            if (p == ScrollPolicy.Automatic)
            {
                c.AutoHide = true;
            }
            else
            {
                c.AutoHide = false;
                c.Visible = p == ScrollPolicy.Always ? true : false;
            }
        }

        private ScrollCanvas contentholder;
        private readonly ContainerCanvas container;
        private readonly Size scrollsize;

        public event EventHandler ViewSizeChanged;

        public ScrollControl2()
        {
            this.HScroll = new HScrollInfo(this);
            this.VScroll = new VScrollInfo(this);

            this.container = new ContainerCanvas(this);

            this.contentholder = new ScrollCanvas(this);
            this.container.AddChild(this.contentholder);

            this.DefaultColumnSpacing = this.DefaultRowSpacing = 0;
            // this.HorizontalPlacement = this.VerticalPlacement = WidgetPlacement.Fill;
            this.Margin = 0;

            this.Add(this.container, 0, 0, hexpand: true, vexpand: true);
            this.Add(this.HScroll, 0, 1, hexpand: false, vexpand: false);
            this.Add(this.VScroll, 1, 0, hexpand: false, vexpand: false);

            var s1 = this.VScroll.Scrollbar.Surface.GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);
            var s2 = this.HScroll.Scrollbar.Surface.GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);

            this.scrollsize = new Size(s1.Width,s2.Height );

            this.MinWidth = Math.Max(s1.Width, s2.Width);
            this.MinHeight = Math.Max(s1.Height, s2.Height);
        }
        protected override void OnBoundsChanged()
        {
            base.OnBoundsChanged();
            this.CheckScroll(true);
            this.contentholder.MoveContent();
            //     CheckScroll(true);
            //  contentholder.MoveContent();
            //     this.ViewSizeChanged?.Invoke(this, EventArgs.Empty);

            //  this.ViewSizeChanged?.Invoke(this, EventArgs.Empty);
        }
        protected void CheckScroll(bool force = false)
        {
            var contentsize = (Size)this.Content.GetType().InvokePrivate(this.Content, "OnGetPreferredSize", new object[] { SizeConstraint.Unconstrained, SizeConstraint.Unconstrained });
            //var contentsize = this.contentholder.GetChildBounds(this.Content);
            var viewsize = this.ViewSize;

            bool hvis = this.HScroll.Visible, vvis = this.VScroll.Visible;

            this.HScroll.Update(contentsize.Width, viewsize.Width);
            this.VScroll.Update(contentsize.Height, viewsize.Height);

            if (force || hvis != this.HScroll.Visible || vvis != this.VScroll.Visible)
            {
                this.OnViewSizeChanged();
          //      this.ViewSizeChanged?.Invoke(this, EventArgs.Empty);
            }

            this.QueueForReallocate();
        }
        protected void ScrollChanged()
        {
            this.contentholder.MoveContent();
        }
        protected virtual void DoScroll(ScrollDirection direction)
        {
            switch (direction)
            {
                case ScrollDirection.Left:
                    this.HScroll.Scrollbar.Value = Math.Max(this.HScroll.Scrollbar.LowerValue, this.HScroll.Scrollbar.Value - this.HScroll.Scrollbar.StepIncrement);
                    break;
                case ScrollDirection.Right:
                    this.HScroll.Scrollbar.Value = Math.Min(this.HScroll.Scrollbar.UpperValue, this.HScroll.Scrollbar.Value + this.HScroll.Scrollbar.StepIncrement);
                    break;
                case ScrollDirection.Up:
                    this.VScroll.Scrollbar.Value = Math.Max(this.VScroll.Scrollbar.LowerValue, this.VScroll.Scrollbar.Value - this.VScroll.Scrollbar.StepIncrement);
                    break;
                case ScrollDirection.Down:
                    this.VScroll.Scrollbar.Value = Math.Min(this.VScroll.Scrollbar.UpperValue, this.VScroll.Scrollbar.Value + this.VScroll.Scrollbar.StepIncrement);
                    break;
            }
            this.contentholder.MoveContent();
        }
        public void Refresh()
        {
            this.CheckContentPos();
        }
        internal void CheckContentPos()
        {
            this.CheckScroll();
            this.contentholder.MoveContent();
        }
  /*      protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            var size= this.Content?.Surface.GetPreferredSize(widthConstraint, heightConstraint) ?? Size.Zero;// base.OnGetPreferredSize(widthConstraint, heightConstraint);

            return new Size(widthConstraint.IsConstrained ? Math.Min(widthConstraint.AvailableSize, size.Width) : size.Width, heightConstraint.IsConstrained ? Math.Min(heightConstraint.AvailableSize, size.Height) : size.Height);
        }*/
    }
}
