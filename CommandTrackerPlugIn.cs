using System;
using System.Text;
using Rhino;
using Rhino.Collections;
using Rhino.Commands;
using Rhino.FileIO;
using Rhino.PlugIns;

namespace CommandTracker
{
  public class CommandTrackerPlugIn : Rhino.PlugIns.PlugIn
  {
    private readonly int m_major = 1;
    private readonly int m_minor = 0;
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
      Command.EndCommand += new System.EventHandler<Rhino.Commands.CommandEventArgs>(OnEndCommand);
      RhinoDoc.CloseDocument += new System.EventHandler<DocumentEventArgs>(OnCloseDocument);
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
      archive.Write3dmChunkVersion(m_major, m_minor);
      archive.WriteDictionary(m_dictionary);
    }

    /// <summary>
    /// Called whenever a Rhino document is being loaded and plug-in user data was
    /// encountered written by a plug-in with this plug-in's GUID.
    /// </summary>
    protected override void ReadDocument(RhinoDoc doc, BinaryArchiveReader archive, FileReadOptions options)
    {
      int major = 0;
      int minor = 0;
      archive.Read3dmChunkVersion(out major, out minor);
      if (m_major == major && m_minor == minor)
      {
        ArchivableDictionary dictionary = archive.ReadDictionary();
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
    void OnEndCommand(object sender, Rhino.Commands.CommandEventArgs e)
    {
      if (CommandTrackingEnabled && e.CommandResult == Result.Success)
      {
        string key = e.CommandEnglishName;
        int value = 1;
        m_dictionary.TryGetInteger(key, out value);
        m_dictionary.Set(key, ++value);
      }
    }

    /// <summary>
    /// RhinoDoc.CloseDocument event handler
    /// </summary>
    void OnCloseDocument(object sender, DocumentEventArgs e)
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
      int count = m_dictionary.Count;
      m_dictionary.Clear();
      return count;
    }

    public bool CommandTrackingReport(ref string message)
    {
      if (0 == m_dictionary.Count)
        return false;

      string[] keys = m_dictionary.Keys;
      Array.Sort(keys);

      StringBuilder sb = new StringBuilder();
      foreach (string k in keys)
        sb.AppendFormat("{0} = {1}\n", k, m_dictionary.GetInteger(k));

      message = sb.ToString();

      return true;
    }
  }
}