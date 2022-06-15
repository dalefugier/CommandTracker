using Rhino;
using Rhino.FileIO;
using Rhino.PlugIns;

namespace CommandTracker
{
  public class CommandTrackerPlugIn : PlugIn
  {
    public CommandTrackerPlugIn()
    {
      Instance = this;
    }

    public static CommandTrackerPlugIn Instance { get; private set; }

    public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;

    public bool CommandTrackingEnabled
    {
      get => m_command_tracking_enabled;
      set
      {
        m_command_tracking_enabled = value;
        Settings.SetBool(nameof(CommandTrackingEnabled), m_command_tracking_enabled);
      }
    }
    private bool m_command_tracking_enabled = true;

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
      if (Settings.TryGetBool(nameof(CommandTrackingEnabled), out var enabled))
        m_command_tracking_enabled = enabled;
      RhinoApp.WriteLine("Command tracking is {0}.", CommandTrackingEnabled ? "enabled" : "disabled");
      return LoadReturnCode.Success;
    }

    protected override bool ShouldCallWriteDocument(FileWriteOptions options)
    {
      return !options.WriteGeometryOnly && !options.WriteSelectedObjectsOnly;
    }

    protected override void WriteDocument(RhinoDoc doc, BinaryArchiveWriter archive, FileWriteOptions options)
    {
      CommandTrackerViewModel.WriteDocument(doc, archive, options);
    }

    protected override void ReadDocument(RhinoDoc doc, BinaryArchiveReader archive, FileReadOptions options)
    {
      CommandTrackerViewModel.ReadDocument(doc, archive, options);
    }
  }
}