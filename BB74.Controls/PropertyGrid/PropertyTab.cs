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

    public class PropertyTab : VBox2
    {
        public bool EditMode => CheckEditMode(this.tree);

        private bool CheckEditMode(GridItem item) => ((item?.Widget?.Tag as EditCanvas)?.editmode ?? false) || (item?.Items?.Any(_subitem => CheckEditMode(_subitem)) ?? false);

        //ITypeDescriptorContext
        private GridItem tree;
        public virtual Attribute[] Filter => new Attribute[] { new BrowsableAttribute(true) };
        private readonly Scrollbar scrollbar;
        private readonly Canvas scrollcanvas;
        private readonly VBox2 vboxlist;
        private readonly HPaned splitheader;
        CancellationTokenSource worker2cancel = new CancellationTokenSource();
        private int updating = 0;
        private PropertyGrid owner;

        public virtual string TabText => "Properties";

        public virtual object[] SelectedObjects
        {
            get => this.owner.SelectedObjects;
        }
        public PropertySort SortMode { get; set; } = PropertySort.CategorizedAlphabetical;

        private double ListHeight => this.vboxlist.Children.Select(_c => _c.HeightRequest).Sum() + 2;

        public PropertyGrid PropertyGrid => this.owner;

        internal bool SortAlphabetical => this.SortMode == PropertySort.Alphabetical || this.SortMode == PropertySort.CategorizedAlphabetical;
        internal bool SortCategorized => this.SortMode == PropertySort.Categorized || this.SortMode == PropertySort.CategorizedAlphabetical;

        public PropertyTab(PropertyGrid owner)
        {
            this.owner = owner;

            this.BackgroundColor = Colors.White;
            this.HorizontalPlacement = this.VerticalPlacement = WidgetPlacement.Fill;

            this.scrollbar = new VScrollbar() { ExpandVertical = true, VerticalPlacement = WidgetPlacement.Fill };
            this.scrollbar.ValueChanged += Scrollbar_ValueChanged;
            this.scrollbar.MouseScrolled += Scroll_MouseScrolled;


            this.vboxlist = new VBox2() { Spacing = 0 };

            this.scrollcanvas = new Canvas() { HorizontalPlacement=WidgetPlacement.Fill,VerticalPlacement=WidgetPlacement.Fill};
            this.scrollcanvas.AddChild(this.vboxlist);

            this.scrollcanvas.MouseScrolled += Scroll_MouseScrolled;

            this.scrollcanvas.ClipToBounds();

            this.MinWidth = PropertyGrid.spacedx * 6;
            this.MinHeight = PropertyGrid.lineheight * 2 + PropertyGrid.splitheight;

            this.splitheader = new HPaned() { };

            this.splitheader.Panel1.Content = new Label() { MinWidth = 3 * PropertyGrid.spacedx };
            this.splitheader.Panel2.Content = new Label() { MinWidth = 3 * PropertyGrid.spacedx };

            //  this.PackStart(splitheader, true, false);
            this.splitheader.PositionChanged += Splitheader_PositionChanged;

            base.PackStart(this.splitheader, false, vpos: WidgetPlacement.Fill, hpos: WidgetPlacement.Fill);

            var hbox = new HBox2();

            hbox.PackStart(this.scrollcanvas, true, true);
            hbox.PackStart(this.scrollbar, false, false);

            base.PackStart(hbox, true, true);

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
            var pagsize = this.scrollcanvas.Size.Height;// - PropertyGrid.splitheight;

            this.scrollcanvas.SetChildBounds(this.vboxlist, new Rectangle(0, -this.scrollbar.Value, this.scrollcanvas.Size.Width, listsize));
        }


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
        public void Fill(bool first = false)
        {
            Clear();

            if (this.SelectedObjects.Length > 0)
            {
                if (first && this.Size.Width <= 0 && this.Size.Height <= 0)
                {
                    QueueOnUI(() => Fill(false));
                }
                else
                {
                    this.tree = new GridItemRoot(this, this.SelectedObjects.SingleOrDefault());
                    CreateList(this.tree);
                    OnBoundsChanged();
                }
            }
        }
        public new void Clear()
        {
            this.vboxlist.Clear();
            this.tree = null;
        }
        protected override void OnBoundsChanged()
        {
            if (this.Size.Width > 1 && this.Size.Height > PropertyGrid.splitheight)
            {
                this.updating++;
/*
                this.SetChildBounds(this.splitheader, new Rectangle(0, 0, this.Bounds.Width - scrollw, PropertyGrid.splitheight));
                this.SetChildBounds(this.scrollcanvas, new Rectangle(0, PropertyGrid.splitheight, this.Bounds.Width - scrollw, this.Bounds.Height - PropertyGrid.splitheight));
                this.SetChildBounds(this.scrollbar, new Rectangle(this.Bounds.Width - scrollw, PropertyGrid.splitheight, scrollw, this.Bounds.Height - PropertyGrid.splitheight));

                *///base.OnBoundsChanged();

                var listsize = this.ListHeight;
                var pagsize = this.Size.Height - PropertyGrid.splitheight;

                this.scrollbar.StepIncrement = 1;
                this.scrollbar.UpperValue = Math.Max(0, listsize - pagsize);
                this.scrollbar.PageSize = 1;
                this.scrollbar.Value = Math.Max(0, Math.Min(this.scrollbar.Value, this.scrollbar.UpperValue));
                //this.scrollbar.ClampPage(0, listsize);

                this.scrollcanvas.SetChildBounds(this.vboxlist, new Rectangle(0, -this.scrollbar.Value, this.scrollcanvas.Size.Width, listsize));

                SetWidth(this.tree);

              //  this.QueueForReallocate();

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

                    var ww = Math.Max(0, this.scrollcanvas.Size.Width);

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

                box.HeightRequest = box.Children.Select(_c => _c.GetBackend().GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.WithSize(PropertyGrid.lineheight)).Height).Max();

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
                       var ww = this.scrollcanvas.Size.Width > 0 && this.Size.Height > 0 ? (this.scrollcanvas.Size.Width * this.splitheader.GetPosition()) : this.scrollcanvas.Size.Width/2;
                    var left = new Table() { WidthRequest = ww, HeightRequest = PropertyGrid.lineheight, ExpandHorizontal = true };
                    var right = new Table() { WidthRequest = Math.Max(0, this.scrollcanvas.Size.Width -  ww), HeightRequest = PropertyGrid.lineheight, ExpandHorizontal = true };

                    // spacing
                    left.Add(new Label() { WidthRequest = level * PropertyGrid.spacedx, HeightRequest = PropertyGrid.lineheight, MinWidth = level * PropertyGrid.spacedx }, 0, 0, 1, 1, vexpand: true);

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
                        MinWidth = PropertyGrid.spacedx * 2,
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

                    box.HeightRequest = PropertyGrid.lineheight;// box.Children.Select(_c => _c.GetBackend().GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.WithSize(PropertyGrid.lineheight)).Height).Max();

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
            if (item != null)
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
            }
            return null;
        }
        internal void SetValue(GridItem item, object value)
        {
            if (item is GridItemRoot)
            {
                //this.SelectedObject = value;
                Debug.Assert(object.ReferenceEquals(this.SelectedObjects.Single(), value));
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

            if (item.Parent.PropertyDescriptor.PropertyType.IsValueType)
            {
                var parentobj = GetValue(item.Parent.Parent);
                item.Parent.PropertyDescriptor.SetValue(parentobj, parentvalue);
            }
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
}