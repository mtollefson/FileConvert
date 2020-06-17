using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileConvert {
  public static class ExtensionMethods {
    static Dictionary<Control, Control> ParentAndLast = new Dictionary<Control, Control>();

    // "Form" class extension methods -----------------------------------------------------------------

    /// <summary>
    /// easily put form in desired location on desktop
    /// </summary>
    /// <param name="x">destination</param>
    /// <param name="y">destination</param>
    public static void PlaceOnDesktop(this Form frm, int x, int y, int w = -1, int h = -1) {
      // ususally in form's constructor use syntax:  this.PlaceOnDesktop(100, 100, 300, 300);
      frm.StartPosition = FormStartPosition.Manual;
      frm.Location = new Point(x, y);
      if (Math.Min(w, y) > 0) frm.Size = new Size(w, h);
    }

    public static void NoTopIcons(this Form frm) {
      // only way to remove the close icon is to remove them all with this
      frm.ControlBox = false;
    }

    public static List<Control> GetControlsAsList(this Control ctrl) {
      List<Control> CtrlList = ctrl.Controls.Cast<Control>().ToList();
      return CtrlList;
    }

    // "Control" class extension methods -----------------------------------------------------------------

    public static void AddToParentAndPlace(this Control child, Control parent, int x=5, int y=5, int w=-1, int h = -1) {
      ParentAndLast[parent] = child;
      //ParentAndLast.Add(parent, child);
      parent.Controls.Add(child);
      child.Location = new Point(x, y);
      if (Math.Min(w, h) > 0) child.Size = new Size(w, h);
    }

    public static void PlaceUnderLast(this Control child, Control parent, int sep = 5, int w = -1, int h = -1) {
      parent.Controls.Add(child);
      Control Last = ParentAndLast[parent];
      child.Location = new Point(Last.Location.X, Last.Location.Y+Last.Height+sep);
      ParentAndLast[parent] = child;
      //ParentAndLast.Add(parent, child);
      if (Math.Min(w, h) > 0) child.Size = new Size(w, h);
    }

    public static void PlaceRightOf(this Control newer, Control existing, int sep = 5, int w = -1, int h = -1) {
      // note: this does NOT set the Last Control in ParentAndLast
      existing.Parent.Controls.Add(newer);
      newer.Location = new Point(existing.Location.X+existing.Width+sep, existing.Location.Y);
      if (Math.Min(w, h) > 0) newer.Size = new Size(w, h);
    }

    public static void PlaceBelow(this Control newer, Control existing, int sep = 5, int w = -1, int h = -1) {
      // note: this does NOT set the Last Control in ParentAndLast
      existing.Parent.Controls.Add(newer);
      newer.Location = new Point(existing.Location.X, existing.Location.Y+existing.Height+sep);
      if (Math.Min(w, h) > 0) newer.Size = new Size(w, h);
    }

    public static void FontSize(this Control ctrl, float points) {
      ctrl.Font = new Font(ctrl.Font.FontFamily, points);
    }




  }
}
