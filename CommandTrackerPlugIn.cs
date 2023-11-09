using Rhino;
using Rhino.FileIO;
using Rhino.PlugIns;

namespace CommandTracker
{
  /// <summary>
  /// CommandTrackerPlugIn
  /// </summary>
  public class CommandTrackerPlugIn : PlugIn
  {
    /// <summary>
    /// Constructor
    /// </summary>
    public CommandTrackerPlugIn()
    {
      Instance = this;
    }

    /// <summary>
    /// Returns the one and only instance of this plug-in
    /// </summary>
    public static CommandTrackerPlugIn Instance { get; private set; }

    /// <summary>
    /// This plug-in loads when Rhino loads
    /// </summary>
    public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;

    /// <summary>
    /// Enables or disabled command tracking
    /// </summary>
    public bool CommandTrackingEnabled
    {
      get => m_commandTrackingEnabled;
      set
      {
        m_commandTrackingEnabled = value;
        Settings.SetBool(nameof(CommandTrackingEnabled), m_commandTrackingEnabled);
      }
    }
    private bool m_commandTrackingEnabled = true;

    /// <summary>
    /// PlugIn.OnLoad override
    /// </summary>
    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
      var settings = Settings;
      if (null != settings)
      {
        if (settings.TryGetBool(nameof(CommandTrackingEnabled), out var enabled))
          m_commandTrackingEnabled = enabled;
      }
      RhinoApp.WriteLine("Command tracking is {0}.", CommandTrackingEnabled ? "enabled" : "disabled");
      return LoadReturnCode.Success;
    }

    /// <summary>
    /// PlugIn.ShouldCallWriteDocument override
    /// </summary>
    protected override bool ShouldCallWriteDocument(FileWriteOptions options)
    {
      return !options.WriteGeometryOnly && !options.WriteSelectedObjectsOnly;
    }

    /// <summary>
    /// PlugIn.WriteDocument override
    /// </summary>
    protected override void WriteDocument(RhinoDoc doc, BinaryArchiveWriter archive, FileWriteOptions options)
    {
      CommandTrackerViewModel.WriteDocument(doc, archive, options);
    }

    /// <summary>
    /// PlugIn.ReadDocument override
    /// </summary>
    protected override void ReadDocument(RhinoDoc doc, BinaryArchiveReader archive, FileReadOptions options)
    {
      CommandTrackerViewModel.ReadDocument(doc, archive, options);
    }
  }
}