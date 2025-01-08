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
      CommandTrackerViewModel vm = CommandTrackerViewModel.GetFromDocument(doc);
      if (null == vm)
        return Result.Failure;

      GetOption go = new GetOption();
      go.SetCommandPrompt("Command tracking options");
      go.AcceptNothing(true);

      for (; ; )
      {
        go.ClearCommandOptions();

        int clear_index = go.AddOption("Clear");
        int report_index = go.AddOption("Report");
        OptionToggle opt_enable = new OptionToggle(CommandTrackerPlugIn.Instance.CommandTrackingEnabled, "Off", "On");
        int enable_index = go.AddOptionToggle("Enable", ref opt_enable);

        GetResult res = go.Get();

        if (res == GetResult.Nothing)
          return Result.Nothing;

        if (res != GetResult.Option)
          break;

        CommandLineOption option = go.Option();
        if (null == option)
          return Result.Failure;

        int index = option.Index;
        if (index == clear_index)
        {
          int count = vm.CommandCount;

          if (count > 0 && mode == RunMode.Interactive)
          {
            string msg = "Are you sure you want to clear all command tracking history?";
            DialogResult result = MessageBox.Show(msg, EnglishName, MessageBoxButtons.YesNo, MessageBoxType.Question, MessageBoxDefaultButton.No);
            if (result == DialogResult.No)
              continue;
          }

          vm.ClearAllHistory();
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
          bool rc = vm.Report(out string message);
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
