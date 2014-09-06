using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace CommandTracker
{
  [System.Runtime.InteropServices.Guid("9d5ad923-3041-41c0-835a-4c8af2c3bb05")]
  public class CommandTrackerCommand : Command
  {
    public CommandTrackerCommand()
    {
    }

    public override string EnglishName
    {
      get { return "CommandTracker"; }
    }

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      GetOption go = new GetOption();
      go.SetCommandPrompt("Command tracking options");
      go.AcceptNothing(true);

      for (;;)
      {
        go.ClearCommandOptions();

        int clear_index = go.AddOption("Clear");

        bool bEnabled = CommandTrackerPlugIn.Instance.CommandTrackingEnabled;
        OptionToggle opt_enable = new OptionToggle(bEnabled, "Off", "On");
        int enable_index = go.AddOptionToggle("Enable", ref opt_enable);

        int report_index = go.AddOption("Report");

        GetResult res = go.Get();

        if (res == GetResult.Nothing)
          return Result.Nothing;
        else if (res != GetResult.Option)
          break;

        CommandLineOption option = go.Option();
        if (null == option)
          return Rhino.Commands.Result.Failure;

        int index = option.Index;

        if (index == clear_index)
        {
          int count = CommandTrackerPlugIn.Instance.ClearCommandDictionary();
          if (0 == count)
            RhinoApp.WriteLine("No command tracking records to clear.");
          else if (1 == count)
            RhinoApp.WriteLine("1 command tracking record cleared.");
          else
            RhinoApp.WriteLine("{0} command tracking records cleared.", count);
        }
        else if (index == enable_index)
        {
          bEnabled = opt_enable.CurrentValue;
          CommandTrackerPlugIn.Instance.CommandTrackingEnabled = bEnabled;
        }
        else if (index == report_index)
        {
          string message = string.Empty;
          bool rc = CommandTrackerPlugIn.Instance.CommandTrackingReport(ref message);
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
