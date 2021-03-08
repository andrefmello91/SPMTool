using Autodesk.AutoCAD.Runtime;
using SPM.Analysis;
using SPM.Elements;
using SPMTool.Application.UserInterface;
using SPMTool.Core;
using SPMTool.Enums;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;
using static SPMTool.Core.Results;

[assembly: CommandClass(typeof(SPMTool.Editor.Commands.Analysis))]

namespace SPMTool.Editor.Commands
{
	public static class Analysis
	{
		[CommandMethod(CommandName.Linear)]
		public static void LinearAnalysis()
		{
            // Get input data
            var input = Model.GenerateInput(AnalysisType.Linear, out var dataOk, out var message);

            if (!dataOk)
            {
	            ShowAlertDialog(message);
				return;
            }

            // Do a linear analysis
            var analysis = new SPM.Analysis.Analysis(input);
			analysis.Do();

            // Draw results of analysis
            DrawResults(analysis);
        }

        [CommandMethod(CommandName.Nonlinear)]
		public static void NonLinearAnalysis()
		{
			// Get input data
			var input = Model.GenerateInput(AnalysisType.NonLinear, out var dataOk, out var message);

			if (!dataOk)
			{
				ShowAlertDialog(message);
				return;
			}

			// Get the index of node to monitor displacement
			var uIndexn = UserInput.MonitoredIndex();

			if(!uIndexn.HasValue)
				return;

			// Get analysis settings
			var settings = DataBase.Settings.Analysis;

            // Do analysis
            var analysis = new SecantAnalysis(input);
			analysis.Do(uIndexn.Value, 1, settings.NumLoadSteps, settings.Tolerance, settings.MaxIterations);

            // Show load-displacement diagram
            var units = DataBase.Settings.Units;

            ShowModelessWindow(MainWindow.Handle, new GraphWindow(analysis.MonitoredDisplacements, analysis.MonitoredLoadFactor, units.Displacements));

            // Draw results of analysis
            DrawResults(analysis);

			if (analysis.Stop)
				ShowAlertDialog(analysis.StopMessage);
		}
	}
}
