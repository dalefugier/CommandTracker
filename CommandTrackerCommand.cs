using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace CommandTracker
{
  [System.Runtime.InteropServices.Guid("9d5ad923-3041-41c0-835a-4c8af2c3bb05")]
  public class CommandTrackerCommand : Command
  {
    public override string EnglishName => "CommandTracker";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      var go = new GetOption();
      go.SetCommandPrompt("Command tracking options");
      go.AcceptNothing(true);

      for (;;)
      {
        go.ClearCommandOptions();

        var clear_index = go.AddOption("Clear");

        var enabled = CommandTrackerPlugIn.Instance.CommandTrackingEnabled;
        var opt_enable = new OptionToggle(enabled, "Off", "On");
        var enable_index = go.AddOptionToggle("Enable", ref opt_enable);

        var report_index = go.AddOption("Report");

        var res = go.Get();

        if (res == GetResult.Nothing)
          return Result.Nothing;

        if (res != GetResult.Option)
          break;

        var option = go.Option();
        if (null == option)
          return Result.Failure;

        var index = option.Index;
        if (index == clear_index)
        {
          var count = CommandTrackerPlugIn.Instance.ClearCommandDictionary();
          switch (CommandTrackerPlugIn.Instance.ClearCommandDictionary())
          {
            case 0:
              RhinoApp.WriteLine("No command tracking records to clear.");
              break;
            case 1:
              RhinoApp.WriteLine("1 command tracking record cleared.");
              break;
            default:
              RhinoApp.WriteLine("{0} command tracking records cleared.", count);
              break;
          }
        }
        else if (index == enable_index)
        {
          CommandTrackerPlugIn.Instance.CommandTrackingEnabled = opt_enable.CurrentValue;
        }
        else if (index == report_index)
        {
          var message = string.Empty;
          var rc = CommandTrackerPlugIn.Instance.CommandTrackingReport(ref message);
          if (rc)
            Rhino.UI.Dialogs.ShowTextDialog(message, EnglishName);
          else
            RhinoApp.WriteLine("No command tracking records to report.");
        }
      }

      return Result.Nothing;
    }
  }
}
