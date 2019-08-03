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
                            CheckContentPos();
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
                CheckContentPos();
            }
            public ScrollCanvas(ScrollControl2 owner)
            {
                this.owner = owner;
                this.ExpandVertical = this.ExpandHorizontal = true;
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
            protected override void OnChildPreferredSizeChanged()
            {
                base.OnChildPreferredSizeChanged();
                this.QueueForReallocate();
            }
            /*  protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
              {
                  var s = this.content?.Size ?? new Size(0, 0);

                  var w = widthConstraint.IsConstrained ? Math.Min(widthConstraint.AvailableSize, s.Width):s.Width;
                  var h = heightConstraint.IsConstrained ? Math.Min(heightConstraint.AvailableSize, s.Height):s.Height;

                  return new Size(0, 0);
              }*/
            protected override void OnReallocate()
            {
                base.OnReallocate();
                CheckContentPos();
            }
            private void CheckContentPos()
            {
                this.MoveContent();
                this.owner.CheckScroll();
            }
            internal void MoveContent()
            {
                var s = (Size)this.content.GetType().InvokePrivate(this.content, "OnGetPreferredSize", new object[] { (SizeConstraint)this.Size.Width, (SizeConstraint)this.Size.Height });
                // var r = this.content.Surface.GetPlacementInRect(new Rectangle(Point.Zero, s)).Round().WithPositiveSize();
                var r = new Rectangle(-this.owner.HScroll.Scrollbar.Value, -this.owner.VScroll.Scrollbar.Value, s.Width, s.Height);
                this.SetChildBounds(this.content, r);
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
            public virtual bool AutoHide { get; set; } = true;

            protected ScrollInfo(ScrollControl2 owner, Scrollbar scrollbar)
            {
                this.owner = owner;
                this.Scrollbar = scrollbar;
                this.Scrollbar.ValueChanged += Scrollbar_ValueChanged;
                this.Scrollbar.MouseScrolled += MouseScrolled;
            }

            private void MouseScrolled(object s, MouseScrolledEventArgs e)
            {
                this.owner.DoScroll(e.Direction);
            }

            private void Scrollbar_ValueChanged(object sender, EventArgs e)
            {
                this.owner.MoveContent();
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
                    if (this.AutoHide && !this.Visible)
                    {
                        this.Visible = true;
                    }
                    this.Scrollbar.UpperValue = v-m;
                }
                this.Scrollbar.ClampPage(this.Scrollbar.LowerValue,this.Scrollbar.UpperValue);
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
            get => this.container.Content;
            set => this.container.Content = value;
        }

        public Size ViewSize => this.container.Size;

        public HScrollInfo HScroll { get; }
        public VScrollInfo VScroll { get; }
        private ScrollCanvas container;
        private readonly Size scrollsize;

        public ScrollControl2()
        {
            this.HScroll = new HScrollInfo(this);
            this.VScroll = new VScrollInfo(this);
            this.container = new ScrollCanvas(this);

            this.DefaultColumnSpacing = this.DefaultRowSpacing = 0;
            this.HorizontalPlacement = this.VerticalPlacement = WidgetPlacement.Fill;
            this.Margin = 0;

            this.Add(this.container, 0, 0, hexpand: true, vexpand: true);
            this.Add(this.HScroll, 0, 1, hexpand: true, vexpand: false);
            this.Add(this.VScroll, 1, 0, hexpand: false, vexpand: true);

            this.scrollsize = new Size(this.VScroll.Scrollbar.Surface.GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained).Width, this.HScroll.Scrollbar.Surface.GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained).Height);
        }
        protected void CheckScroll()
        {
            var s = this.Size;
            var w = this.container.Content?.Size.Width ?? 0;
            var h = this.container.Content?.Size.Height ?? 0;

            if (h > s.Height && (this.VScroll.Visible || this.VScroll.AutoHide))
            {
                this.VScroll.Visible = true;
                if (w > s.Width - scrollsize.Width)
                {
                    if (this.HScroll.Visible || this.HScroll.AutoHide)
                    {
                        this.HScroll.Update(w, s.Width - scrollsize.Width);
                        this.VScroll.Update(h, s.Height - scrollsize.Height);
                    }
                    else if (this.HScroll.Visible)
                    {
                        this.HScroll.Clear();
                        this.VScroll.Update(h, s.Height - scrollsize.Height);
                    }
                    else
                    {
                        this.VScroll.Update(h, s.Height);
                    }
                }
                else
                {
                    if (!this.HScroll.Visible || this.HScroll.AutoHide)
                    {
                        this.HScroll.Clear();
                        this.VScroll.Update(h, this.container.Size.Height - scrollsize.Height);
                    }
                    else
                    {
                        this.HScroll.Clear();
                        this.VScroll.Update(h, this.container.Size.Height - scrollsize.Height);
                    }
                }
            }
            else if (w > s.Width && (this.HScroll.Visible || this.HScroll.AutoHide))
            {
                this.HScroll.Visible = true;
                if (h > s.Height - scrollsize.Height)
                {
                    if (this.VScroll.Visible || this.VScroll.AutoHide)
                    {
                        this.VScroll.Update(h, s.Height - scrollsize.Height);
                        this.HScroll.Update(w, s.Width - scrollsize.Width);
                    }
                    else if (this.VScroll.Visible)
                    {
                        this.VScroll.Clear();
                        this.HScroll.Update(w, s.Width - scrollsize.Width);
                    }
                    else
                    {
                        this.HScroll.Update(w, s.Width);
                    }
                }
                else
                {
                    if (!this.VScroll.Visible || this.VScroll.AutoHide)
                    {
                        this.VScroll.Clear();
                        this.HScroll.Update(w, this.container.Size.Width - scrollsize.Width);
                    }
                    else
                    {
                        this.VScroll.Clear();
                        this.HScroll.Update(w, this.container.Size.Width - scrollsize.Width);
                    }
                }
            }
            else
            {
                this.HScroll.Clear();
                this.VScroll.Clear();
            }
            this.QueueForReallocate();
        }
        protected void MoveContent()
        {
            this.container.MoveContent();
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
            this.container.MoveContent();
        }
    }
}
