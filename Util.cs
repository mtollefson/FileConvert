using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FileConvert {
  public static class Util {
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool Beep(uint dFreq, uint dwDuration);
    public static void NiceBeep(int freq = 500, int msec = 100) {
      Beep((uint)freq, (uint)msec);
    }
    internal static void ErrorBeep() {
      NiceBeep(200, 300);
    }

    /// <summary>
    /// insure destFolder exists
    /// </summary>
    /// <param name="destFolder">desired folder name</param>
    /// <returns>true if it now exists</returns>
    internal static bool DirectoryExistsOrWasCreated(string destFolder) {
      if (Directory.Exists(destFolder)) return true;
      try {
        Directory.CreateDirectory(destFolder);
      } catch {
        return false;
      }
      return true;
    }

    internal static bool DirectoryIsWriteable(string destFolder) {
      string timestring = string.Format("{0:HHmmss}", DateTime.Now);
      string testfilename = Path.Combine(destFolder, "Junk_" + timestring+".txt");
      try {
        File.Delete(testfilename);
        using (FileStream x = File.Create(testfilename)) { x.Close(); }
        File.Delete(testfilename);
      } catch {
        return false;
      }
      return true;
    }

  }
}
