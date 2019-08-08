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

    public class PropertyGridValueChangedArgs : EventArgs
    {
        public PropertyDescriptor PropertyDescriptor { get; }
        public object NewValue { get; }

        public PropertyGridValueChangedArgs(PropertyDescriptor pd, object newvalue)
        {
            this.PropertyDescriptor = pd;
            this.NewValue = newvalue;
        }
    }

    public class PropertyTab : VBox2
    {
        class SplitHeader : HPaned
        {
            public event EventHandler CheckValue;

            private double position = -1;
            private bool needsresize = false;

            public SplitHeader()
            {
                this.Panel1.Content = new Label() { MinWidth = 3 * PropertyGrid.spacedx };
                this.Panel2.Content = new Label() { MinWidth = 3 * PropertyGrid.spacedx };

                SetPosition(.5);
            }
            protected override void OnBoundsChanged()
            {
                SetPosition(this.position);
                base.OnBoundsChanged();
            }
            protected override void OnPositionChanged()
            {
                base.OnPositionChanged();
                this.CheckValue?.Invoke(this, EventArgs.Empty);
            }
            public double GetPosition()
            {
                var ww = this.Size.Width;
               /* if (ww <= 0.0)
                {
                    return this.position;
                    ww = this.Panel1.Content.MinWidth + this.Panel2.Content.MinWidth;
                }*/
                if (ww > 0.0)
                {
                    double value;
                    if (this.needsresize)
                    {
                        this.needsresize = false;
                        value = this.Position = this.position * ww;
                    }
                    else if (Toolkit.CurrentEngine.Type == ToolkitType.XamMac)
                    {
                        value = this.Panel1.Content.Size.Width;
                    }
                    else
                    {
                        value = this.Position;
                    }
                    return Math.Max(0, Math.Min(1, value / ww));
                }
                return this.position;
            }
            public void SetPosition(double value)
            {
                if (this.needsresize || !this.position.Equals(value))
                {
                    this.position = value;

                    try
                    {
                        //   if (Toolkit.CurrentEngine.Type != ToolkitType.XamMac)

                        /*if (paned.Size.Width <= 0)
                        {
                            Debug.Assert(false);
                        }
                        else*/
                        {
                            value *= this.Size.Width;
                        }
                        if (this.Size.Width > 0)
                        {
                            this.Position = value;

                            if (this.needsresize)
                            {
                                this.needsresize = false;
                                this.CheckValue?.Invoke(this, EventArgs.Empty);
                            }
                        }
                        else
                        {
                            this.needsresize = true;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        this.needsresize = true;
                    }
                }
            }
        }
        public event PropertyGrid.PropertyValueChangedHandler ValueChanged;

        public bool EditMode => CheckEditMode(this.tree);

        public int Index => Array.IndexOf(this.owner.curtabs, this);

        private bool CheckEditMode(GridItem item) => ((item?.Widget?.Tag as EditCanvas)?.editmode ?? false) || (item?.Items?.Any(_subitem => CheckEditMode(_subitem)) ?? false);

        //ITypeDescriptorContext
        private GridItem tree;
        public virtual Attribute[] Filter => new Attribute[] { new BrowsableAttribute(true) };
        private readonly VBox2 vboxlist;
        private readonly ScrollControl2 scroller;
        private readonly SplitHeader splitheader;
        CancellationTokenSource worker2cancel = new CancellationTokenSource();
        private int updating = 0;
        private PropertyGrid owner;

        public virtual string TabText => "Properties";

        public virtual object[] SelectedObjects
        {
            get => this.owner.SelectedObjects;
            set => this.owner.SelectedObjects = value;
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
            
            this.vboxlist = new VBox2() { Spacing = 0 };

            this.scroller = new ScrollControl2() {  HorizontalPlacement = WidgetPlacement.Fill, VerticalPlacement = WidgetPlacement.Fill };

            this.scroller.Content = this.vboxlist;

            this.MinWidth = PropertyGrid.spacedx * 6;
            this.MinHeight = PropertyGrid.lineheight * 2 + PropertyGrid.splitheight;

            var hbox2 = new HBox2();
            this.splitheader = new SplitHeader() { };
            this.splitheader.SetPosition(.5);
            this.splitheader.CheckValue += Splitheader_CheckValue;

            hbox2.PackStart(this.splitheader, true, true);
            hbox2.PackStart(new Spacer(()=>this.scroller.VScroll.Scrollbar.Size.Width, Orientation.Horizontal));
            
            base.PackStart(hbox2, false, vpos: WidgetPlacement.Fill, hpos: WidgetPlacement.Fill);
            
            base.PackStart(this.scroller, true, true);

            this.scroller.BoundsChanged += (s, a) => hbox2.QueueForReallocate();
        }

        private void Scrollcanvas_BoundsChanged(object sender, EventArgs e)
        {
            //      CheckSize();
        }
        void CheckSize()
        {
            if (this.WindowBounds.Width >= this.scroller.VScroll.Scrollbar.Size.Width && this.scroller.Size.Height >= PropertyGrid.splitheight)
            {
                this.updating++;
                /*
                                this.SetChildBounds(this.splitheader, new Rectangle(0, 0, this.Bounds.Width - scrollw, PropertyGrid.splitheight));
                                this.SetChildBounds(this.scrollcanvas, new Rectangle(0, PropertyGrid.splitheight, this.Bounds.Width - scrollw, this.Bounds.Height - PropertyGrid.splitheight));
                                this.SetChildBounds(this.scrollbar, new Rectangle(this.Bounds.Width - scrollw, PropertyGrid.splitheight, scrollw, this.Bounds.Height - PropertyGrid.splitheight));

                                *///base.OnBoundsChanged();

        //        UpdateScroll();

                var listsize = this.ListHeight;

                //this.scrollbar.ClampPage(0, listsize);

              //  this.scrollcanvas.SetChildBounds(this.vboxlist, new Rectangle(0, -this.scrollbar.Value, this.WindowBounds.Width - this.scrollbar.Size.Width, listsize));

                SetWidth(this.tree);

                //  this.QueueForReallocate();

                this.updating--;
            }
        }

        public virtual void Refresh()
        {
            if (this.SelectedObjects.Length > 0)
            {
                if (this.tree != null)
                {
                    var newtree = new GridItemRoot(this, this.SelectedObjects.SingleOrDefault());
                    (this.tree as GridItemRoot).Refresh(newtree);

                    Clear();
                    this.tree = newtree;
                    CreateList(this.tree);
                    CheckSize();
                }
                else
                {
                    Fill(false);
                }
            }
            else
            {
                Clear();
            }
        }
        

        private void Splitheader_CheckValue(object sender, EventArgs e)
        {
            SetWidth(this.tree);
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

        /*   public void Refresh()
           {
               var tree = new GridItemRoot(this, this.SelectedObjects.SingleOrDefault());

               if (this.tree != null)
               {
                   this.tree.Refresh(tree)
               }
               else
               {
                   Clear();
                   CreateList(this.tree);
                   CheckSize();
               }
             //  CreateList(this.tree);
             //  CheckSize();
           }*/
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
                    CheckSize();
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
            {
                CheckSize();
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

                    var ww = Math.Max(0, this.scroller.ViewSize.Width);

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
                    var ww = this.scroller.ViewSize.Width > 0 && this.Size.Height > 0 ? (this.scroller.ViewSize.Width * this.splitheader.GetPosition()) : this.scroller.ViewSize.Width / 2;
                    var left = new Table() { WidthRequest = ww, HeightRequest = PropertyGrid.lineheight, ExpandHorizontal = true };
                    var right = new Table() { WidthRequest = Math.Max(0, this.scroller.ViewSize.Width - ww), HeightRequest = PropertyGrid.lineheight, ExpandHorizontal = true };

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
                CheckSize();
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
                Debug.Assert(false);
                Debug.Assert(object.ReferenceEquals(this.SelectedObjects.Single(), value));
                Debug.Assert(object.ReferenceEquals((item as GridItemRoot).Value, value));
                return;
            }
            if (item.Parent is GridItemRoot)
            {
                item.PropertyDescriptor.SetValue((item.Parent as GridItemRoot).Value/*this.SelectedObject*/, value);
                CheckParentNotify(item);

                this.OnPropertyValueChanged(item.PropertyDescriptor, value);
                this.owner.fire_OnPropertyValueChanged(this, item.PropertyDescriptor, value);
                return;
            }

            _SetValue(item, value);
       /*     var parentvalue = this.GetValue(item.Parent);
            item.PropertyDescriptor.SetValue(parentvalue, value);

            if (item.Parent.PropertyDescriptor.PropertyType.IsValueType)
            {
                var parentobj = GetValue(item.Parent.Parent);
                item.Parent.PropertyDescriptor.SetValue(parentobj, parentvalue);
            }*/
            CheckParentNotify(item);

            this.OnPropertyValueChanged(item.PropertyDescriptor, value);
            this.owner.fire_OnPropertyValueChanged(this, item.PropertyDescriptor, value);
        }
        void _SetValue(GridItem item, object value)
        {
            var parentvalue = this.GetValue(item.Parent);
            item.PropertyDescriptor.SetValue(parentvalue, value);

            if (item.Parent.PropertyDescriptor.PropertyType.IsValueType)
            {
                if (item.Parent == null)
                {
         //       Debug.Assert(item.Parent != null); // no struct at root right now
                    this.SelectedObjects = new object[] { parentvalue };
                }
                else
                {
                    _SetValue(item.Parent, parentvalue);
                }
              //  var parentobj = GetValue(item.Parent.Parent);
            //item.Parent.PropertyDescriptor.SetValue(parentobj, parentvalue);
            }
}
        protected virtual void OnPropertyValueChanged(PropertyDescriptor pd, object value)
        {
            this.ValueChanged?.Invoke(this, new PropertyGridValueChangedArgs(pd, value));
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
            /*   if (item.Parent?.Widget?.Tag is EditCanvas)
               {
                   (item.Parent.Widget.Tag as EditCanvas).Refresh();

                   CheckParentNotify(item.Parent);
               }*/
            this.Refresh();
           // item.RefreshProperties(this.GetValue(item));
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