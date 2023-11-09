using Rhino;
using Rhino.Collections;
using Rhino.Commands;
using Rhino.FileIO;
using System;
using System.Text;

namespace CommandTracker
{
  /// <summary>
  /// CommandTrackerViewModel
  /// </summary>
  public class CommandTrackerViewModel
  {
    private const int MAJOR = 1;
    private const int MINOR = 1;
    private ArchivableDictionary m_command_dictionary;
    private ArchivableDictionary m_save_history;

    private static EventHandler<DocumentEventArgs> g_new_document;
    private static EventHandler<DocumentOpenEventArgs> g_end_open_document;
    private static EventHandler<DocumentSaveEventArgs> g_begin_save_document;
    private static EventHandler<CommandEventArgs> g_end_command;

    /// <summary>
    /// Constructor
    /// </summary>
    public CommandTrackerViewModel(uint documentSerialNumber)
    {
      DocumentSerialNumber = documentSerialNumber;

      HookRhinoEvents();
    }

    /// <summary>
    /// This view model's document serial number
    /// </summary>
    public uint DocumentSerialNumber { get; private set; }

    /// <summary>
    /// This view model's document serial number
    /// </summary>
    public RhinoDoc Document => RhinoDoc.FromRuntimeSerialNumber(DocumentSerialNumber);

    /// <summary>
    /// Get the command dictionary
    /// </summary>
    private ArchivableDictionary CommandDictionary
    {
      get
      {
        if (null == m_command_dictionary)
          m_command_dictionary = new ArchivableDictionary();
        return m_command_dictionary;
      }
      set
      {
        if (null == m_command_dictionary)
          m_command_dictionary = new ArchivableDictionary();
        if (null != value)
          m_command_dictionary = value.Clone();
        else
          m_command_dictionary.Clear();
      }
    }

    /// <summary>
    /// Get the save dictionary
    /// </summary>
    private ArchivableDictionary SaveDictionary
    {
      get
      {
        if (null == m_save_history)
          m_save_history = new ArchivableDictionary();
        return m_save_history;
      }
      set
      {
        if (null == m_save_history)
          m_save_history = new ArchivableDictionary();
        if (null != value)
          m_save_history = value.Clone();
        else
          m_save_history.Clear();
      }
    }

    /// <summary>
    /// Returns the number of commands logged in the command dictionary
    /// </summary>
    public int CommandCount => CommandDictionary.Count;

    /// <summary>
    /// Returns the number of ssaves logged in the save dictionary
    /// </summary>
    public int SaveCount => SaveDictionary.Count;

    /// <summary>
    /// Clears both command and save history
    /// </summary>
    public void ClearAllHistory()
    {
      CommandDictionary.Clear();
      SaveDictionary.Clear();
    }

    public void ClearSaveHistory()
    {
      SaveDictionary.Clear();
    }

    /// <summary>
    /// Generates a text report
    /// </summary>
    public bool Report(out string message)
    {
      message = null;

      var doc = Document;
      var commands = CommandDictionary;
      if (null == doc || 0 == commands.Count)
        return false;

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

      var keys = commands.Keys;
      Array.Sort(keys);

      sb.Append("\nCommand history:\n");
      foreach (var k in keys)
      {
        if (commands.TryGetInteger(k, out var value))
          sb.AppendFormat($"\t{k} ({value})\n");
      }

      var saves = SaveDictionary;
      if (saves.Count > 0)
      {
        keys = saves.Keys;
        Array.Sort(keys);
        sb.Append("\nSave history:\n");
        foreach (var k in keys)
        {
          if (saves.TryGetString(k, out var value))
            sb.AppendFormat($"\t{k} ({value})\n");
        }
      }

      message = sb.ToString();
      return !string.IsNullOrEmpty(message);
    }

    /// <summary>
    /// Hook up Rhino event watchers
    /// </summary>
    private static void HookRhinoEvents()
    {
      if (null == g_new_document)
        RhinoDoc.NewDocument += g_new_document = OnNewDocument;

      if (null == g_end_open_document)
        RhinoDoc.EndOpenDocument += g_end_open_document = OnEndOpenDocument;

      if (null == g_begin_save_document)
        RhinoDoc.BeginSaveDocument += g_begin_save_document = OnBeginSaveDocument;

      if (null == g_end_command)
        Rhino.Commands.Command.EndCommand += g_end_command = OnEndCommand;
    }

    /// <summary>
    /// RhinoDoc.NewDocument event handler
    /// </summary>
    public static void OnNewDocument(object sender, DocumentEventArgs args)
    {
      // Create a new view model
      CommandTrackerViewModel.GetFromDocument(args.Document);
    }

    /// <summary>
    /// RhinoDoc.EndOpenDocument event handler
    /// </summary>
    public static void OnEndOpenDocument(object sender, DocumentOpenEventArgs args)
    {
      // Create a new view model
      CommandTrackerViewModel.GetFromDocument(args.Document);
    }

    internal const string dateFormat = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// RhinoDoc.EndSaveDocument event handler
    /// </summary>
    public static void OnBeginSaveDocument(object sender, DocumentSaveEventArgs args)
    {
      if (CommandTrackerPlugIn.Instance.CommandTrackingEnabled)
      {
        var vm = GetFromDocument(args.Document);
        if (null != vm)
        {
          var date = DateTime.Now;
          var key = date.ToString(dateFormat);
          var value = $"{Environment.UserName}";
          var saves = vm.SaveDictionary;
          saves.Set(key, value);
        }
      }
    }

    /// <summary>
    /// RhinoDoc.EndOpenDocument handler
    /// </summary>
    private static void OnEndCommand(object sender, CommandEventArgs args)
    {
      if (CommandTrackerPlugIn.Instance.CommandTrackingEnabled && args.CommandResult == Result.Success)
      { 
        var vm = GetFromDocument(args.Document);
        if (null != vm)
        {
          var commands = vm.CommandDictionary;
          var key = args.CommandEnglishName;
          var value = commands.Getint(key, 0);
          value++;
          commands.Set(key, value);
        }
      }
    }

    /// <summary>
    /// Gets a view model from the document
    /// </summary>
    public static CommandTrackerViewModel GetFromDocument(RhinoDoc doc)
    {
      return doc?.RuntimeData.GetValue(typeof(CommandTrackerViewModel), rhinoDoc => new CommandTrackerViewModel(rhinoDoc.RuntimeSerialNumber));
    }

    /// <summary>
    /// Write document data
    /// </summary>
    public static void WriteDocument(RhinoDoc doc, BinaryArchiveWriter archive, FileWriteOptions options)
    {
      var vm = GetFromDocument(doc);
      if (null != vm)
      {
        archive.Write3dmChunkVersion(MAJOR, MINOR);
        archive.WriteDictionary(vm.CommandDictionary);
        archive.WriteDictionary(vm.SaveDictionary);
      }
    }

    /// <summary>
    /// Read document data
    /// </summary>
    public static void ReadDocument(RhinoDoc doc, BinaryArchiveReader archive, FileReadOptions options)
    {
      var vm = GetFromDocument(doc);
      if (null != vm)
      { 
        archive.Read3dmChunkVersion(out var major, out var minor);
        if ( major == MAJOR)
        {
          if (minor >= 0)
          {
            var commands = archive.ReadDictionary();
            if (null != commands && options.OpenMode)
              vm.CommandDictionary = commands.Clone();
          }
          if (minor >= 1)
          {
            var saves = archive.ReadDictionary();
            if (null != saves && options.OpenMode)
              vm.SaveDictionary = saves.Clone();
          }
        }
      }
    }
  }
}
