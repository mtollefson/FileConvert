using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileConvert {
  public static class SaveState {
    const string SaveStateFilename = "FileConvertState.txt";
    static SaveData MostRecent;

    // static constructor run automatically
    static SaveState() {
      ReadFile();
    }

    public static SaveData Get() {
      return MostRecent;
    }

    public static void Save(string apikey, string destDirectory, bool Overwrite, bool SameDirectory, bool Sandbox) {
      if (MostRecent == null) MostRecent = new SaveData();
      MostRecent.apikey = apikey;
      MostRecent.Overwrite = Overwrite;
      MostRecent.SameDirectory = SameDirectory;
      MostRecent.Sandbox = Sandbox;
      if (apikey == null) { // never is true
        // this and related sections work correctly
        // disabled for now by never saving the folder name
        // intent was to give the user a choice of recent destinations or search for a new one
        // not sure it wouldn't be confusing instead
        int loc = MostRecent.DirList.IndexOf(destDirectory);
        if (loc >= 0) {
          MostRecent.DTimeList[loc] = DateTime.Now;
        } else {
          MostRecent.DirList.Add(destDirectory);
          MostRecent.DTimeList.Add(DateTime.Now);
        }
      }
      WriteFile();
    }

    private static void ReadFile() {
      if (!File.Exists(SaveStateFilename)) {
        Console.WriteLine("Saved State File does not exist.");
        return;
      }
      try {
        char[] splitchars = { '\r', '\n' };
        string[] lines = File.ReadAllLines(SaveStateFilename);
        SaveData sd = new SaveData();
        sd.apikey = lines[0];
        sd.Overwrite = bool.Parse(lines[1]);
        sd.SameDirectory = bool.Parse(lines[2]);
        sd.Sandbox = bool.Parse(lines[3]);
        int count = int.Parse(lines[4]);
        for (int i = 0; i < count; i++) {
          sd.DirList.Add(lines[5 + (2 * i)]);
          sd.DTimeList.Add(DateTime.Parse(lines[6 + (2 * i)]));
        }
        MostRecent = sd;
      } catch (Exception e) {
        Console.WriteLine(e.Message);
        MainFormClass.MainForm.Log("Internal Error:");
        MainFormClass.MainForm.Log("  Could not read saved state file:");
        MainFormClass.MainForm.Log("  " + SaveStateFilename);
      }
    }

    private static void WriteFile() {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine(MostRecent.apikey);
      sb.AppendLine(MostRecent.Overwrite.ToString());
      sb.AppendLine(MostRecent.SameDirectory.ToString());
      sb.AppendLine(MostRecent.Sandbox.ToString());
      int count = MostRecent.DirList.Count;
      sb.AppendLine(count.ToString());
      for(int i=0; i<count; i++) {
        sb.AppendLine(MostRecent.DirList[i]);
        sb.AppendLine(MostRecent.DTimeList[i].ToString());
      }
      File.WriteAllText(SaveStateFilename, sb.ToString());
    }

    public class SaveData {
      public string apikey;
      public bool Overwrite;
      public bool SameDirectory;
      public bool Sandbox;
      public List<string> DirList;
      public List<DateTime> DTimeList;
      public SaveData() {  // constructor
        apikey = string.Empty;
        DirList = new List<string>();
        DTimeList = new List<DateTime>();
      }
    }

  }
}
