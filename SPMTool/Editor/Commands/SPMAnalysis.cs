﻿using System.Linq;
using andrefmello91.FEMAnalysis;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Application.UserInterface;
using SPMTool.Core;
using andrefmello91.FEMAnalysis;
using andrefmello91.SPMElements;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;
using static SPMTool.Core.Results;

#nullable enable

[assembly: CommandClass(typeof(SPMTool.Editor.Commands.SPMAnalysis))]

namespace SPMTool.Editor.Commands
{
	public static class SPMAnalysis
	{
		[CommandMethod(CommandName.Linear)]
		public static void LinearAnalysis()
		{
            // Get input data
            var input = Model.GenerateInput(AnalysisType.Linear, out var dataOk, out var message, out var nodes, out var stringers, out var panels);

            if (!dataOk)
            {
	            ShowAlertDialog(message);
				return;
            }
			
			// Do a linear analysis
            var analysis = new Analysis(input);
			analysis.Execute();
			
			Model.Editor.WriteMessage(analysis.ToString());

            // Draw results of analysis
            DrawResults(nodes, stringers.Cast<Stringer>().ToArray(), panels.Cast<Panel>().ToArray(), false);
        }

        [CommandMethod(CommandName.Nonlinear)]
		public static void NonLinearAnalysis()
		{
			// Get input data
			var input = Model.GenerateInput(AnalysisType.Nonlinear, out var dataOk, out var message, out var nodes, out var stringers, out var panels);

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
            var analysis = new SecantAnalysis(input, settings.NumLoadSteps, settings.Tolerance, settings.MaxIterations);
			analysis.Execute(uIndexn.Value);
			
			// Show window
			var plot = new PlotWindow(analysis.GenerateOutput()!);
			ShowModelessWindow(MainWindow.Handle, plot);
			
			// Show a message if analysis stopped
			if (analysis.Stop)
				ShowAlertDialog(analysis.StopMessage);

			// Updated plot
			plot.UpdatePlot();
			
            // Draw results of analysis
            DrawResults(nodes, stringers.Cast<Stringer>().ToArray(), panels.Cast<Panel>().ToArray(), true);
		}
	}
}