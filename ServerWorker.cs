using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serialization.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileConvert {
  public class ServerWorker {
    string apiKey;
    string ZServer;

    public ServerWorker(TextBox apiKeyTxtBox, CheckBox SandboxCkBox) {
      apiKey = apiKeyTxtBox.Text;
      ZServer = "https://api.zamzar.com/v1/";
      if (SandboxCkBox.Checked) ZServer = "https://sandbox.zamzar.com/v1/";
    }

    public (ErrStruct, tgtdata) GetTargetTypes(string TargetFormat) {
      // get target types available from the source format
      if (TargetFormat.StartsWith(".")) TargetFormat = TargetFormat.Substring(1);
      RestClient client = new RestClient(ZServer + "formats/");
      client.Authenticator = new HttpBasicAuthenticator(apiKey, "");
      RestRequest request = new RestRequest(TargetFormat, DataFormat.Json);
      IRestResponse response = client.Get(request);
      JsonDeserializer deserializer = new JsonDeserializer();
      if (response.StatusCode == HttpStatusCode.OK) {
        tgtdata td = deserializer.Deserialize<tgtdata>(response);
        return (new ErrStruct(), td);
      } else { 
        ErrStruct err = deserializer.Deserialize<ErrStruct>(response);
        return (err, new tgtdata());
      }
    }

    public (ErrStruct, jobstatus) UploadFileStartConversion(string FullInputFileName, string TargetFormat) {
      // upload the input file
      RestClient client = new RestClient(ZServer);
      client.Authenticator = new HttpBasicAuthenticator(apiKey, "");
      RestRequest request = new RestRequest("jobs/", Method.POST, DataFormat.Json);
      request.AddParameter("target_format", TargetFormat);
      request.AddFile("source_file", FullInputFileName);
      request.AlwaysMultipartFormData = true;
      IRestResponse response = client.Execute(request);
      Console.WriteLine(response.Content);
      Console.WriteLine(response.ResponseUri);
      JsonDeserializer deserializer = new JsonDeserializer();
      if (response.StatusCode == HttpStatusCode.Created) {
        jobstatus InitialStatus = deserializer.Deserialize<jobstatus>(response);
        return (new ErrStruct(), InitialStatus);
      } else {
        ErrStruct err = deserializer.Deserialize<ErrStruct>(response);
        return (err, new jobstatus());
      }
    }

    internal (ErrStruct, jobstatus) AwaitCompletion(int id, MainFormClass mainForm) {
      string ConversionStatus = string.Empty;
      jobstatus StatusUpdate;
      RestClient client = new RestClient(ZServer + "jobs/");
      client.Authenticator = new HttpBasicAuthenticator(apiKey, "");
      RestRequest request = new RestRequest(id.ToString(), DataFormat.Json);
      JsonDeserializer deserializer = new JsonDeserializer();
      do {
        System.Threading.Thread.Sleep(10000);
        IRestResponse response = client.Get(request);
        if (response.StatusCode != HttpStatusCode.OK) {
          ErrStruct err = deserializer.Deserialize<ErrStruct>(response);
          return (err, new jobstatus());
        }
        //Console.WriteLine(response.Content);
        StatusUpdate = deserializer.Deserialize<jobstatus>(response);
        ConversionStatus = StatusUpdate.status;
        string msg = string.Format("Conversion status = {0} at {1:HH:mm:ss}", ConversionStatus, DateTime.Now);
        mainForm.Log(msg);
        Console.WriteLine("Conversion status = " + ConversionStatus);
      } while (ConversionStatus != "successful");
      return (new ErrStruct(), StatusUpdate);
    }

    internal void DownloadFiles(string DestFolderName, List<aboutfile> target_files, bool overwrite) {
      // download and save the generated file(s)
      RestClient client = new RestClient(ZServer + "files/");
      client.Authenticator = new HttpBasicAuthenticator(apiKey, "");
      foreach (aboutfile about in target_files) {
        RestRequest request = new RestRequest(about.id.ToString() + "/content", Method.GET);
        IRestResponse response = client.Get(request);
        // check for an error
        if (response.StatusCode != HttpStatusCode.OK) {
          JsonDeserializer deserializer = new JsonDeserializer();
          ErrStruct err = deserializer.Deserialize<ErrStruct>(response);
          MainFormClass.MainForm.Log(string.Format("Error downloading {0}", about.name));
          foreach (OneError one in err.errors) {
            MainFormClass.MainForm.Log(string.Format("  {0}   (code {1})", one.message, one.code));
          }
        } else {
          byte[] data = client.DownloadData(request);
          (string,string) ErrAndName = SafelyWriteOutputFile(DestFolderName, about.name, data, overwrite);
          string outfilename = ErrAndName.Item2;
          if (outfilename == string.Empty) {
            MainFormClass.MainForm.Log(ErrAndName.Item1);
          } else {
            //string msg = string.Format("Downloaded: {0}", Path.GetFileName(outfilename));
            string finalDirectory = Path.GetDirectoryName(outfilename);
            string finalName = Path.GetFileName(outfilename);
            string msg = string.Format("Downloaded File:\n  {0}\n  {1}", finalDirectory, finalName);
            Console.WriteLine(msg);
            MainFormClass.MainForm.Log(msg);
          }
        }
      }
    }

    /// <summary>
    /// Writes data to an output file, insuring the file isn't locked, for instance.
    /// If needed, finds a usable name by adding a number in parenthesis. abc.txt -> abc(3).txt
    /// </summary>
    /// <param name="DestFolderName">a directory name</param>
    /// <param name="shortName">desired name of file</param>
    /// <param name="data">content for the file</param>
    /// <param name="overwrite">if true, don't ask about overwriting an existing file</param>
    /// <returns>A tuple of strings: error message and outputfilename</returns>
    private static (string, string) SafelyWriteOutputFile(string DestFolderName, string shortName, byte[] data, bool overwrite) {
      bool ok = Util.DirectoryExistsOrWasCreated(DestFolderName);
      if (!ok) return ("ERROR - can not create output directory: " + DestFolderName, string.Empty);
      ok = Util.DirectoryIsWriteable(DestFolderName);
      if (!ok) return ("ERROR - can not write to output directory: " + DestFolderName, string.Empty);
      //
      string outfilename = Path.Combine(DestFolderName, shortName);
      string result = string.Empty;
      if (File.Exists(outfilename)) {
        if (overwrite) {
          result = SafeWriteHelper(data, outfilename);
        } else {
          // overwrite not allowed
          outfilename = FindAvailableName(outfilename);
          if (outfilename == string.Empty) return ("ERROR - can not write a file for: \n " + shortName, string.Empty);
          result = SafeWriteHelper(data, outfilename);
        }
      } else {
        // desired output file does not already exist
        result = SafeWriteHelper(data, outfilename);
      }
      if (result.StartsWith("ERROR")) {
        return (result, string.Empty);
      } else {
        return (string.Empty, result);
      }
    }

    private static string SafeWriteHelper(byte[] data, string outfilename) {
      try {
        File.Delete(outfilename);
        string TempName = MakeTempName(outfilename);
        File.Delete(TempName);
        File.WriteAllBytes(TempName, data);
        File.Move(TempName, outfilename);
      } catch {
        return "ERROR";
      }
      return outfilename;
    }

    private static string FindAvailableName(string outfilename) {
      // adds a number in parenthesis to file name   abc.txt -> abc(3).txt
      string dir = Path.GetDirectoryName(outfilename);
      string shortname = Path.GetFileNameWithoutExtension(outfilename);
      string ext = Path.GetExtension(outfilename);
      string Name = string.Empty;
      int i = 0;
      do {
        i++;
        if(i==100) {
          // giving up
          return string.Empty;
        }
        Name = Path.Combine(dir, shortname + "(" + i + ")"+ ext);
      } while (File.Exists(Name));
      return Name;
    }

    private static string MakeTempName(string outfilename) {
      // adds "_part" to file name   abc.txt -> abc_part.txt
      string dir = Path.GetDirectoryName(outfilename);
      string shortname = Path.GetFileNameWithoutExtension(outfilename);
      string ext = Path.GetExtension(outfilename);
      string partial = "_part";
      string TempName = Path.Combine(dir, shortname + partial + ext);
      return TempName;
    }
  }

  public struct atarget {
    public string name { get; set; }
    public int credit_cost { get; set; }
  }
  public struct tgtdata {
    public string name { get; set; }
    public List<atarget> targets { get; set; }
  }

  public struct ErrStruct {
    public List<OneError> errors { get; set; }
  }
  public struct OneError {
    public int code { get; set; }
    public string message { get; set; }
  }

  public struct jobstatus {
    public int id { get; set; }
    public string key { get; set; }
    public string status { get; set; }
    public bool sandbox { get; set; }
    public string created_at { get; set; }
    public string finished_at { get; set; }
    public aboutfile source_file { get; set; }
    public List<aboutfile> target_files { get; set; }
    public string target_format { get; set; }
    public int credit_cost { get; set; }
  }

  public struct aboutfile {
    public int id { get; set; }
    public string name { get; set; }
    public int size { get; set; }
  }





}
