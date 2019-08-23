using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using BaseLib.Xwt.Controls.PropertyGrid;
using Xwt;

namespace DockExample.Samples
{
    class PropertyGrid : FrameBox
    {
        private readonly BaseLib.Xwt.Controls.PropertyGrid.PropertyGrid propgrid;

        [TypeConverter(typeof(TestConverter))]
        public class TestSettings
        {
            class TestConverter: TypeConverter
            {
                public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
                {
                    return TypeDescriptor.GetProperties(value ,attributes);
                }
                public override bool GetPropertiesSupported(ITypeDescriptorContext context)
                {
                    return true;
                }
            }
            [Browsable(true)]
            public System.Drawing.Size WindowSize { get; set; } = new System.Drawing.Size(200, 200);
        }

        public PropertyGrid()
        {
            this.Content = this.propgrid = new BaseLib.Xwt.Controls.PropertyGrid.PropertyGrid();
            this.Margin = 0;

            this.propgrid.SelectedObject = new TestSettings();
        }
    }
}
