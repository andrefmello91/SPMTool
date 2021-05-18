﻿using andrefmello91.FEMAnalysis;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Application.UserInterface;
using SPMTool.Core;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;
using static SPMTool.Core.Results;

#nullable enable

namespace SPMTool.Editor.Commands
{
	public static partial class AcadCommands
	{

		#region Methods

		[CommandMethod(CommandName.Linear)]
		public static void LinearAnalysis()
		{
			// Get input data
			var input = SPMModel.ActiveModel.GenerateInput(AnalysisType.Linear, out var dataOk, out var message);

			if (!dataOk)
			{
				ShowAlertDialog(message);
				return;
			}

			// Do a linear analysis
			var analysis = new LinearAnalysis(input);
			analysis.Execute();

			// Model.Editor.WriteMessage(analysis.ToString());

			// Draw results of analysis
			DrawResults();
		}

		[CommandMethod(CommandName.Nonlinear)]
		public static void NonLinearAnalysis() => ExecuteNonlinearAnalysis();


		[CommandMethod(CommandName.Simulation)]
		public static void Simulation() => ExecuteNonlinearAnalysis(true);

		/// <summary>
		///     Execute the nonlinear analysis.
		/// </summary>
		/// <param name="simulate">Execute a simulation until failure?</param>
		private static void ExecuteNonlinearAnalysis(bool simulate = false)
		{
			// Get input data
			var input = SPMModel.ActiveModel.GenerateInput(AnalysisType.Nonlinear, out var dataOk, out var message);

			if (!dataOk)
			{
				ShowAlertDialog(message);
				return;
			}

			// Get the index of node to monitor displacement
			var uIndexn = UserInput.MonitoredIndex();

			if (!uIndexn.HasValue)
				return;

			// Get analysis settings
			var settings = SPMDatabase.ActiveDatabase.Settings.Analysis;

			// Do analysis
			var analysis = new SPMNonlinearAnalysis(input, settings.Solver, settings.NumLoadSteps, settings.Tolerance, settings.MaxIterations);
			analysis.Execute(uIndexn.Value, simulate);
			var output = analysis.GenerateOutput();

			// Show window
			var plot = new PlotWindow(output);
			ShowModelessWindow(MainWindow.Handle, plot);

			// Show a message if analysis stopped
			if (analysis.Stop)
				ShowAlertDialog(analysis.StopMessage);

			// Updated plot
			plot.UpdatePlot();

			// Draw results of analysis
			DrawResults();
		}

		#endregion

	}
}