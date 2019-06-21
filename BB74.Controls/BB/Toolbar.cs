using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Xwt;
using Xwt.Backends;
using Xwt.Drawing;

namespace BaseLib.Xwt.Controls
{
    public class Toolbar : HBox
    {
        private readonly ToolbarImpl impl;
        private float spacing = 6;

        public Toolbar()
        {
            this.impl = new ToolbarImpl(this);
        }
        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            return this.impl.OnGetPreferredSize(widthConstraint, heightConstraint);
        }
        protected override void OnReallocate()
        {
            this.impl.OnReallocate();
        }

        /*    public void AddButton(Command command)
            {
                AddButton(NewButton(command.Label, command, null, null));
            }
            public void AddButton(Command command, EventHandler clickfunc)
            {
                AddButton(NewButton(command.Label, command, null, null));
            }*/
        public void Add(Widget widget)
        {
            this.impl.AddControl(widget);
        }
        protected class ToolbarImpl
        {
            private readonly Toolbar owner;
            private readonly Orientation direction;
            private readonly List<Widget> buttons = new List<Widget>();

            private bool needslayout;

            public ToolbarImpl(Toolbar toolbar)
            {
                this.owner = toolbar;
            }

            IBoxBackend Backend
            {
                get { return (IBoxBackend)this.owner.GetBackend(); }
            }
            public void AddControl(Widget button)
            {
                this.buttons.Add(button);
                this.owner.PackStart(button);
            }

            public Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
            {
                Size s = new Size();
                int count = 0;
                double[] nextsize = null;
                double spacing = this.owner.Spacing;

                // If the width is constrained then we have a total width, and we can calculate the exact width assigned to each child.
                // We can then use that width as a width constraint for the child.

                if (widthConstraint.IsConstrained)
                {
                    nextsize = CalcDefaultSizes(widthConstraint, heightConstraint, false); // Calculates the width assigned to each child
                }
                for (var nit = 0; nit < this.buttons.Count; nit++)
                {
                    // Use the calculated width if available
                    var wsize = this.buttons[nit].GetBackend().GetPreferredSize(widthConstraint.IsConstrained ? SizeConstraint.WithSize(nextsize[nit]) : SizeConstraint.Unconstrained, heightConstraint);
                    s.Width += wsize.Width;
                    if (wsize.Height > s.Height)
                        s.Height = wsize.Height;
                    count++;
                }
                if (count > 0)
                    s.Width += spacing * (double)(count - 1);
                return s;
            }
            public void OnReallocate()
            {
                var size = this.owner.GetBackend().Size;

                double spacing = this.owner.Spacing;

                List<IWidgetBackend> widgets = new List<IWidgetBackend>();
                List<Rectangle> rects = new List<Rectangle>();

                if (size.Width <= 0 || size.Height <= 0)
                {
                    var ws = this.buttons.Select(bp => bp.GetBackend()).ToArray();
                    this.Backend.SetAllocation(ws, new Rectangle[this.buttons.Count]);
                    return;
                }
                else
                {
                    var nextsize = CalcDefaultSizes(size.Width, size.Height, true);
                    double xs = 0;
                    double xe = size.Width + spacing;
                    for (int n = 0; n < this.buttons.Count; n++)
                    {
                        if (xs + nextsize[n] >= size.Width)
                        {
                            this.buttons[n].Visible = false;
                        }
                        else
                        {
                            this.buttons[n].Visible = true;
                            widgets.Add(this.buttons[n].GetBackend());
                            double availableWidth = nextsize[n] >= 0 ? nextsize[n] : 0;
                            var slot = new Rectangle(xs, 0, availableWidth, size.Height);
                            rects.Add(this.buttons[n].Surface.GetPlacementInRect(slot).Round().WithPositiveSize());

                            xs += availableWidth + spacing;
                        }
                    }
                }
                Backend.SetAllocation(widgets.ToArray(), rects.ToArray());
            }
            double[] CalcDefaultSizes(SizeConstraint width, SizeConstraint height, bool allowShrink)
            {
                double requiredSize = 0, totminsize = 0;
                double availableSize = width.AvailableSize;
                double spacing = this.owner.Spacing;

                var widthConstraint = SizeConstraint.Unconstrained;
                var heightConstraint = height;

                //  var sizes = new Dictionary<BoxPlacement, double>();

                var mw = new double[this.buttons.Count];
                var ww = new double[this.buttons.Count];
                var hh = 0.0;

                int nexpands = 0;

                // Get the natural size of each child
                for (int nit = 0; nit < this.buttons.Count; nit++)
                {
                    var b = this.buttons[nit];
                    var s = (Size)b.GetBackend().GetPreferredSize(widthConstraint, heightConstraint);
                    mw[nit] = MinWidth(b, s);
                    ww[nit] = s.Width;
                    hh = Math.Max(s.Height, hh);
                    requiredSize += ww[nit];
                    totminsize += mw[nit];

                    if (b.ExpandHorizontal)
                        nexpands++;
                }
                double remaining = availableSize - totminsize - (spacing * (double)(this.buttons.Count - 1));
                if (remaining > 0)
                {
                    var remaining2 = availableSize - requiredSize - (spacing * (double)(this.buttons.Count - 1));
                    if (remaining2 > 0)
                    {
                        var expandRemaining = new SizeSplitter(remaining2 - (requiredSize - totminsize), nexpands);
                        for (int nit = 0; nit < this.buttons.Count; nit++)
                        {
                            ww[nit] = mw[nit];
                            if (this.buttons[nit].ExpandHorizontal)
                            {
                                ww[nit] += expandRemaining.NextSizePart();
                                //todo getprefsize
                            }
                        }
                    }
                    else if (remaining2 < 0)
                    {
                        var dd = remaining - (requiredSize - totminsize);
                        var expandRemaining = new SizeSplitter((requiredSize - totminsize), this.buttons.Count);
                        for (int nit = 0; nit < this.buttons.Count; nit++)
                        {
                            ww[nit] = mw[nit] + dd * (ww[nit] - mw[nit]) / (requiredSize - totminsize);
                            if (this.buttons[nit].ExpandHorizontal)
                            {
                                ww[nit] += expandRemaining.NextSizePart();
                            }
                            //if (ExpandsForOrientation(bp.Child))
                            //    nextsize[bp] = minsize[bp]+expandRemaining.NextSizePart();
                        }
                    }
                }
                else //if (/*allowShrink &&*/ remaining < 0)
                {
                    for (int nit = 0; nit < this.buttons.Count; nit++)
                    {
                        ww[nit] = mw[nit];
                    }
                }
                return ww.ToArray();
            }
            private double MinWidth(Widget w, Size s)
            {
                var r = s.Width;

                if (w.MinWidth > 0)
                {
                    r = w.MinWidth;
                }
                if (w.WidthRequest > 0)
                {
                    r = w.WidthRequest;
                }
                return r;
            }

            private double MinHeight(Widget w, Size s)
            {
                var r = s.Height;

                if (w.MinHeight > 0)
                {
                    r = w.MinHeight;
                }
                if (w.HeightRequest > 0)
                {
                    r = w.HeightRequest;
                }
                return r;
            }
        }
    }
}