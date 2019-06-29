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
    using System.Collections.ObjectModel;

    public class PropertyTabCollection : Collection<PropertyTab>
    {
        public event EventHandler OnCollectionChanged;

        public PropertyTabCollection()
        {
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            OnCollectionChanged?.Invoke(this, EventArgs.Empty);
        }
        protected override void InsertItem(int index, PropertyTab item)
        {
            base.InsertItem(index, item);
            OnCollectionChanged?.Invoke(this, EventArgs.Empty);
        }
        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            OnCollectionChanged?.Invoke(this, EventArgs.Empty);
        }
        protected override void SetItem(int index, PropertyTab item)
        {
            base.SetItem(index, item);
            OnCollectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public class PropertyGrid : VBox2
    {
        public static readonly Font PropertyFont = Font.SystemFont.WithSize(13);
        public static readonly Font CategoryFont = PropertyFont.WithWeight(FontWeight.Heavy);
        public static readonly Font ButtonFont = Font.SystemFont.WithSize(13).WithWeight(FontWeight.Heavy);
        public static readonly int spacedx = 12, splitheight = 12, lineheight = 22;
        internal bool EditMode => this.curtab?.EditMode ?? false;

        private bool CheckEditMode(GridItem item) => ((item?.Widget?.Tag as EditCanvas)?.editmode ?? false) || (item?.Items?.Any(_subitem => CheckEditMode(_subitem)) ?? false);

        //ITypeDescriptorContext
        private object[] selection = new object[0];
        //    internal Attribute[] filter = new Attribute[] { new BrowsableAttribute(true) };

        protected PropertyTabCollection Tabs { get; } = new PropertyTabCollection();

        CancellationTokenSource worker2cancel = new CancellationTokenSource();
        private Toolbar toolbar;
        private Table viewtable;
        private PropertyTab curtab;

        public object SelectedObject
        {
            get => this.selection.SingleOrDefault();
            set
            {
                this.selection = new object[] { value };
                Fill();
            }
        }
        public object[] SelectedObjects
        {
            get => this.selection;
            set
            {
                this.selection = value ?? new object[0];
                Fill();
            }
        }
        public PropertySort SortMode { get; set; } = PropertySort.CategorizedAlphabetical;


        internal bool SortAlphabetical => this.SortMode == PropertySort.Alphabetical || this.SortMode == PropertySort.CategorizedAlphabetical;
        internal bool SortCategorized => this.SortMode == PropertySort.Categorized || this.SortMode == PropertySort.CategorizedAlphabetical;

        public PropertyGrid()
        {
            this.BackgroundColor = Colors.White;
            this.MinWidth = spacedx * 6;
            this.MinHeight = lineheight * 2 + splitheight;
            base.Margin = 0;
            base.Spacing = 0;

            this.toolbar = new Toolbar() { BackgroundColor = Colors.LightGray };

            base.PackStart(this.toolbar, false, true);

            this.viewtable = new Table() { DefaultColumnSpacing = 0, DefaultRowSpacing = 0, HorizontalPlacement = WidgetPlacement.Fill, VerticalPlacement = WidgetPlacement.Fill, ExpandHorizontal = true, ExpandVertical = true };

            base.PackStart(this.viewtable, true, WidgetPlacement.Fill, WidgetPlacement.Fill);

            this.Tabs.OnCollectionChanged += Tabs_OnCollectionChanged;

            this.Tabs.Insert(0, new PropertyTab(this));
        }

        private void Tabs_OnCollectionChanged(object sender, EventArgs e)
        {
            if (this.curtab == null)
            {
                ShowTab(this.Tabs.FirstOrDefault());
            }
            this.toolbar.Clear();

            foreach (var tab in this.Tabs)
            {
                this.toolbar.Add(NewButton(tab));
            }
        }
        private Button NewButton(PropertyTab tab)
        {
            var r = new Button();
            r.Clicked += select_tab;
            r.Label = tab.TabText;
            r.Tag = tab;
            return r;
        }
        private void select_tab(object sender, EventArgs e)
        {
            ShowTab((sender as Widget).Tag as PropertyTab);
        }
        public void ShowTab(PropertyTab tab)
        {
            if (this.curtab != null)
            {
                this.viewtable.Remove(this.curtab);
            }
            if ((this.curtab = tab) != null)
            {
                this.viewtable.Add(tab, 0, 0, 1, 1, true, true, WidgetPlacement.Fill, WidgetPlacement.Fill);
                tab.Fill();
            }
        }
        protected override void OnBoundsChanged()
        {
            //  this.viewtable.QueueForReallocate();
            base.OnBoundsChanged();
            this.viewtable.QueueForReallocate();
        }
        public void Fill(bool first = true)
        {
            this.curtab?.Fill(first);
        }
        public bool CancelEdit(bool apply)
        {
            return this.curtab?.CancelEdit(apply) ?? true;
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

       /*     public static IWidgetBackend GetBackend(this Widget o)
            {
                return (IWidgetBackend)global::Xwt.Toolkit.CurrentEngine.GetSafeBackend(o);
            }*/
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
                    return Math.Max(0, Math.Min(1, value / ww));
                }
                return .5;
            }
            public static void SetPosition(this HPaned paned, double value)
            {
                //   if (Toolkit.CurrentEngine.Type != ToolkitType.XamMac)
                {
                    /*if (paned.Size.Width <= 0)
                    {
                        Debug.Assert(false);
                    }
                    else*/
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