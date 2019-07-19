    using BaseLib.Xwt.Design;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xwt;

namespace BaseLib.Xwt.Controls.PropertyGrid
{
    using Internals;

    public abstract class GridItem
    {
        public PropertyGrid Owner { get; }
        PropertyGrid OwnerGrid { get; }
        public PropertyTab Tab { get; }
        protected GridItem(PropertyTab owner)
        {
            this.Tab = owner;
            this.OwnerGrid = this.Owner = owner.PropertyGrid;
        }
        public virtual bool Expandable { get; internal set; } = false;
        public virtual bool Expanded { get; set; }
        public virtual GridItem[] Items { get; internal set; }
        public abstract string Label { get; }
        public virtual PropertyDescriptor PropertyDescriptor { get; internal set; }
        public virtual TypeConverter TypeConverter { get; internal set; }
        public virtual UITypeEditor Editor { get; internal set; }
        public GridItem Parent { get; internal set; }

        internal Widget Widget { get; set; }
        internal virtual void RefreshProperties(object value) { }

        public virtual void Refresh(GridItem newitem, object newvalue)
        {
            if (newitem.GetType() == this.GetType() && newitem.Label==this.Label)
            {
                newitem.Expanded = this.Expanded;

                if (this.Expanded && this.Items!=null)
                {
                    if (newitem.Items == null)
                    {
                        newitem.RefreshProperties(newvalue);
                    }
                    foreach (var i in newitem.Items)
                    {
                        var o = this.Items.Where(_i => _i.GetType() == i.GetType() &&
                                                       _i.Label == i.Label &&
                                                       _i.PropertyDescriptor?.PropertyType == i.PropertyDescriptor?.PropertyType).FirstOrDefault();

                        if (o != null)
                        {
                            var v = i.PropertyDescriptor?.GetValue(newvalue);
                            o.Refresh(i, v);
                        }
                        else
                        {
                            i.Expanded = false;
                        }
                    }
                }
                else
                {
                    newitem.Expanded = false;
                }
            }
        }
    }
    public class GridItemProperty : GridItem, ITypeDescriptorContext
    {
        protected string displayvalue;
        protected string _label;
        public override string Label => _label;

        IContainer ITypeDescriptorContext.Container => throw new NotImplementedException();
        object ITypeDescriptorContext.Instance => this.Tab.GetValue(this.Parent);
        PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor => this.PropertyDescriptor;

