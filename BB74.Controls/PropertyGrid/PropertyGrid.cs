using BaseLib.Xwt.Design;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xwt;
using Xwt.Backends;
using Xwt.Drawing;

namespace BaseLib.Xwt.Controls.PropertyGrid
{
    using Internals;

    public class PropertyGrid : Canvas
    {
        static Font PropertyFont = Font.SystemFont.WithSize(13);
        static Font CategoryFont = PropertyFont.WithWeight(FontWeight.Heavy);
        static Font ButtonFont = Font.SystemFont.WithSize(13).WithWeight(FontWeight.Heavy);
        const int spacedx = 12, splitheight = 12, lineheight = 22;
        internal bool EditMode => CheckEditMode(this.tree);

        private bool CheckEditMode(GridItem item) => ((item?.Widget?.Tag as EditCanvas)?.editmode ?? false) || (item?.Items?.Any(_subitem => CheckEditMode(_subitem)) ?? false);

        //ITypeDescriptorContext
        private object[] selection;
        private GridItem tree;
        internal Attribute[] filter = new Attribute[] { new BrowsableAttribute(true) };
        private readonly Scrollbar scrollbar;
        private readonly Canvas scrollcanvas;
        private readonly VBox vboxlist;
        private readonly HPaned splitheader;
        private readonly double scrollw;
        CancellationTokenSource worker2cancel = new CancellationTokenSource();
        private int updating = 0;

        public object SelectedObject
        {
            get => this.selection.SingleOrDefault();
            set
            {
                this.selection = new object[] { value };
                Fill();
            }
        }
        public PropertySort SortMode { get; set; } = PropertySort.CategorizedAlphabetical;


        internal bool SortAlphabetical => this.SortMode == PropertySort.Alphabetical || this.SortMode == PropertySort.CategorizedAlphabetical;
        internal bool SortCategorized => this.SortMode == PropertySort.Categorized || this.SortMode == PropertySort.CategorizedAlphabetical;

        public PropertyGrid()
        {
            this.BackgroundColor = Colors.White;

            this.scrollbar = new VScrollbar() { ExpandVertical = true, VerticalPlacement = WidgetPlacement.Fill };
            this.scrollbar.ValueChanged += Scrollbar_ValueChanged;
            this.scrollbar.MouseScrolled += Scroll_MouseScrolled;


            this.vboxlist = new VBox() { Spacing = 0 };

            this.scrollcanvas = new Canvas();
            this.scrollcanvas.AddChild(this.vboxlist);

            this.scrollcanvas.MouseScrolled += Scroll_MouseScrolled;

            this.scrollcanvas.ClipToBounds();

            this.MinWidth = spacedx * 6;
            this.MinHeight = lineheight * 2 + splitheight;

            this.splitheader = new HPaned() { };

            this.splitheader.Panel1.Content = new Label() { MinWidth = 3 * spacedx };
            this.splitheader.Panel2.Content = new Label() { MinWidth = 3 * spacedx };

            //  this.PackStart(splitheader, true, false);
            this.splitheader.PositionChanged += Splitheader_PositionChanged;

            this.AddChild(this.splitheader);
            this.AddChild(this.scrollcanvas);
            this.AddChild(this.scrollbar);

            this.scrollw = this.scrollbar.GetBackend().GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained).Width;

            QueueOnUI(() => { this.splitheader.SetPosition(.5); SetWidth(this.tree); });
        }

        private void Scroll_MouseScrolled(object sender, MouseScrolledEventArgs e)
        {
            ScrollDelta((e.Direction == ScrollDirection.Down ? 1 : -1) * 10);
        }
        private void ScrollDelta(double v)
        {
            this.scrollbar.Value = Math.Max(this.scrollbar.LowerValue, Math.Min(this.scrollbar.UpperValue, this.scrollbar.Value + v));
            Scrollbar_ValueChanged(null, EventArgs.Empty);
        }

        private void Scrollbar_ValueChanged(object sender, EventArgs e)
        {
            var listsize = this.ListHeight;
            var pagsize = this.Bounds.Height - splitheight;

            this.scrollcanvas.SetChildBounds(this.vboxlist, new Rectangle(0, -this.scrollbar.Value, this.Bounds.Width - scrollw, listsize));
        }

        private double ListHeight => this.vboxlist.Children.Select(_c => _c.HeightRequest).Sum() + 2;

        private void QueueOnUI(Action m)
        {
            Task.Factory.StartNew(m, worker2cancel.Token, TaskCreationOptions.None, Application.UITaskScheduler);
        }
        protected override void OnButtonPressed(ButtonEventArgs args)
        {
            base.OnButtonPressed(args);

            CancelEdit(true);
        }

