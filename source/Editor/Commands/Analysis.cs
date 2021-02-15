using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using SPM.Analysis;
using SPM.Elements;
using SPMTool.UserInterface;
using SPMTool.Core;
using Analysis = SPM.Analysis.Analysis;


[assembly: CommandClass(typeof(SPMTool.Editor.Commands.Analysis))]

namespace SPMTool.Editor.Commands
{
	public static class Analysis
	{
		[CommandMethod("DoLinearAnalysis")]
		public static void DoLinearAnalysis()
		{
            // Get input data
            var input = Model.GenerateInput(AnalysisType.Linear, out var dataOk, out var message);

            if (!dataOk)
            {
	            Application.ShowAlertDialog(message);
				return;
            }

            // Do a linear analysis
            var analysis = new SPM.Analysis.Analysis(input);
			analysis.Do();

            // Draw results of analysis
            Model.DrawResults(analysis);
        }

        [CommandMethod("DoNonLinearAnalysis")]
		public static void DoNonLinearAnalysis()
		{
			// Get input data
			var input = Model.GenerateInput(AnalysisType.NonLinear, out var dataOk, out var message);

			if (!dataOk)
			{
				Application.ShowAlertDialog(message);
				return;
			}

			// Get the index of node to monitor displacement
			var uIndexn = UserInput.MonitoredIndex();

			if(!uIndexn.HasValue)
				return;

			// Get analysis settings
			var settings = ApplicationSettings.Settings.Analysis;

            // Do analysis
            var analysis = new SecantAnalysis(input);
			analysis.Do(uIndexn.Value, 1, settings.NumLoadSteps, settings.Tolerance, settings.MaxIterations);

            // Show load-displacement diagram
            var units = ApplicationSettings.Settings.Units;

            Application.ShowModelessWindow(Application.MainWindow.Handle, new GraphWindow(analysis.MonitoredDisplacements, analysis.MonitoredLoadFactor, units.Displacements));

            // Draw results of analysis
            Model.DrawResults(analysis);

			if (analysis.Stop)
				Application.ShowAlertDialog(analysis.StopMessage);
		}
	}
}
