using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Xwt;

namespace BaseLib.Xwt
{
    public abstract class GridItem
    {
        protected PropertyGrid owner;
        protected GridItem(PropertyGrid owner)
        {
            this.owner = owner;
        }
        public virtual bool Expandable { get; internal set; } = false;
        public virtual bool Expanded { get; set; }
        public virtual GridItem[] Items { get; internal set; }
        public abstract string Label { get; }
        public virtual PropertyDescriptor PropertyDescriptor { get; internal set; }
        public virtual TypeConverter TypeConverter { get; internal set; }
        public GridItem Parent { get; internal set; }
        public virtual bool HasEditor => false;

        internal Widget Widget { get; set; }
        internal virtual void RefreshProperties(object value) { }
    }
    public class GridItemProperty : GridItem, ITypeDescriptorContext
    {
        protected string displayvalue;
        protected string _label;
        public override string Label => _label;
        public override bool HasEditor => false;

        IContainer ITypeDescriptorContext.Container => throw new NotImplementedException();
        object ITypeDescriptorContext.Instance => this.owner.GetValue(this.Parent);
        PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor => this.PropertyDescriptor;

        private GridItemProperty(PropertyGrid owner)
            : base(owner)
        {
        }
        internal GridItemProperty(PropertyGrid owner, object value, bool expand)
            : this(owner)
        {
            this.Expanded = expand;
            this.TypeConverter = TypeDescriptor.GetConverter(value);
            this._label = value?.GetType().Name;
            this.displayvalue = this.TypeConverter?.ConvertToString(this, value) ?? value?.ToString();
            Initialize(value);
        }
        internal GridItemProperty(GridItemProperty owner, object parentvalue, PropertyDescriptor pd)
            : this(owner.owner)//, pd.GetValue(value), true)
        {
            this.Parent = owner;
            this.PropertyDescriptor = pd;

            var a = pd.Attributes.OfType<TypeConverterAttribute>().FirstOrDefault();
            object value = pd.GetValue(parentvalue);
            if (a != null)
            {
                this.TypeConverter = (TypeConverter)Activator.CreateInstance(Type.GetType(a.ConverterTypeName));
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
                    var props = TypeConverter.GetProperties(this, value, this.owner.filter);

                    if (this is GridItemRoot && owner.SortCategorized)
                    {
                        if (owner.SortAlphabetical)
                        {
                            props.Sort(new sort(true, true));
                        }
                        else
                        {
                            props.Sort(new sort(false, true));
                        }
                        string cat = null;

                        List<GridItem> l = new List<GridItem>();
                        foreach (var pd in props.OfType<PropertyDescriptor>())
                        {
                            if (cat != pd.Category)
                            {
                                cat = pd.Category;
                                l.Add(new GridItemCategory(owner, cat));
                            }
                            l.Add(new GridItemProperty(this, value, pd));
                        }
                        this.Items = l.ToArray();
                        return;
                    }
                    else if (owner.SortAlphabetical)
                    {
                        props.Sort(new sort(true, false));
                    }
                    this.Items = props.Cast<PropertyDescriptor>().Select(_pd => new GridItemProperty(this, value, _pd)).ToArray();
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
        class sort : IComparer
        {
            bool abc, cat;
            public sort(bool abc, bool cat)
            {
                this.abc = abc;
                this.cat = cat;
            }
            int IComparer.Compare(object x, object y)
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
        internal override void RefreshProperties(object value)
        {
            this.Items = TypeConverter.GetProperties(this, value, this.owner.filter).Cast<PropertyDescriptor>().Select(_pd => new GridItemProperty(this, value, _pd)).ToArray();
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
            return null;
        }
    }
    public class GridItemCategory : GridItem
    {
        private string category;

        public override string Label => this.category;

        internal GridItemCategory(PropertyGrid owner, string category)
            : base(owner)
        {
            this.Expandable = true;
            this.Expanded = true;
            this.category = category;
        }
    }
    public class GridItemArrayValue : GridItem
    {
        internal GridItemArrayValue(PropertyGrid owner)
            : base(owner)
        {
        }

        public override string Label => "array";
    }
    public class GridItemRoot : GridItemProperty
    {
        public object Value { get; }

        internal GridItemRoot(PropertyGrid owner, object value)
            : base(owner, value, true)
        {
            this.Value = value;
        }
    }
}