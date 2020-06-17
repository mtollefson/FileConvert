using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FileConvert {
  public class MainFormClass : Form {
    public static MainFormClass MainForm;
    public string SelectedTypeExtension = string.Empty;
    string FullInputFileName;
    Button SelectInputBtn;
    List<String> FullLog;
    RichTextBox rtb;
    TextBox ApiKeyTBox;
    CheckBox SandboxCkBox;
    CheckBox SameDirectoryCkBox;
    CheckBox OverwriteCkBox;

    public MainFormClass() {
      MainForm = this;
      this.PlaceOnDesktop(50, 50);
      this.ClientSize = new Size(410, 400);
      this.FormBorderStyle = FormBorderStyle.FixedDialog; // no resize
      this.MaximizeBox = false;
      this.Text = "File Conversion by Zamzar.com";
      Application.Idle += Initialize;
    }

    private void Initialize(object sender, System.EventArgs e) {
      if (!this.Visible) return;
      Application.Idle -= Initialize;
      //
      Color apiColor = Color.FromArgb(255, 212, 255, 219);
      FullLog = new List<String>();
      //
      Label apiLbl = new Label();
      apiLbl.AddToParentAndPlace(this, 5, 5);
      apiLbl.FontSize(12);
      apiLbl.Text = "API Key:";
      apiLbl.BackColor = apiColor;
      //
      ApiKeyTBox = new TextBox();
      ApiKeyTBox.PlaceUnderLast(this, 0);
      ApiKeyTBox.Size = new Size(this.ClientRectangle.Width - 10, 20);
      ApiKeyTBox.FontSize(12);
      ApiKeyTBox.BackColor = apiColor;
      //
      SandboxCkBox = new CheckBox();
      SandboxCkBox.PlaceUnderLast(this);
      SandboxCkBox.Text = "Use Sandbox";
      SandboxCkBox.AutoSize = true;
      SandboxCkBox.FontSize(12);
      //
      SameDirectoryCkBox = new CheckBox();
      SameDirectoryCkBox.PlaceRightOf(SandboxCkBox, 25);
      SameDirectoryCkBox.Text = "Place Output files with Input file";
      SameDirectoryCkBox.AutoSize = true;
      SameDirectoryCkBox.Checked = true;
      SameDirectoryCkBox.FontSize(12);
      //
      OverwriteCkBox = new CheckBox();
      OverwriteCkBox.PlaceBelow(SameDirectoryCkBox);
      OverwriteCkBox.Text = "Overwrite existing files";
      OverwriteCkBox.AutoSize = true;
      OverwriteCkBox.Checked = false;
      OverwriteCkBox.FontSize(12);
      //
      SelectInputBtn = new Button();
      SelectInputBtn.PlaceUnderLast(this);
      SelectInputBtn.Text = "Select Input File";
      SelectInputBtn.AutoSize = true;
      SelectInputBtn.FontSize(12);
      SelectInputBtn.Show();
      SelectInputBtn.BackColor = Color.White;
      SelectInputBtn.Click += SelectInputBtn_Click;
      //
      rtb = new RichTextBox();
      rtb.PlaceUnderLast(this);
      rtb.Width = ClientSize.Width - 2 * rtb.Left;
      rtb.Height = ClientSize.Height - rtb.Location.Y - 12;
      rtb.BackColor = Color.LightBlue;
      rtb.FontSize(12);
      //
      Log("\n  1. Insure your API Key appears above.\n  2.Select option checkboxes\n  3.Press \"Select Input File\" button.");

      // try to get saved state to restore controls
      SaveState.SaveData saved = SaveState.Get();
      if (saved == null) {
        ApiKeyTBox.Text = string.Empty;
        SandboxCkBox.Checked = true;
        SameDirectoryCkBox.Checked = true;
        OverwriteCkBox.Checked = false;
      } else {
        ApiKeyTBox.Text = saved.apikey;
        ApiKeyTBox.SelectionStart = ApiKeyTBox.Text.Length;
        SandboxCkBox.Checked = saved.Sandbox;
        SameDirectoryCkBox.Checked = saved.SameDirectory;
        OverwriteCkBox.Checked = saved.Overwrite;
      }
    }

    private void SelectInputBtn_Click(object sender, System.EventArgs e) {
      ControlsEnabled(false);
      Start();
    }

    private void Start() {
      // this will do all communication with the server
      ServerWorker Server = new ServerWorker(ApiKeyTBox, SandboxCkBox);
      //
      Log("Clear");

      // have user select input file
      Log("Selecting Input file");
      UserSelectInputFile();
      if (FullInputFileName == string.Empty) {
        Log("Clear");
        Log("No input file selected");
        ControlsEnabled(true);
        return;
      }
      Log("Input file: " + Path.GetFileName(FullInputFileName));

      // get possible target formats
      (ErrStruct, tgtdata) ErrAndTgt = Server.GetTargetTypes(Path.GetExtension(FullInputFileName));
      ErrStruct err = ErrAndTgt.Item1;
      tgtdata tdat = ErrAndTgt.Item2;
      if (err.errors != null) {
        Log("Error from Server:");
        foreach (OneError oneE in err.errors) {
          string msg = oneE.message;
          if (msg == "resource does not exist") msg = "Can not convert a file of this type.";
          Log("  " + msg + "  (code: " + oneE.code + ")");
        }
        Util.ErrorBeep();
        ControlsEnabled(true);
        return;
      }

      // have user select destination format
      UserSelectTargetFileType(tdat.targets);
      if (SelectedTypeExtension == string.Empty) {
        Log("No conversion Format selected.");
        ControlsEnabled(true);
        return;
      }
      Log("Convert file to ." + SelectedTypeExtension);

      // determine where output file(s) should be placed
      string DestFolder = Path.GetDirectoryName(FullInputFileName);
      if (!SameDirectoryCkBox.Checked) {
        // ask user where to put the files
        bool UseWindowsDefaultUglyBrowser = false;
        if (UseWindowsDefaultUglyBrowser) {
          FolderBrowserDialog folderDlg = new FolderBrowserDialog();
          folderDlg.ShowNewFolderButton = true;
          folderDlg.SelectedPath = DestFolder;
          folderDlg.Description = "Where should output files be placed?";
          DialogResult result = folderDlg.ShowDialog(this);
          if (result != DialogResult.OK) {
            Log("No Destination folder was selected.");
            ControlsEnabled(true);
            return;
          }
          DestFolder = folderDlg.SelectedPath;
        } else {
          WKLib.BetterFolderBrowser bfb = new WKLib.BetterFolderBrowser();
          bfb.RootFolder = DestFolder.Substring(0, DestFolder.LastIndexOf('\\'));
          bfb.Title = "Where should output files be placed?";
          DialogResult result = bfb.ShowDialog();
          if (result != DialogResult.OK) {
            Log("No Destination folder was selected.");
            ControlsEnabled(true);
            return;
          }
          DestFolder = bfb.SelectedFolder;
        }
      }

      // insure the output directory exists (it always should, in theory)
      bool okay = Util.DirectoryExistsOrWasCreated(DestFolder);
      if (!okay) {
        ControlsEnabled(true);
        Log("ERROR");
        Log("  Can not create output directory: ");
        Log("  " + DestFolder);
        return;
      }

      // insure we can write to the selected output directory
      okay = Util.DirectoryIsWriteable(DestFolder);
      if(!okay) { 
        ControlsEnabled(true);
        Log("ERROR:");
        Log("  Can not write to destination directory:");
        Log("  " + DestFolder);
        return;
      }

      // send input file to server and start conversion job
      (ErrStruct, jobstatus) ErrAndJob = Server.UploadFileStartConversion(FullInputFileName, SelectedTypeExtension);
      err = ErrAndJob.Item1;
      jobstatus jobstat = ErrAndJob.Item2;
      if (err.errors != null) {
        Log("Error from Server:");
        foreach (OneError oneE in err.errors) {
          string msg = oneE.message;
          Log("  " + msg + "  (code: " + oneE.code + ")");
        }
        Util.ErrorBeep();
        ControlsEnabled(true);
        return;
      }
      string msg2 = string.Format("job id = {0}, status = {1}", jobstat.id, jobstat.status);
      Log(msg2);

      // wait for conversion completion by the server
      (ErrStruct, jobstatus) StatusUpdate = Server.AwaitCompletion(jobstat.id, this);
      jobstat = StatusUpdate.Item2;
      Log(string.Format("Completion status: {0}", jobstat.status));
      Log(string.Format("Conversion cost:   {0} credit", jobstat.credit_cost));

      // download and save the completed file/files
      bool overwrite = OverwriteCkBox.Checked;
      Server.DownloadFiles(DestFolder, jobstat.target_files, overwrite);

      Log("Done.");
      //
      SaveState.Save(ApiKeyTBox.Text, DestFolder, OverwriteCkBox.Checked, SameDirectoryCkBox.Checked, SandboxCkBox.Checked);

      // done
      ControlsEnabled(true);
    }

    private void UserSelectInputFile() {
      using (OpenFileDialog ofd = new OpenFileDialog()) {
        DialogResult result = ofd.ShowDialog();
        FullInputFileName = string.Empty;
        if (result != DialogResult.OK) {
          FullInputFileName = string.Empty;
          return;
        }
        FullInputFileName = ofd.FileName;
      }
    }

    private static void UserSelectTargetFileType(List<atarget> targets) {
      Form frm = new Form();
      frm.NoTopIcons();
      frm.FormBorderStyle = FormBorderStyle.FixedDialog;
      frm.PlaceOnDesktop(MainForm.Location.X + 50, MainForm.Location.Y + 220, 300, 300);
      frm.BackColor = Color.Beige;
      //
      Label topLbl = new Label();
      topLbl.AddToParentAndPlace(frm);
      topLbl.Text = "Select Output Format";
      topLbl.BackColor = Color.FromArgb(255, 212, 255, 219);
      topLbl.FontSize(12);
      topLbl.AutoSize = true;
      topLbl.Show();
      //
      for (int i = 0; i < targets.Count; i++) {
        atarget tgt = targets[i];
        CheckBox ckbx = new CheckBox();
        ckbx.PlaceUnderLast(frm);
        ckbx.Text = string.Format(".{0}", tgt.name);
        ckbx.FontSize(12);
        ckbx.AutoSize = true;
        ckbx.Show();
        //
        Label lbl = new Label();
        lbl.PlaceRightOf(ckbx);
        lbl.Location = new Point(150, lbl.Location.Y);
        lbl.Text = string.Format("(cost = {0} credit)", tgt.credit_cost);
        lbl.FontSize(12);
        lbl.AutoSize = true;
        lbl.Show();
      }
      //
      Button OkBtn = new Button();
      OkBtn.PlaceUnderLast(frm);
      OkBtn.Text = "OK";
      OkBtn.FontSize(12);
      OkBtn.Height += 5;
      OkBtn.BackColor = Color.White;
      OkBtn.Click += UserSelectTargetBtn_Click;
      //
      Button CancelBtn = new Button();
      CancelBtn.PlaceRightOf(OkBtn);
      CancelBtn.Text = "Cancel";
      CancelBtn.FontSize(12);
      CancelBtn.BackColor = Color.White;
      CancelBtn.Height += 5;
      CancelBtn.Location = new Point(frm.ClientSize.Width - CancelBtn.Width - 5, CancelBtn.Location.Y);
      CancelBtn.Click += UserSelectTargetBtn_Click;
      //
      int bottom = OkBtn.Location.Y + OkBtn.Height + 5;
      frm.ClientSize = new Size(frm.ClientSize.Width, bottom);
      var ScreenRect = Screen.FromControl(MainForm).Bounds;
      if (frm.Top + frm.Height > ScreenRect.Height) frm.Location = new Point(frm.Location.X, 0);
      frm.ShowDialog();
    }

    private static void UserSelectTargetBtn_Click(object sender, EventArgs e) {
      Button btn = (Button)sender;
      Form form = (Form)btn.Parent;
      //List<Control> CtrlList = form.Controls.Cast<Control>().ToList();
      if (btn.Text == "OK") {
        IEnumerable<Control> BoxesChecked =
          form.GetControlsAsList()
          .Where(z => z.GetType() == typeof(CheckBox))
          .Where(z => ((CheckBox)z).Checked);
        if (BoxesChecked.Count() == 1) {
          // get the desired format from the checkbox Text
          CheckBox cb = (CheckBox)(BoxesChecked.First());
          MainForm.SelectedTypeExtension = cb.Text.Substring(1);
          form.Close();
        } else {
          Util.ErrorBeep();
          MessageBox.Show("Select exactly one format.");
        }
      } else {
        // Cancel button
        MainForm.SelectedTypeExtension = string.Empty;
        form.Close();
      }
    }

    public void Log(string msg) {
      if (msg == "Clear") {
        FullLog.Clear();
        rtb.Text = string.Empty;
        return;
      }
      // break a multi-line message into separate lines we can count
      string[] lines = msg.Split('\n');
      foreach (string line in lines) {
        FullLog.Add(line);
      }
      int maxlines = 12; // number of lines rtb can display
      StringBuilder sb = new StringBuilder();
      int start = Math.Max(FullLog.Count - maxlines, 0);
      int stop = Math.Min(FullLog.Count, start + maxlines);
      for (int i = start; i < stop; i++) sb.AppendLine(FullLog[i]);
      rtb.Text = sb.ToString();
      rtb.Select(rtb.Text.Length - 1, 0);
      rtb.Invalidate();
      Application.DoEvents();
    }

    void ControlsEnabled(bool choice) {
      ApiKeyTBox.Enabled = choice;
      SandboxCkBox.Enabled = choice;
      SameDirectoryCkBox.Enabled = choice;
      OverwriteCkBox.Enabled = choice;
      SelectInputBtn.Enabled = choice;
    }

    protected override void OnPaint(PaintEventArgs e) {
      base.OnPaint(e);
      Graphics g = e.Graphics;
      //Console.WriteLine(e.ClipRectangle+"     "+DateTime.Now);
      Font fnt = new Font(this.Font.FontFamily, 8);
      string CpyRight = (char)169 + " 2020 Mark V.Tollefson";
      g.DrawString(CpyRight, fnt, Brushes.Blue, 90, e.ClipRectangle.Height-13);
    }

  }
}
