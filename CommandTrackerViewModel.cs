using System;
using System.Text;
using Rhino;
using Rhino.Collections;
using Rhino.Commands;
using Rhino.FileIO;

namespace CommandTracker
{
  public class CommandTrackerViewModel
  {
    private const int MAJOR = 1;
    private const int MINOR = 0;
    private ArchivableDictionary m_dictionary;

    private static EventHandler<DocumentEventArgs> g_new_document;
    private static EventHandler<DocumentOpenEventArgs> g_end_open_document;
    private static EventHandler<CommandEventArgs> g_end_command;

    public CommandTrackerViewModel(uint documentSerialNumber)
    {
      DocumentSerialNumber = documentSerialNumber;
      HookRhinoEvents();
    }

    public uint DocumentSerialNumber { get; private set; }

    public RhinoDoc Document => RhinoDoc.FromRuntimeSerialNumber(DocumentSerialNumber);

    private ArchivableDictionary Dictionary
    {
      get
      {
        if (null == m_dictionary)
          m_dictionary = new ArchivableDictionary();
        return m_dictionary;
      }
      set
      {
        if (null == m_dictionary)
          m_dictionary = new ArchivableDictionary();
        if (null != value)
          m_dictionary = value.Clone();
        else
          m_dictionary.Clear();
      }
    }

    public int Count
    {
      get
      {
        var dict = Dictionary;
        return dict.Count;
      }
    }

    public int Clear()
    {
      var dict = Dictionary;
      var count = dict.Count;
      dict.Clear();
      return count;
    }

    public bool Report(out string message)
    {
      message = null;

      var doc = Document;
      var dict = Dictionary;
      if (null == doc || 0 == dict.Count)
        return false;

      var keys = dict.Keys;
      Array.Sort(keys);

      var sb = new StringBuilder();
      sb.Append("Summary information:\n");

      var path = doc.Path;
      if (string.IsNullOrEmpty(path))
      {
        sb.AppendFormat("\t{0}\n", "<unnamed>");
      }
      else
      {
        sb.AppendFormat("\t{0}\n", path);
        if (File3dm.ReadRevisionHistory(path, out var creator, out var editor, out var revision, out var createDate, out var editDate))
        {
          sb.AppendFormat("\tCreated by:  {0}\n", creator);
          sb.AppendFormat("\tCreated on:  {0}\n", createDate.ToLongDateString());
          sb.AppendFormat("\tLast saved by:  {0}\n", editor);
          sb.AppendFormat("\tLast saved on:  {0}\n", editDate.ToLongDateString());
          sb.AppendFormat("\tRevision number:  {0}\n", revision);
        }
      }

      sb.Append("\nCommands run successfully:\n");
      foreach (var k in keys)
        sb.AppendFormat("\t{0} ({1})\n", k, dict.GetInteger(k));

      message = sb.ToString();
      return !string.IsNullOrEmpty(message);
    }

    private void HookRhinoEvents()
    {
      if (null == g_new_document)
        RhinoDoc.NewDocument += g_new_document = OnNewDocument;
      if (null == g_end_open_document)
        RhinoDoc.EndOpenDocument += g_end_open_document = OnEndOpenDocument;
      if (null == g_end_command)
        Rhino.Commands.Command.EndCommand += g_end_command = OnEndCommand;
    }

    public static void OnNewDocument(object sender, DocumentEventArgs args)
    {
      // Create a new view model
      CommandTrackerViewModel.GetFromDocument(args.Document);
    }

    public static void OnEndOpenDocument(object sender, DocumentOpenEventArgs args)
    {
      // Create a new view model
      CommandTrackerViewModel.GetFromDocument(args.Document);
    }

    private static void OnEndCommand(object sender, CommandEventArgs args)
    {
      if (CommandTrackerPlugIn.Instance.CommandTrackingEnabled && args.CommandResult == Result.Success)
      { 
        var vm = GetFromDocument(args.Document);
        if (null != vm)
        {
          var dict = vm.Dictionary;
          var key = args.CommandEnglishName;
          dict.TryGetInteger(key, out int value);
          dict.Set(key, ++value);
        }
      }
    }

    public static CommandTrackerViewModel GetFromDocument(RhinoDoc doc)
    {
      return doc?.RuntimeData.GetValue(typeof(CommandTrackerViewModel), rhinoDoc => new CommandTrackerViewModel(rhinoDoc.RuntimeSerialNumber));
    }

    public static void WriteDocument(RhinoDoc doc, BinaryArchiveWriter archive, FileWriteOptions options)
    {
      var vm = GetFromDocument(doc);
      if (null != vm)
      {
        archive.Write3dmChunkVersion(MAJOR, MINOR);
        archive.WriteDictionary(vm.Dictionary);
      }
    }

    public static void ReadDocument(RhinoDoc doc, BinaryArchiveReader archive, FileReadOptions options)
    {
      var vm = GetFromDocument(doc);
      if (null != vm)
      { 
        archive.Read3dmChunkVersion(out var major, out var minor);
        if (MAJOR == major && MINOR == minor)
        {
          var dictionary = archive.ReadDictionary();
          if (null != dictionary && options.OpenMode)
          {
            vm.Dictionary = dictionary.Clone();
          }
        }
      }
    }
  }
}
