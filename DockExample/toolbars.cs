using BaseLib.Xwt;
using BaseLib.Xwt.Design;
using BaseLib.Xwt.DockPanel;
using BaseLib.Xwt.PropertyGrid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Xwt;
using Xwt.Drawing;

namespace DockExample
{
    partial class mainwindow
    {
        class testtoolitem : Canvas, IDockToolbar, IDockSerializable
        {
            Widget IDockContent.Widget => this;
            string IDockContent.TabText => $"tool{this.Id}";
            IDockPane IDockContent.DockPane { get; set; }

            public int Id { get; }

            public testtoolitem(mainwindow main)
                : this(main.dock)
            {
            }
            private testtoolitem(DockPanel dock)
            {
                var w = dock.AllContent.OfType<testtoolitem>().Select(_tw => _tw.Id);

                if (w.Any())
                {
                    Id = w.Max() + 1;
                }
                else
                {
                    Id = 1;
                }
                Initialize();
            }
            public testtoolitem(string data)
            {
                this.Id = int.Parse(data);
                Initialize();
            }
            void Initialize()
            {
                this.MinWidth = this.MinHeight = 10;
                this.BackgroundColor = Colors.Aquamarine;
            }
            string IDockSerializable.Serialize()
            {
                return this.Id.ToString();
            }
        }



        class testpropertiesitem : Canvas, IDockToolbar
        {
            private PropertyGrid widget;
            private IDockPane dockpane;

            Widget IDockContent.Widget => this;
            string IDockContent.TabText => $"Properties";
            IDockPane IDockContent.DockPane
            {
                get => this.dockpane;
                set
                {
                    if (this.dockpane != null)
                    {
                        this.dockpane.DockPanel.ActiveDocumentChanged -= DockPanel_ActiveDocumentChanged;
                    }
                    if ((this.dockpane = value) != null)
                    {
                        this.dockpane.DockPanel.ActiveDocumentChanged += DockPanel_ActiveDocumentChanged;
                    }
                }
            }

            class ICollectionEditor : UITypeEditor
            {
                public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
                {
                    return UITypeEditorEditStyle.Modal;
                }
                public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
                {
                    var svc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

                    var dialog = new Dialog();
                    dialog.Content = new Label("hello");

                    var cmd = svc.ShowDialog(dialog);

                    return value;
                }
            }

            class ICollectionConverter : CollectionConverter
            {
                public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
                {
                    if (sourceType == typeof(string)) { return false; }
                    return base.CanConvertFrom(context, sourceType);
                }
                public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
                {
                    if (destinationType == typeof(string)) { return true; }
                    return base.CanConvertTo(context, destinationType);
                }
                public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
                {
                    if (destinationType == typeof(string))
                    {
                        return $"Count={(value as ICollection)?.Count}";
                    }
                    return base.ConvertTo(context, culture, value, destinationType);
                }
            }

            [TypeConverter(typeof(ExpandableObjectConverter))]
            public struct point
            {
                public int X { get; set; }
                public int Y { get; set; }
            }
                [TypeConverter(typeof(ExpandableObjectConverter))]
                class testobject
            {
                [Category("abc")]
                public point pt { get; set; } = new point();

                [Category("abc")]
                [DefaultValue("hello")]
                public string Text { get; set; }

                public Int32 getal { get; set; }

                [Editor(typeof(ICollectionEditor), typeof(UITypeEditor))]
                [TypeConverter(typeof(ICollectionConverter))]
                public object[][] testarray { get; set; } = new object[][] { new object[] { "abc", 0 } };

                [Editor(typeof(ICollectionEditor), typeof(UITypeEditor))]
                [TypeConverter(typeof(ICollectionConverter))]
                [BrowsableAttribute(true)]
                public List<Int32> testlist { get; set; } = new List<int>(new int[] { 2, 3 });

                public enum testenum
                {
                    een,
                    twee,
                    drie
                }

                public testenum test { get; set; }

           //     [Category("test")]
            //    public System.Drawing.Size size { get; set; } = new System.Drawing.Size();
            //    public System.Drawing.Rectangle rect { get; set; } = new System.Drawing.Rectangle();
            }
            private void DockPanel_ActiveDocumentChanged(object sender, EventArgs e)
            {
                this.widget.SelectedObject = new testobject();
            }
            
            public testpropertiesitem()
            {
                Initialize();
            }
            void Initialize()
            {
                this.BackgroundColor = Colors.Aquamarine;

                this.widget = new PropertyGrid();
                this.AddChild(this.widget);

                this.MinWidth = this.widget.MinWidth;
                this.MinWidth = this.widget.MinHeight;
                this.OnBoundsChanged();

            }
            protected override void OnBoundsChanged()
            {
                this.SetChildBounds(this.widget, this.Bounds);
            }
        }
    }
}