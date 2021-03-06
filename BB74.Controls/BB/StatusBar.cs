﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Xwt;
using Xwt.Backends;
using Xwt.Drawing;

namespace BaseLib.Xwt.Controls
{
    public class StatusBar : HBox
    {
        private readonly StatusBarImpl impl;

        public StatusBar()
        {
            this.impl = new StatusBarImpl(this);
            this.Spacing = 2;

            this.HorizontalPlacement = WidgetPlacement.Fill;
            this.ExpandHorizontal = true;
        }
        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            return this.impl.OnGetPreferredSize(widthConstraint, heightConstraint);
        }
        protected override void OnReallocate()
        {
            this.impl.OnReallocate();
        }

        /*   public new void Clear()
           {
               foreach (var b in this.Children.ToArray())
               {
                   UnregisterChild(b);
                   (this.GetBackend() as IBoxBackend).Remove((IWidgetBackend)GetBackend(b));
               }
               base.OnPreferredSizeChanged();
           }*/
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
        public void FrameAndAdd(Widget widget)
        {
            this.impl.AddControl(new FrameBox(widget));
        }
    /*    public Button Add(string label, EventHandler clicked)
        {
            var b = new Button(label);
            b.Clicked += clicked;
            this.Add(b);
            return b;
        }
        public Button Add(Command command)
        {
            var b = new Button(command.Label);
            this.Add(b);
            return b;
        }*/
        protected class StatusBarImpl
        {
            private readonly StatusBar owner;
            //     private readonly List<Widget> buttons = new List<Widget>();

            public StatusBarImpl(StatusBar toolbar)
            {
                this.owner = toolbar;
            }

            IBoxBackend Backend
            {
                get { return (IBoxBackend)this.owner.GetBackend(); }
            }
            public void AddControl(Widget button)
            {
                this.owner.PackStart(button);

                CheckMinSize();
            }

            private void CheckMinSize()
            {
                Size ms = Size.Zero;
                for (var nit = 0; nit < this.owner.Placements.Count; nit++)
                {
                    var ws = this.owner.Placements[nit].Child.Surface.GetPreferredSize();
                    ms = new Size(ms.Width + ws.Width, Math.Max(ms.Height, ws.Height));
                }
                this.owner.MinWidth = Math.Max(10, Math.Min(100, ms.Width));
                this.owner.MinHeight = Math.Max(10, ms.Height);
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
                for (var nit = 0; nit < this.owner.Placements.Count; nit++)
                {
                    // Use the calculated width if available
                    var wsize = this.owner.Placements[nit].Child.GetBackend().GetPreferredSize(widthConstraint.IsConstrained ? SizeConstraint.WithSize(nextsize[nit]) : SizeConstraint.Unconstrained, heightConstraint);
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
                    var ws = this.owner.Placements.Select(bp => bp.Child.GetBackend()).ToArray();
                    this.Backend.SetAllocation(ws, new Rectangle[this.owner.Placements.Count]);
                    return;
                }
                else
                {
                    var nextsize = CalcDefaultSizes(size.Width, size.Height, true);
                    double xs = 0;
                    double xe = size.Width + spacing;
                    for (int n = 0; n < this.owner.Placements.Count; n++)
                    {
                        var w = this.owner.Placements[n].Child;
                        if (xs + nextsize[n] >= size.Width)
                        {
                            //     this.buttons[n].Visible = false;
                            widgets.Add(w.GetBackend());
                            rects.Add(Rectangle.Zero);
                        }
                        else
                        {
                            //   this.buttons[n].Visible = true;
                            widgets.Add(w.GetBackend());
                            double availableWidth = nextsize[n] >= 0 ? nextsize[n] : 0;
                            var slot = new Rectangle(xs, 0, availableWidth, size.Height);
                            rects.Add(w.Surface.GetPlacementInRect(slot).Round().WithPositiveSize());

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

                var mw = new double[this.owner.Placements.Count];
                var ww = new double[this.owner.Placements.Count];
                var hh = 0.0;

                int nexpands = 0;

                // Get the natural size of each child
                for (int nit = 0; nit < this.owner.Placements.Count; nit++)
                {
                    var b = this.owner.Placements[nit].Child;
                    var s = (Size)b.GetBackend().GetPreferredSize(widthConstraint, heightConstraint);
                    var s2 = b.Surface.GetPreferredSize(widthConstraint, heightConstraint, false);
                    mw[nit] = MinWidth(b, s);
                    ww[nit] = s.Width;
                    hh = Math.Max(s.Height, hh);
                    requiredSize += ww[nit];
                    totminsize += mw[nit];

                    if (b.ExpandHorizontal)
                        nexpands++;
                }
                double remaining = availableSize - totminsize - (spacing * (double)(this.owner.Placements.Count - 1));
                if (remaining > 0)
                {
                    var remaining2 = availableSize - requiredSize - (spacing * (double)(this.owner.Placements.Count - 1));
                    if (remaining2 > 0)
                    {
                        var expandRemaining = new SizeSplitter(remaining2 - (requiredSize - totminsize), nexpands);
                        for (int nit = 0; nit < this.owner.Placements.Count; nit++)
                        {
                            var b = this.owner.Placements[nit].Child;
                            ww[nit] = mw[nit];
                            if (b.ExpandHorizontal)
                            {
                                ww[nit] += expandRemaining.NextSizePart();
                                //todo getprefsize
                            }
                        }
                    }
                    else if (remaining2 < 0)
                    {
                        var dd = remaining - (requiredSize - totminsize);
                        var expandRemaining = new SizeSplitter((requiredSize - totminsize), this.owner.Placements.Count);
                        for (int nit = 0; nit < this.owner.Placements.Count; nit++)
                        {
                            var b = this.owner.Placements[nit].Child;
                            ww[nit] = mw[nit] + dd * (ww[nit] - mw[nit]) / (requiredSize - totminsize);
                            if (b.ExpandHorizontal)
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
                    for (int nit = 0; nit < this.owner.Placements.Count; nit++)
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