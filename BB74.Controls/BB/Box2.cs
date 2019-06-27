using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xwt;
using Xwt.Backends;

namespace BaseLib.Xwt.Controls
{
    public class VBox2 : VBox
    {
        private Box2 box2;

        public VBox2() : base()
        {
            this.box2 = new Box2(this, Orientation.Vertical);
        }
        protected override void OnReallocate()
        {
            this.box2.OnReallocate();
        }
        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            return this.box2.OnGetPreferredSize(widthConstraint, heightConstraint);
        }
        protected override void OnBoundsChanged()
        {
         //   this.box2.BoundsChanged();
            base.OnBoundsChanged();
        }
    }
    public class HBox2 : HBox
    {
        private Box2 box2;

        public HBox2() : base()
        {
            this.box2 = new Box2(this, Orientation.Horizontal);
        }
        protected override void OnReallocate()
        {
            this.box2.OnReallocate();
        }
        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            return this.box2.OnGetPreferredSize(widthConstraint, heightConstraint);
        }
        protected override void OnBoundsChanged()
        {
       //     this.box2.BoundsChanged();
            base.OnBoundsChanged();
        }
    }
    internal class Box2
    {
        private readonly Box owner;
        private readonly Orientation direction;

        IBoxBackend Backend
        {
            get { return (IBoxBackend)this.owner.GetBackend(); }
        }
        public Box2(Box box, Orientation direction)
        {
            this.owner = box;
            this.direction = direction;
        }
        private static object GetFieldValuePrivate(Type type, object instance, string propertyname)
        {
            return type.GetField(propertyname, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField).GetValue(instance);
        }
        /// <summary>
        /// Determines whether this <see cref="Xwt.Widget"/> expands in a specific orientation.
        /// </summary>
        /// <returns><c>true</c>, if the widget expands in the specified orientation, <c>false</c> otherwise.</returns>
        /// <param name="or">Or.</param>
        private bool ExpandsForOrientation(Widget w)
        {
            if (direction == Orientation.Vertical)
                return w.ExpandVertical;
            else
                return w.ExpandHorizontal;
        }
        public void BoundsChanged()
        {
            OnReallocate();
        }
        public Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            var children = (ChildrenCollection<BoxPlacement>)GetFieldValuePrivate(typeof(Box), owner, "children");
            double spacing = owner.Spacing;

            Size s = new Size();
            int count = 0;

            var visibleChildren = children.Where(b => b.Child.Visible).ToArray();

            Dictionary<BoxPlacement, double> nextsize = null;

            if (direction == Orientation.Horizontal)
            {
                // If the width is constrained then we have a total width, and we can calculate the exact width assigned to each child.
                // We can then use that width as a width constraint for the child.

                if (widthConstraint.IsConstrained)
                    nextsize = CalcDefaultSizes(widthConstraint, heightConstraint, false); // Calculates the width assigned to each child

                foreach (var cw in visibleChildren)
                {
                    // Use the calculated width if available
                    var wsize = cw.Child.Surface.GetPreferredSize(widthConstraint.IsConstrained ? nextsize[cw] : SizeConstraint.Unconstrained, heightConstraint, true);
                    s.Width += wsize.Width;
                    if (wsize.Height > s.Height)
                        s.Height = wsize.Height;
                    count++;
                }
                if (count > 0)
                    s.Width += spacing * (double)(count - 1);
            }
            else
            {
                if (heightConstraint.IsConstrained)
                    nextsize = CalcDefaultSizes(widthConstraint, heightConstraint, false);
                foreach (var cw in visibleChildren)
                {
                    var wsize = cw.Child.Surface.GetPreferredSize(widthConstraint, heightConstraint.IsConstrained ? nextsize[cw] : SizeConstraint.Unconstrained, true);
                    s.Height += wsize.Height;
                    if (wsize.Width > s.Width)
                        s.Width = wsize.Width;
                    count++;
                }
                if (count > 0)
                    s.Height += spacing * (double)(count - 1);
            }
            return s;
        }
        public void OnReallocate()
        {
            var size = Backend.Size;

            var placements = (ChildrenCollection<BoxPlacement>)GetFieldValuePrivate(typeof(Box), owner, "children");
            double spacing = owner.Spacing;

            var visibleChildren = placements.Where(c => c.Child.Visible).ToArray();

            IWidgetBackend[] widgets = new IWidgetBackend[visibleChildren.Length];
            Rectangle[] rects = new Rectangle[visibleChildren.Length];

            if (size.Width <= 0 || size.Height <= 0)
            {
                var ws = visibleChildren.Select(bp => bp.Child.GetBackend()).ToArray();
                Backend.SetAllocation(ws, new Rectangle[visibleChildren.Length]);
                return;
            }
            else
            {
                if (direction == Orientation.Horizontal)
                {
                    var nextsize = CalcDefaultSizes(size.Width, size.Height, true);
                    double xs = 0;
                    double xe = size.Width + spacing;
                    for (int n = 0; n < visibleChildren.Length; n++)
                    {
                        var bp = visibleChildren[n];
                        double availableWidth = nextsize[bp] >= 0 ? nextsize[bp] : 0;
                        if (bp.PackOrigin == PackOrigin.End)
                            xe -= availableWidth + spacing;

                        var slot = new Rectangle(bp.PackOrigin == PackOrigin.Start ? xs : xe, 0, availableWidth, size.Height);
                        widgets[n] = bp.Child.GetBackend();
                        rects[n] = bp.Child.Surface.GetPlacementInRect(slot).Round().WithPositiveSize();

                        if (bp.PackOrigin == PackOrigin.Start)
                            xs += availableWidth + spacing;
                    }
                }
                else
                {
                    var nextsize = CalcDefaultSizes(size.Width, size.Height, true);
                    double ys = 0;
                    double ye = size.Height + spacing;
                    for (int n = 0; n < visibleChildren.Length; n++)
                    {
                        var bp = visibleChildren[n];
                        double availableHeight = nextsize[bp] >= 0 ? nextsize[bp] : 0;
                        if (bp.PackOrigin == PackOrigin.End)
                            ye -= availableHeight + spacing;

                        var slot = new Rectangle(0, bp.PackOrigin == PackOrigin.Start ? ys : ye, size.Width, availableHeight);
                        widgets[n] = bp.Child.GetBackend();
                        rects[n] = bp.Child.Surface.GetPlacementInRect(slot).Round().WithPositiveSize();

                        if (bp.PackOrigin == PackOrigin.Start)
                            ys += availableHeight + spacing;
                    }
                }
                Backend.SetAllocation(widgets, rects);
            }
      /*      foreach (var w in visibleChildren.Select(bp => bp.Child))
            {
                w.QueueForReallocate();
            }*/
        }
        Dictionary<BoxPlacement, double> CalcDefaultSizes(SizeConstraint width, SizeConstraint height, bool allowShrink)
        {
            var nextsize = new Dictionary<BoxPlacement, double>();
            var minsize = new Dictionary<BoxPlacement, double>();

            bool vertical = direction == Orientation.Vertical;
            int nexpands = 0;
            double requiredSize = 0, totminsize = 0;
            double availableSize = vertical ? height.AvailableSize : width.AvailableSize;

            var widthConstraint = vertical ? width : SizeConstraint.Unconstrained;
            var heightConstraint = vertical ? SizeConstraint.Unconstrained : height;

            var children = (ChildrenCollection<BoxPlacement>)GetFieldValuePrivate(typeof(Box), owner, "children");
            double spacing = owner.Spacing;

            var visibleChildren = children.Where(b => b.Child.Visible).ToArray();
            //  var sizes = new Dictionary<BoxPlacement, double>();

            // Get the natural size of each child
            foreach (var bp in visibleChildren)
            {
                var s1 = bp.Child.Surface.GetPreferredSize();
             //   var s2 = bp.Child.GetBackend().GetPreferredSize(widthConstraint, heightConstraint);
                var s3 = (Size)bp.Child.GetType().InvokePrivate(bp.Child, "OnGetPreferredSize", new object[] { widthConstraint, heightConstraint });
                var s4 = bp.Child.Size;
                var s =(Size)bp.Child.GetType().InvokePrivate(bp.Child, "OnGetPreferredSize", new object[] { widthConstraint, heightConstraint});
                minsize[bp] = vertical ? MinHeight(bp.Child, s) : MinWidth(bp.Child, s);
                nextsize[bp] = Math.Max(minsize[bp], vertical ? s.Height : s.Width);
                requiredSize += nextsize[bp];
                totminsize += minsize[bp];
                //      sizes[bp] = nextsize[bp];
                if (ExpandsForOrientation(bp.Child))
                    nexpands++;
            }

            double remaining = availableSize - totminsize - (spacing * (double)(visibleChildren.Length - 1));
            if (remaining > 0)
            {
                var remaining2 = availableSize - requiredSize - (spacing * (double)(visibleChildren.Length - 1));
                if (remaining2 > 0)
                {
                    var expandRemaining = new SizeSplitter(remaining2-(requiredSize - totminsize), nexpands);
                    foreach (var bp in visibleChildren)
                    {
                        nextsize[bp] = minsize[bp];
                        if (ExpandsForOrientation(bp.Child))
                        {
                            nextsize[bp] += expandRemaining.NextSizePart();
                            // todo getprefsize
                        }
                    }
                }
                else if (remaining2 < 0)
                {
                    var dd = remaining- (requiredSize - totminsize);
                    var expandRemaining = new SizeSplitter((requiredSize- totminsize), nexpands);
                    foreach (var bp in visibleChildren)
                    {
                        nextsize[bp] = minsize[bp] + dd * (nextsize[bp] - minsize[bp]) / (requiredSize- totminsize);
                        if (ExpandsForOrientation(bp.Child))
                        {
                            nextsize[bp] += expandRemaining.NextSizePart();
                        }
                        //if (ExpandsForOrientation(bp.Child))
                        //    nextsize[bp] = minsize[bp]+expandRemaining.NextSizePart();
                    }
                }
             /*   var expandRemaining = new SizeSplitter(remaining, nexpands);
                foreach (var bp in visibleChildren)
                {
                    if (ExpandsForOrientation(bp.Child))
                        nextsize[bp] += expandRemaining.NextSizePart();
                }*/
            }
            else //if (/*allowShrink &&*/ remaining < 0)
            {
                foreach (var bp in visibleChildren)
                {
                    nextsize[bp] = minsize[bp];
                }
            }
            return nextsize;
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
    class SizeSplitter
    {
        int rem;
        int part;

        public SizeSplitter(double total, int numParts)
        {
            if (numParts > 0)
            {
                part = ((int)total) / numParts;
                rem = ((int)total) % numParts;
            }
        }

        public double NextSizePart()
        {
            if (rem > 0)
            {
                rem--;
                return part + 1;
            }
            else
                return part;
        }
    }
}
