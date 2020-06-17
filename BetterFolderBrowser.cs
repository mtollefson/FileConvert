/*
 * Developer    : Willy Kimura (WK).
 * Library      : BetterFolderBrowser.
 * License      : MIT.
 * 
 * This .NET component was written to help developers
 * provide a better folder-browsing and selection
 * experience to users by employing the old Windows
 * Vista folder browser dialog in place of the current
 * 'FolderBrowserDialog' tree-view style. This dialog
 * implementation mimics the 'OpenFileDialog' design
 * which allows for a much easier viewing, selection, 
 * and search experience using Windows Explorer.
 * 
 * Improvements are always welcome :)
 * 
 * Modified by Tollefson
 *   mainly removed designer support
 *   renamed namespace for brevity
 *   renamed .cs files
 */

using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using System.ComponentModel.Design;

namespace WKLib {
  /// <summary>
  /// A Windows Forms component that enhances the standard folder-browsing experience.
  /// </summary>
  public class BetterFolderBrowser : Form {

    public BetterFolderBrowser() {
      SetDefaults();
    }

    public BetterFolderBrowser(IContainer container) {
      container.Add(this);
      SetDefaults();
    }

    // warning message associated with this is in Error
    // Helpers refers to namespace WKLib.Helpers
    private Helpers.BetterFolderBrowserDialog bfDialog = new Helpers.BetterFolderBrowserDialog();

    /// <summary>
    /// Gets or sets the folder dialog box title.
    /// </summary>
    public string Title {
      get { return bfDialog.Title; }
      set { bfDialog.Title = value; }
    }

    /// <summary>
    /// Gets or sets the root folder where the browsing starts from.
    /// </summary>
    public string RootFolder {
      get { return bfDialog.InitialDirectory; }
      set { bfDialog.InitialDirectory = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the 
    /// dialog box allows multiple folders to be selected.
    /// </summary>
    public bool Multiselect {
      get { return bfDialog.AllowMultiselect; }
      set { bfDialog.AllowMultiselect = value; }
    }

    /// <summary>
    /// Gets the folder-path selected by the user.
    /// </summary>
    public string SelectedPath {
      get { return bfDialog.FileName; }
    }

    /// <summary>
    /// Gets the list of folder-paths selected by the user.
    /// </summary>
    public string[] SelectedPaths {
      get { return bfDialog.FileNames; }
    }

    /// <summary>
    /// Variant of <see cref="SelectedPath"/> property.
    /// Gets the folder-path selected by the user.
    /// </summary>
    public string SelectedFolder {
      get { return bfDialog.FileName; }
    }

    /// <summary>
    /// Variant of <see cref="SelectedPaths"/> property.
    /// Gets the list of folder-paths selected by the user.
    /// </summary>
    public string[] SelectedFolders {
      get { return bfDialog.FileNames; }
    }


    private void SetDefaults() {
      bfDialog.AllowMultiselect = false;
      bfDialog.Title = "Please select a folder...";
      bfDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    /// <summary>
    /// Runs a common dialog box with a default owner.
    /// </summary>
    public DialogResult ShowDialog(int x = -1, int y = -1) {
      // following added, but doesn't seem to do anything
      if (Math.Min(x, y) >= 0) {
        StartPosition = FormStartPosition.Manual;
        Location = new Point(x, y);
      }
      if (bfDialog.ShowDialog(IntPtr.Zero)) return DialogResult.OK;
      return DialogResult.Cancel;
    }

    /// <summary>
    /// Runs a common dialog box with the specified owner.
    /// </summary>
    /// <param name="owner">
    /// Any object that implements <see cref="IWin32Window"/> that represents
    /// the top-level window that will own the modal dialog box.
    /// </param>
    public new DialogResult ShowDialog(IWin32Window owner) {
      DialogResult result = DialogResult.Cancel;

      if (bfDialog.ShowDialog(owner.Handle))
        result = DialogResult.OK;
      else
        result = DialogResult.Cancel;

      return result;
    }

  }
}
