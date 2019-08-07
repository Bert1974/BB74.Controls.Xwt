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
            }

            private void Content_BoundsChanged(object sender, EventArgs e)
            {
                //this.MoveContent();
                //   this.owner.frame.CheckContentPos();
            }
            public ScrollCanvas(ScrollControl2 owner)
            {
                base.BackgroundColor = owner.BackgroundColor;
                this.owner = owner;
                this.ClipToBounds();
            }
            protected override void OnBoundsChanged()
            {
                base.OnBoundsChanged();
                this.owner.CheckScroll();

                Console.WriteLine($"new frme bounds={this.Bounds}");
            }
            protected override void OnChildPlacementChanged(Widget child)
            {
                Console.WriteLine($"new child placment={this.Bounds}");
                base.OnChildPlacementChanged(child);
            }
            protected override void OnChildPreferredSizeChanged()
            {
                Console.WriteLine($"new child prefsize={this.Bounds}");
                base.OnChildPreferredSizeChanged();

                //   this.MinWidth = this.content?.Size.Width ?? 0;
                //    this.MinHeight = this.content?.Size.Height ?? 0;

                //   this.MoveContent();
            }
            protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
            {
                // this.owner.CheckScroll();
                return this.owner.ViewSize;
                var s = this.content?.Size ?? new Size(0, 0);

                var w = widthConstraint.IsConstrained ? Math.Min(widthConstraint.AvailableSize, s.Width) : s.Width;
                var h = heightConstraint.IsConstrained ? Math.Min(heightConstraint.AvailableSize, s.Height) : s.Height;

                return s; //base.OnGetPreferredSize(widthConstraint, heightConstraint);
            }
            protected override bool SupportsCustomScrolling => base.SupportsCustomScrolling;
            internal void MoveContent()
            {
                var scrollpt = new Point(-this.owner.HScroll.Scrollbar.Value, -this.owner.VScroll.Scrollbar.Value);
                var s = (Size)this.content.GetType().InvokePrivate(this.content, "OnGetPreferredSize", new object[] { (SizeConstraint)this.Size.Width, (SizeConstraint)this.Size.Height });
                var r = new Rectangle(scrollpt.X, scrollpt.Y, s.Width, s.Height);
                this.SetChildBounds(this.content, r);
            }
        }

        protected sealed class ContainerCanvas : Canvas
        {
            private readonly ScrollControl2 owner;

            public ContainerCanvas(ScrollControl2 owner)
            {
                this.owner = owner;
                // this.ExpandVertical = this.ExpandHorizontal = true;
                this.HorizontalPlacement = this.VerticalPlacement = WidgetPlacement.Fill;
                this.ClipToBounds();
                //   this.MinWidth = this.MinHeight = 10;
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.Content = null;
                }
                base.Dispose(disposing);
            }
            protected override void OnChildPreferredSizeChanged()
            {
                base.OnChildPreferredSizeChanged();
                //    this.QueueForReallocate();
            }
            protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
            {
                return /*owner.container.Surface.GetPreferredSize(widthConstraint, heightConstraint);*/ new Size(0, 0);
            }
            protected override void OnReallocate()
            {
                base.OnReallocate();
                //  CheckContentPos();
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

        }
        public sealed class HScrollInfo : ScrollInfo
        {
            public HScrollInfo(ScrollControl2 owner)
                : base(owner, new HScrollbar())
            {
            }
            public override Orientation Directon => Orientation.Horizontal;

        }

        public new Widget Content
        {
            get => this.contentholder.Content;
            set => this.contentholder.Content = value;
        }

        public Size ViewSize
        {
            get
            {
                if (this.Content != null)
                {
                    // var contentsize = this.contentholder.GetChildBounds(this.Content);
                    var contentsize = (Size)this.Content.GetType().InvokePrivate(this.Content, "OnGetPreferredSize", new object[] { SizeConstraint.Unconstrained, SizeConstraint.Unconstrained });
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
                            nv[0] -= scrollsize.Width;
                        }
                        if (this.VScroll.Visible && !this.VScroll.AutoHide)
                        {
                            nv[1] -= scrollsize.Height;
                        }
                    }
                    return new Size(Math.Max(0, nv[0]), Math.Max(0, nv[1]));
                }
                return Size.Zero;
            }
        }
        public HScrollInfo HScroll { get; }
        public VScrollInfo VScroll { get; }
        private ScrollCanvas contentholder;
        private readonly ContainerCanvas container;
        private readonly Size scrollsize;

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

            this.scrollsize = new Size(
                    this.VScroll.Scrollbar.Surface.GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained).Width,
                    this.HScroll.Scrollbar.Surface.GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained).Height);

            this.MinWidth = this.scrollsize.Width;
            this.MinHeight = this.scrollsize.Height;
        }
        protected override void OnBoundsChanged()
        {
            base.OnBoundsChanged();
            CheckScroll();
            contentholder.MoveContent();
        }
        protected void CheckScroll()
        {
            var contentsize = (Size)this.Content.GetType().InvokePrivate(this.Content, "OnGetPreferredSize", new object[] { SizeConstraint.Unconstrained, SizeConstraint.Unconstrained });
            //var contentsize = this.contentholder.GetChildBounds(this.Content);
            var viewsize = this.ViewSize;

            this.HScroll.Update(contentsize.Width, viewsize.Width);
            this.VScroll.Update(contentsize.Height, viewsize.Height);

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
    }
}