        private void Splitheader_PositionChanged(object sender, EventArgs e)
        {
            if (this.updating == 0)
            {
                SetWidth(this.tree);
            }
        }
        public void Fill(bool first = true)
        {
            Clear();

            if (first && this.Size.Width <= 0 && this.Size.Height <= 0)
            {
                QueueOnUI(() => Fill(false));
            }
            else
            {
                this.tree = new GridItemRoot(this, this.selection[0]);
                CreateList(this.tree);
                OnBoundsChanged();
            }
        }
        public new void Clear()
        {
            this.vboxlist.Clear();
            this.tree = null;
        }
        protected override void OnBoundsChanged()
        {
            if (this.Bounds.Width > 1 && this.Bounds.Height > splitheight)
            {
                this.updating++;

                this.SetChildBounds(this.splitheader, new Rectangle(0, 0, this.Bounds.Width - scrollw, splitheight));
                this.SetChildBounds(this.scrollcanvas, new Rectangle(0, splitheight, this.Bounds.Width - scrollw, this.Bounds.Height - splitheight));
                this.SetChildBounds(this.scrollbar, new Rectangle(this.Bounds.Width - scrollw, splitheight, scrollw, this.Bounds.Height - splitheight));

                //base.OnBoundsChanged();

                var listsize = this.ListHeight;
                var pagsize = this.Bounds.Height - splitheight;

                this.scrollbar.StepIncrement = 1;
                this.scrollbar.UpperValue = Math.Max(0, listsize - pagsize);
                this.scrollbar.PageSize = 1;
                this.scrollbar.Value = Math.Max(0, Math.Min(this.scrollbar.Value, this.scrollbar.UpperValue));
                //this.scrollbar.ClampPage(0, listsize);

                this.scrollcanvas.SetChildBounds(this.vboxlist, new Rectangle(0, -this.scrollbar.Value, this.Bounds.Width - scrollw, listsize));

                SetWidth(this.tree);

                this.updating--;
            }
            else
            {
                base.OnBoundsChanged();
            }
        }
        private void SetWidth(GridItem item)
        {
            if (item != null)
            {
                if (item.Widget != null && item.Widget.Tag is EditCanvas)
                {
                    var hbox = item.Widget as HBox;
                    var pos = this.splitheader.GetPosition();

                    var ww = Math.Max(0, this.Size.Width - scrollw);

                    hbox.Children.First().WidthRequest = Math.Floor(ww * pos);
                    hbox.Children.Skip(1).First().WidthRequest = Math.Floor(ww * (1 - pos));
                }
                if (item.Expanded && !(item is GridItemCategory))
                {
                    foreach (var subitem in item.Items)
                    {
                        SetWidth(subitem);
                    }
                }
            }
        }
        private bool CreateList(GridItem item, int level = 0)
        {
            Debug.Assert(item.Widget == null);
            Debug.Assert(item.Widget == null);

            if (item is GridItemCategory)
            {
                var box = new HBox() { Tag = item, Spacing = 0, ExpandHorizontal = true };
                // expand button
                if (item.Expandable)
                {
                    var expand = new Label(item.Expanded ? @"-" : @"+")
                    {
                        Tag = item,
                        Font = PropertyGrid.ButtonFont
                    };
                    expand.ButtonPressed += Expand_Clicked;
                    box.PackStart(expand);
                }
                var label = new Label(item.Label)
                {
                    Tag = item,
                    Font = PropertyGrid.CategoryFont
                };
                box.PackStart(label);
                item.Widget = box;

                box.HeightRequest = box.Children.Select(_c => _c.GetBackend().GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.WithSize(lineheight)).Height).Max();

                this.vboxlist.PackStart(item.Widget);

                return item.Expanded;
            }
            else
            {
                if (!(item is GridItemRoot))
                {
                    var editfld = new EditCanvas(this, item)
                    {
                        MinWidth = 64,
                        Font = PropertyGrid.PropertyFont
                    };
                    var ww = this.Size.Width > 0 && this.Size.Height > 0 ? ((this.Size.Width - scrollw) * this.splitheader.GetPosition()) : 0;
                    var left = new Table() { WidthRequest = ww, HeightRequest = lineheight, ExpandHorizontal = true };
                    var right = new Table() { WidthRequest = Math.Max(0, this.Size.Width - scrollw - ww), HeightRequest = lineheight, ExpandHorizontal = true };

                    // spacing
                    left.Add(new Label() { WidthRequest = level * spacedx, HeightRequest = lineheight, MinWidth = level * spacedx }, 0, 0, 1, 1, vexpand: true);

                    // expand button
                    if (item.Expandable)
                    {
                        var expand = new Label(item.Expanded ? @"-" : @"+")
                        {
                            Tag = editfld,
                            Font = PropertyGrid.ButtonFont
                        };
                        expand.ButtonPressed += Expand_Clicked;
                        left.Add(expand, 1, 0, 1, vexpand: true);
                    }
                    // label
                    var label = new Label(item.Label)
                    {
                        MinWidth = spacedx * 2,
                        Tag = editfld,
                        Font = PropertyGrid.PropertyFont,
                        //     HorizontalPlacement=WidgetPlacement.Fill
                    };
                    left.Add(label, 2, 0, 1, 1, hexpand: true, vexpand: true);


                    right.Add(editfld, 0, 0, hexpand: true, vexpand: true);
                    if ((item.Editor?.GetEditStyle() ?? UITypeEditorEditStyle.None) != UITypeEditorEditStyle.None)
                    {
                        var edit = new Button("E")
                        {
                            Tag = editfld,
                            Font = PropertyGrid.PropertyFont
                        };
                        edit.Clicked += Edit_Clicked;
                        right.Add(edit, 1, 0, vexpand: true);
                    }

                    var box = new HBox() { Tag = editfld, Spacing = 0 };
                    box.PackStart(left, false, false);
                    box.PackEnd(right, false, false);

                    item.Widget = box;

                    box.HeightRequest = lineheight;// box.Children.Select(_c => _c.GetBackend().GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.WithSize(lineheight)).Height).Max();

                    Debug.Assert(item.Widget != null);

                    this.vboxlist.PackStart(item.Widget);
                }
            }
            if (item.Expanded)
            {
                bool skip = false;
                foreach (var item2 in item.Items)
                {
                    if (!skip || item2 is GridItemCategory)
                    {
                        skip = false;
                        if (!CreateList(item2, level + 1))
                        {
                            skip = true;
                        }
                    }
                }
            }
            return true;
        }

        private void Edit_Clicked(object sender, EventArgs e)
        {
            var editfld = (EditCanvas)(sender as Widget).Tag;
            var item = editfld.item;

            if (item.Editor != null)
            {
                if (!this.EditMode || this.CancelEdit(true))
                {
                    var value = GetValue(item);
                    item.Editor.EditValue(item as ITypeDescriptorContext, item as IServiceProvider, value);
                }
            }
        }
        private void Expand_Clicked(object sender, ButtonEventArgs e)
        {
            if (e.Button == PointerButton.Left)
            {
                e.Handled = true;

                var expand = sender as Widget;
                GridItem item;

                if (expand.Tag is EditCanvas)
                {
                    var editfld = (EditCanvas)expand.Tag;
                    item = editfld.item;
                    if ((item.Expanded = !item.Expanded) == true)
                    {
                        item.RefreshProperties(this.GetValue(item));
                    }
                }
                else
                {
                    item = (GridItem)expand.Tag;
                    item.Expanded = !item.Expanded;
                }
                this.vboxlist.Clear();
                ClearWidgets(this.tree);

                CreateList(this.tree);
                OnBoundsChanged();
            }
        }
        private void ClearWidgets(GridItem item)
        {
            item.Widget?.Dispose();
            item.Widget = null;

            if (item.Items != null)
            {
                foreach (var subitem in item.Items)
                {
                    ClearWidgets(subitem);
                }
            }
        }
        public bool CancelEdit(bool apply)
        {
            var edititem = GetEditItem(this.tree);

            if (edititem != null)
            {
                try
                {
                    return (edititem.Widget.Tag as EditCanvas).CancelEdit(apply);
                }
                catch (Exception e)
                {
                    var mq = new QuestionMessage("Error saving value", e.Message);
                    mq.Buttons.Add(Command.Close);
                    mq.Buttons.Add(Command.Cancel);

                    if (MessageDialog.AskQuestion(this.ParentWindow, mq) == Command.Cancel)
                    {
                        return (edititem.Widget.Tag as EditCanvas).CancelEdit(false);
                    }
                }
            }
            return true;
        }

        private GridItem GetEditItem(GridItem item)
        {
            if ((item.Widget?.Tag as EditCanvas)?.editmode ?? false)
            {
                return item;
            }
            if (item.Expanded && !(item is GridItemCategory))
            {
                foreach (var subitem in item.Items)
                {
                    var r = GetEditItem(subitem);
                    if (r != null)
                    {
                        return r;
                    }
                }
            }
            return null;
        }
        internal void SetValue(GridItem item, object value)
        {
            if (item is GridItemRoot)
            {
                //this.SelectedObject = value;
                Debug.Assert(object.ReferenceEquals(this.selection[0], value));
                Debug.Assert(object.ReferenceEquals((item as GridItemRoot).Value, value));
                return;
            }
            if (item.Parent is GridItemRoot)
            {
                item.PropertyDescriptor.SetValue((item.Parent as GridItemRoot).Value/*this.SelectedObject*/, value);
                CheckParentNotify(item);
                return;
            }
            var parentvalue = this.GetValue(item.Parent);
            item.PropertyDescriptor.SetValue(parentvalue, value);
            CheckParentNotify(item);
        }

        private void CheckParentNotify(GridItem item)
        {
            /*  if (item.PropertyDescriptor?.Attributes.OfType<NotifyParentPropertyAttribute>().FirstOrDefault()?.NotifyParent??false)
              {
                  if (item.Parent.Widget.Tag is EditCanvas)
                  {
                      (item.Parent.Widget.Tag as EditCanvas).Refresh();
                  }
              }*/
            if (item.Parent?.Widget?.Tag is EditCanvas)
            {
                (item.Parent.Widget.Tag as EditCanvas).Refresh();

                CheckParentNotify(item.Parent);
            }
        }

        public object GetValue(GridItem item)
        {
            if (item is GridItemRoot)
            {
                return (item as GridItemRoot).Value;
            }
            var parentobj = GetValue(item.Parent);
            return item.PropertyDescriptor.GetValue(parentobj);
        }
    }
    namespace Internals
    {
        internal static class Extension
        {
            public static void ClipToBounds(this Canvas widget)
            {
                ClipToBounds(widget.GetBackend());
            }
            public static void ClipToBounds(this IWidgetBackend backend)
            {
                if (Toolkit.CurrentEngine.Type == ToolkitType.Wpf)
                {
                    var nativectl = backend.NativeWidget;

                    Type t = GetType("System.Windows.Controls.Panel");

                    Debug.Assert(t != null);

                    if (nativectl.GetType().IsDerived(t))
                    {
                        nativectl.GetType().SetPropertyValue(nativectl, "ClipToBounds", true);
                    }
                }
            }
            public static Type GetType(string typeName)
            {
                var type = Type.GetType(typeName);
                if (type != null) return type;
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = a.GetType(typeName);
                    if (type != null)
                        return type;
                }
                return null;
            }
            public static bool IsDerived(this Type b, Type t)
            {
                while (b != null && !object.ReferenceEquals(b, t))
                {
                    b = b.BaseType;
                }
                return b != null;
            }
            public static void SetPropertyValue(this Type type, object instance, string propertyname, object value)
            {
                type.GetProperty(propertyname, BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty).SetValue(instance, value, new object[0]);
            }

            public static IWidgetBackend GetBackend(this Widget o)
            {
                return (IWidgetBackend)global::Xwt.Toolkit.CurrentEngine.GetSafeBackend(o);
            }
            public static IWindowFrameBackend GetBackend(this WindowFrame o)
            {
                return (IWindowFrameBackend)global::Xwt.Toolkit.CurrentEngine.GetSafeBackend(o);
            }
            public static IWindowBackend GetBackend(this Window o)
            {
                return (IWindowBackend)global::Xwt.Toolkit.CurrentEngine.GetSafeBackend(o);
            }
            public static double GetPosition(this HPaned paned)
            {
                var ww = paned.Size.Width;
                if (ww <= 0.0)
                {
                    ww = paned.Panel1.Content.MinWidth + paned.Panel2.Content.MinWidth;
                }
                if (ww > 0.0)
                {
                    double value;
                    if (Toolkit.CurrentEngine.Type == ToolkitType.XamMac)
                    {
                        value = paned.Panel1.Content.Size.Width;
                    }
                    else
                    {
                        value = paned.Position;
                    }
                    return value / ww;
                }
                return .5;
            }
            public static void SetPosition(this HPaned paned, double value)
            {
                //   if (Toolkit.CurrentEngine.Type != ToolkitType.XamMac)
                {
                    if (paned.Size.Width <= 0)
                    {
                        Debug.Assert(false);
                    }
                    else
                    {
                        value *= paned.Size.Width;
                    }
                }
                try
                {
                    paned.Position = value;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}