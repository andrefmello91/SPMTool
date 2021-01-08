﻿using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using SPM.Analysis;
using SPM.Elements;
using SPMTool.UserInterface;
using SPMTool.Database;
using Analysis = SPMTool.Editor.Commands.Analysis;


[assembly: CommandClass(typeof(Analysis))]

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
            var analysis = new LinearAnalysis(input);
			analysis.Do();

            // Draw results of analysis
            Model.DrawResults(analysis, SettingsData.SavedUnits);
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
			var settings = SettingsData.SavedAnalysisSettings;

            // Do analysis
            var analysis = new SecantAnalysis(input);
			analysis.Do(uIndexn.Value, 1, settings.NumLoadSteps, settings.Tolerance, settings.MaxIterations);

            // Show load-displacement diagram
            var units = SettingsData.SavedUnits;

            Application.ShowModelessWindow(Application.MainWindow.Handle, new GraphWindow(analysis.MonitoredDisplacements, analysis.MonitoredLoadFactor, units.Displacements));

            // Draw results of analysis
            Model.DrawResults(analysis, units);

			if (analysis.Stop)
				Application.ShowAlertDialog(analysis.StopMessage);
		}
	}
}
