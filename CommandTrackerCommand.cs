using Eto.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace CommandTracker
{
  public class CommandTrackerCommand : Rhino.Commands.Command
  {
    public override string EnglishName => "CommandTracker";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      var vm = CommandTrackerViewModel.GetFromDocument(doc);
      if (null == vm)
        return Result.Failure;

      var go = new GetOption();
      go.SetCommandPrompt("Command tracking options");
      go.AcceptNothing(true);

      for (;;)
      {
        go.ClearCommandOptions();

        var clear_index = go.AddOption("Clear");
        var report_index = go.AddOption("Report");
        var opt_enable = new OptionToggle(CommandTrackerPlugIn.Instance.CommandTrackingEnabled, "Off", "On");
        var enable_index = go.AddOptionToggle("Enable", ref opt_enable);

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
          var count = vm.Count;

          if (count > 0 && mode == RunMode.Interactive)
          {
            var msg = "Are you sure you want to clear all command tracking history?";
            var result = MessageBox.Show(msg, EnglishName, MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.No);
            if (result == DialogResult.No)
              continue;
          }

          vm.Clear();
          switch (count)
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
        else if (index == report_index)
        {
          var rc = vm.Report(out var message);
          if (rc)
            Rhino.UI.Dialogs.ShowTextDialog(message, EnglishName);
          else
            RhinoApp.WriteLine("No command tracking records to report.");
        }
        else if (index == enable_index)
        {
          CommandTrackerPlugIn.Instance.CommandTrackingEnabled = opt_enable.CurrentValue;
        }
      }

      return Result.Nothing;
    }
  }
}
