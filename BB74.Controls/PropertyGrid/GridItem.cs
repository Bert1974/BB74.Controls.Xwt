﻿using BaseLib.Xwt.Design;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xwt;

namespace BaseLib.Xwt.PropertyGrid
{
    using Internals;

    public abstract class GridItem
    {
        public PropertyGrid Owner { get; }
        protected GridItem(PropertyGrid owner)
        {
            this.Owner = owner;
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
    }
    public class GridItemProperty : GridItem, ITypeDescriptorContext
    {
        protected string displayvalue;
        protected string _label;
        public override string Label => _label;

        IContainer ITypeDescriptorContext.Container => throw new NotImplementedException();
        object ITypeDescriptorContext.Instance => this.Owner.GetValue(this.Parent);
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
        internal GridItemProperty(GridItemProperty owner, object parentvalue, PropertyDescriptor pd)
            : this(owner.Owner)//, pd.GetValue(value), true)
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
                    var props = TypeConverter.GetProperties(this, value, this.Owner.filter);

                    if (this is GridItemRoot && Owner.SortCategorized)
                    {
                        if (Owner.SortAlphabetical)
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
                                l.Add(new GridItemCategory(Owner, cat));
                            }
                            l.Add(new GridItemProperty(this, value, pd));
                        }
                        this.Items = l.ToArray();
                        return;
                    }
                    else if (Owner.SortAlphabetical)
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
            this.Items = TypeConverter.GetProperties(this, value, this.Owner.filter).Cast<PropertyDescriptor>().Select(_pd => new GridItemProperty(this, value, _pd)).ToArray();
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