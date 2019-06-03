using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Xwt;

namespace BaseLib.Xwt.Design
{
    //
    // Summary:
    //     Provides an interface for a System.Drawing.Design.UITypeEditor to display Windows
    //     Forms or to display a control in a drop-down area from a property grid control
    //     in design mode.
    public interface IWindowsFormsEditorService
    {
        //
        // Summary:
        //     Closes any previously opened drop down control area.
        void CloseDropDown();
        //
        // Summary:
        //     Displays the specified control in a drop down area below a value field of the
        //     property grid that provides this service.
        //
        // Parameters:
        //   control:
        //     The drop down list System.Windows.Forms.Control to open.
        void DropDownControl(Widget control);
        //
        // Summary:
        //     Shows the specified System.Windows.Forms.Form.
        //
        // Parameters:
        //   dialog:
        //     The System.Windows.Forms.Form to display.
        //
        // Returns:
        //     A System.Windows.Forms.DialogResult indicating the result code returned by the
        //     System.Windows.Forms.Form.
        Command ShowDialog(Dialog dialog);
    }
    //
    // Summary:
    //     Specifies identifiers that indicate the value editing style of a System.Drawing.Design.UITypeEditor.
    public enum UITypeEditorEditStyle
    {
        //
        // Summary:
        //     Provides no interactive user interface (UI) component.
        None = 1,
        //
        // Summary:
        //     Displays an ellipsis (...) button to start a modal dialog box, which requires
        //     user input before continuing a program, or a modeless dialog box, which stays
        //     on the screen and is available for use at any time but permits other user activities.
        Modal = 2,
        //
        // Summary:
        //     Displays a drop-down arrow button and hosts the user interface (UI) in a drop-down
        //     dialog box.
        DropDown = 3
    }
    //
    // Summary:
    //     Provides a base class that can be used to design value editors that can provide
    //     a user interface (UI) for representing and editing the values of objects of the
    //     supported data types.
    public class UITypeEditor
    {
        //
        // Summary:
        //     Initializes a new instance of the System.Drawing.Design.UITypeEditor class.
        public UITypeEditor()
        {
        }

        //
        // Summary:
        //     Gets a value indicating whether drop-down editors should be resizable by the
        //     user.
        //
        // Returns:
        //     true if drop-down editors are resizable; otherwise, false.
        public virtual bool IsDropDownResizable => true;

        //
        // Summary:
        //     Edits the value of the specified object using the editor style indicated by the
        //     System.Drawing.Design.UITypeEditor.GetEditStyle method.
        //
        // Parameters:
        //   provider:
        //     An System.IServiceProvider that this editor can use to obtain services.
        //
        //   value:
        //     The object to edit.
        //
        // Returns:
        //     The new value of the object.
        public object EditValue(IServiceProvider provider, object value) => EditValue(null, provider, value);
        //
        // Summary:
        //     Edits the specified object's value using the editor style indicated by the System.Drawing.Design.UITypeEditor.GetEditStyle
        //     method.
        //
        // Parameters:
        //   context:
        //     An System.ComponentModel.ITypeDescriptorContext that can be used to gain additional
        //     context information.
        //
        //   provider:
        //     An System.IServiceProvider that this editor can use to obtain services.
        //
        //   value:
        //     The object to edit.
        //
        // Returns:
        //     The new value of the object. If the value of the object has not changed, this
        //     should return the same object it was passed.
        public virtual object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) => value;
        //
        // Summary:
        //     Gets the editor style used by the System.Drawing.Design.UITypeEditor.EditValue(System.IServiceProvider,System.Object)
        //     method.
        //
        // Returns:
        //     A System.Drawing.Design.UITypeEditorEditStyle enumeration value that indicates
        //     the style of editor used by the current System.Drawing.Design.UITypeEditor. By
        //     default, this method will return System.Drawing.Design.UITypeEditorEditStyle.None.
        public UITypeEditorEditStyle GetEditStyle() => GetEditStyle(null);
        //
        // Summary:
        //     Gets the editor style used by the System.Drawing.Design.UITypeEditor.EditValue(System.IServiceProvider,System.Object)
        //     method.
        //
        // Parameters:
        //   context:
        //     An System.ComponentModel.ITypeDescriptorContext that can be used to gain additional
        //     context information.
        //
        // Returns:
        //     A System.Drawing.Design.UITypeEditorEditStyle value that indicates the style
        //     of editor used by the System.Drawing.Design.UITypeEditor.EditValue(System.IServiceProvider,System.Object)
        //     method. If the System.Drawing.Design.UITypeEditor does not support this method,
        //     then System.Drawing.Design.UITypeEditor.GetEditStyle will return System.Drawing.Design.UITypeEditorEditStyle.None.
        public virtual UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.None;
#if (false)
        //
        // Summary:
        //     Indicates whether this editor supports painting a representation of an object's
        //     value.
        //
        // Returns:
        //     true if System.Drawing.Design.UITypeEditor.PaintValue(System.Object,System.Drawing.Graphics,System.Drawing.Rectangle)
        //     is implemented; otherwise, false.
        public bool GetPaintValueSupported() => false;
        //
        // Summary:
        //     Indicates whether the specified context supports painting a representation of
        //     an object's value within the specified context.
        //
        // Parameters:
        //   context:
        //     An System.ComponentModel.ITypeDescriptorContext that can be used to gain additional
        //     context information.
        //
        // Returns:
        //     true if System.Drawing.Design.UITypeEditor.PaintValue(System.Object,System.Drawing.Graphics,System.Drawing.Rectangle)
        //     is implemented; otherwise, false.
        public virtual bool GetPaintValueSupported(ITypeDescriptorContext context);
        //
        // Summary:
        //     Paints a representation of the value of the specified object to the specified
        //     canvas.
        //
        // Parameters:
        //   value:
        //     The object whose value this type editor will display.
        //
        //   canvas:
        //     A drawing canvas on which to paint the representation of the object's value.
        //
        //   rectangle:
        //     A System.Drawing.Rectangle within whose boundaries to paint the value.
        public void PaintValue(object value, Graphics canvas, Rectangle rectangle);
        //
        // Summary:
        //     Paints a representation of the value of an object using the specified System.Drawing.Design.PaintValueEventArgs.
        //
        // Parameters:
        //   e:
        //     A System.Drawing.Design.PaintValueEventArgs that indicates what to paint and
        //     where to paint it.
        public virtual void PaintValue(PaintValueEventArgs e);
#endif
    }
}
namespace BaseLib.Xwt.Controls.PropertyGrid
{
    public enum PropertySort
    {
        NoSort,
        Alphabetical,
        Categorized,
        CategorizedAlphabetical
    }
}