        private GridItemProperty(PropertyTab owner)
            : base(owner)
        {
        }
        internal GridItemProperty(PropertyTab owner, object value, bool expand)
            : this(owner)
        {
            this.Expanded = expand;
            this.TypeConverter = TypeDescriptor.GetConverter(value);
            this.Editor = (UITypeEditor)TypeDescriptor.GetEditor(value, typeof(UITypeEditor));
         /*   if (this.Editor == null)
            {
                var e = value?.GetType().GetCustomAttributes(typeof(EditorAttribute), true)?.Cast<EditorAttribute>().SingleOrDefault();
                if (e != null)
                {
                    this.Editor = (UITypeEditor)Activator.CreateInstance(e.EditorType);
                }
            }*/
            this._label = value?.GetType().Name;
            this.displayvalue = this.TypeConverter?.ConvertToString(this, value) ?? value?.ToString();
            Initialize(value);
        }
        internal GridItemProperty(GridItem owner, object parentvalue, PropertyDescriptor pd)
            : this(owner.Tab)//, pd.GetValue(value), true)
        {
            this.Parent = owner;
            this.PropertyDescriptor = pd;

            var a = pd.Attributes.OfType<TypeConverterAttribute>().FirstOrDefault();
            var e = pd.Attributes.OfType<EditorAttribute>().FirstOrDefault();

            object value = pd.GetValue(parentvalue);
            if (a != null)
            {
                this.TypeConverter = (TypeConverter)Activator.CreateInstance(Type.GetType(a.ConverterTypeName));
            }
            if (e != null)
            {
                this.Editor = (UITypeEditor)Activator.CreateInstance(Type.GetType(e.EditorTypeName));
            }
            if (TypeConverter == null)
            {
                this.TypeConverter = TypeDescriptor.GetConverter(pd.PropertyType);
            }
            if (TypeConverter == null && value != null)
            {
                TypeConverter = TypeDescriptor.GetConverter(value);
            }
            this._label = this.PropertyDescriptor.DisplayName ?? value?.GetType().FullName;
            Initialize(value);
        }
        private void GetArray(ICollection collection)
        {
        }
        private void Initialize(object value)
        {
            if (TypeConverter != null && TypeConverter.GetPropertiesSupported(this))
            {
                this.Expandable = true;

                if (this.Expanded)
                {
                    var props = TypeConverter.GetProperties(this, value, this.Tab.Filter);

                    var pp = props.OfType<PropertyDescriptor>().ToList();

                    if (this is GridItemRoot && Owner.SortCategorized)
                    {
                        if (Owner.SortAlphabetical)
                        {
                            pp.Sort(new sort(true, true));
                        }
                        else
                        {
                            pp.Sort(new sort(false, true));
                        }
                        string cat = null;

                        List<GridItem> l = new List<GridItem>();
                        foreach (var pd in pp)
                        {
                            if (cat != pd.Category)
                            {
                                cat = pd.Category;
                                l.Add(new GridItemCategory(this.Tab, cat));
                            }
                            l.Add(new GridItemProperty(this, value, pd));
                        }
                        this.Items = l.ToArray();
                        return;
                    }
                    else if (Owner.SortAlphabetical)
                    {
                        pp.Sort(new sort(true, false));
                    }
                    this.Items = pp.Select(_pd => new GridItemProperty(this, value, _pd)).ToArray();
                }
            }
            else if (value is ICollection)
            {
                GetArray(value as ICollection);
            }
            else
            {
                this.Expanded = this.Expandable = false;
            }
        }
        class sort : IComparer<PropertyDescriptor>
        {
            bool abc, cat;
            public sort(bool abc, bool cat)
            {
                this.abc = abc;
                this.cat = cat;
            }
            int IComparer<PropertyDescriptor>.Compare(PropertyDescriptor x, PropertyDescriptor y)
            {
                if (cat)
                {
                    int n = string.Compare((x as PropertyDescriptor).Category, (y as PropertyDescriptor).Category);

                    if (n != 0 || !abc)
                    {
                        return n;
                    }
                }
                return string.Compare((x as PropertyDescriptor).DisplayName, (y as PropertyDescriptor).DisplayName);
            }
        }
        class editor : IWindowsFormsEditorService
        {
            private GridItemProperty owner;

            public editor(GridItemProperty owner)
            {
                this.owner = owner;
            }
            void IWindowsFormsEditorService.CloseDropDown()
            {
                (this.owner.Widget as EditCanvas).CloseDropDown();
            }
            void IWindowsFormsEditorService.DropDownControl(Widget control)
            {
                if (!this.owner.Owner.EditMode || this.owner.Owner.CancelEdit(true))
                {
                    (this.owner.Widget as EditCanvas).Showdropdown(control);
                }
            }
            Command IWindowsFormsEditorService.ShowDialog(Dialog dialog)
            {
                if (!this.owner.Owner.EditMode || this.owner.Owner.CancelEdit(true))
                {
                    return dialog.Run(this.owner.Owner.ParentWindow);
                }
                return Command.Cancel;
            }
        }
        internal override void RefreshProperties(object value)
        {
            this.Items = TypeConverter.GetProperties(this, value, this.Tab.Filter).Cast<PropertyDescriptor>().Select(_pd => new GridItemProperty(this, value, _pd)).ToArray();
        }
        void ITypeDescriptorContext.OnComponentChanged()
        {
        }
        bool ITypeDescriptorContext.OnComponentChanging()
        {
            return true;
        }
        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType == typeof(IWindowsFormsEditorService))
            {
                return new editor(this);
            }
            return null;
        }
    }
    public class GridItemCategory : GridItem
    {
        private string category;

        public override string Label => this.category;

        internal GridItemCategory(PropertyTab owner, string category)
            : base(owner)
        {
            this.Expandable = true;
            this.Expanded = true;
            this.category = category;
        }
    }
    public class GridItemArrayValue : GridItem
    {
        internal GridItemArrayValue(PropertyTab owner)
            : base(owner)
        {
        }

        public override string Label => "array";
    }
    public class GridItemRoot : GridItemProperty
    {
        public object Value { get; }

        internal GridItemRoot(PropertyTab owner, object value)
            : base(owner, value, true)
        {
            this.Value = value;
        }
        public override void Refresh(GridItem newitem, object newvalue)
        {
            if ((newitem as GridItemRoot).Value.GetType() == this.Value.GetType())
            {
                base.Refresh(newitem, newvalue);
            }
        }
        public void Refresh(GridItemRoot newitem)
        {
            this.Refresh(newitem, newitem.Value);
        }
    }
}