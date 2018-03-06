using System;
using System.Text;
using Rhino;
using Rhino.Collections;
using Rhino.Commands;
using Rhino.FileIO;
using Rhino.PlugIns;

namespace CommandTracker
{
  public class CommandTrackerPlugIn : PlugIn
  {
    private const int MAJOR = 1;
    private const int MINOR = 0;
    private ArchivableDictionary m_dictionary;

    // Public Constructor
    public CommandTrackerPlugIn()
    {
      m_dictionary = new ArchivableDictionary();
      Instance = this;
      CommandTrackingEnabled = false;
    }

    /// <summary>
    /// Gets the only instance of the CommandTrackerPlugIn plug-in.
    /// </summary>
    public static CommandTrackerPlugIn Instance
    {
      get;
      private set;
    }

    /// <summary>
    /// Called when the plug-in is being loaded.
    /// </summary>
    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
      // Add event watchers
      Command.EndCommand += OnEndCommand;
      RhinoDoc.CloseDocument += OnCloseDocument;
      return LoadReturnCode.Success;
    }

    /// <summary>
    /// Called whenever a Rhino is about to save a .3dm file.  If you want to save
    //  plug-in document data when a model is saved in a version 5 .3dm file, then
    //  you must override this function to return true and you must override WriteDocument().
    /// </summary>
    protected override bool ShouldCallWriteDocument(FileWriteOptions options)
    {
      return !options.WriteGeometryOnly && 
             !options.WriteSelectedObjectsOnly && 
             0 != m_dictionary.Count;
    }

    /// <summary>
    /// Called when Rhino is saving a .3dm file to allow the plug-in to save document user data.
    /// </summary>
    protected override void WriteDocument(RhinoDoc doc, BinaryArchiveWriter archive, FileWriteOptions options)
    {
      archive.Write3dmChunkVersion(MAJOR, MINOR);
      archive.WriteDictionary(m_dictionary);
    }

    /// <inheritdoc />
    /// <summary>
    /// Called whenever a Rhino document is being loaded and plug-in user data was
    /// encountered written by a plug-in with this plug-in's GUID.
    /// </summary>
    protected override void ReadDocument(RhinoDoc doc, BinaryArchiveReader archive, FileReadOptions options)
    {
      archive.Read3dmChunkVersion(out var major, out var minor);
      if (MAJOR == major && MINOR == minor)
      {
        var dictionary = archive.ReadDictionary();
        if (null != dictionary && !options.ImportMode && !options.ImportReferenceMode)
        {
          m_dictionary = dictionary.Clone();

          // Note, if we read our user data from a document, then
          // assume that we can to track commands...
          CommandTrackingEnabled = true;
        }
      }
    }

    /// <summary>
    /// Command.EndCommand event handler
    /// </summary>
    private void OnEndCommand(object sender, CommandEventArgs e)
    {
      if (CommandTrackingEnabled && e.CommandResult == Result.Success)
      {
        string key = e.CommandEnglishName;
        m_dictionary.TryGetInteger(key, out int value);
        m_dictionary.Set(key, ++value);
      }
    }

    /// <summary>
    /// RhinoDoc.CloseDocument event handler
    /// </summary>
    private void OnCloseDocument(object sender, DocumentEventArgs e)
    {
      // Upon closing the document, clear the dictionary
      m_dictionary.Clear();
    }

    /// <summary>
    /// Enabled or disables command tracker
    /// </summary>
    public bool CommandTrackingEnabled { get; set; }

    /// <summary>
    /// Clears the command tracking dictionary
    /// </summary>
    public int ClearCommandDictionary()
    {
      var count = m_dictionary.Count;
      m_dictionary.Clear();
      return count;
    }

    /// <summary>
    /// Reports the command history
    /// </summary>
    public bool CommandTrackingReport(ref string message)
    {
      if (0 == m_dictionary.Count)
        return false;

      var keys = m_dictionary.Keys;
      Array.Sort(keys);

      var sb = new StringBuilder();
      foreach (var k in keys)
        sb.AppendFormat("{0} = {1}\n", k, m_dictionary.GetInteger(k));

      message = sb.ToString();

      return true;
    }
  }
}