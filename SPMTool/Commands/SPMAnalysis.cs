﻿using andrefmello91.FEMAnalysis;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Application.UserInterface;
using SPMTool.Core;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

#nullable enable

namespace SPMTool.Commands
{
	public static partial class AcadCommands
	{

		#region Methods

		[CommandMethod(Command.Linear)]
		public static void LinearAnalysis()
		{
			var model = SPMModel.ActiveModel;

			// Get input data
			var input = model.GenerateInput(AnalysisType.Linear, out var dataOk, out var message);

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
			var results = new SPMResults(model);
			results.DrawResults();
		}

		[CommandMethod(Command.Nonlinear)]
		public static void NonLinearAnalysis() => ExecuteNonlinearAnalysis();


		[CommandMethod(Command.Simulation)]
		public static void Simulation() => ExecuteNonlinearAnalysis(true);

		/// <summary>
		///     Execute the nonlinear analysis.
		/// </summary>
		/// <param name="simulate">Execute a simulation until failure?</param>
		private static void ExecuteNonlinearAnalysis(bool simulate = false)
		{
			var model = SPMModel.ActiveModel;

			// Get input data
			var input = model.GenerateInput(AnalysisType.Nonlinear, out var dataOk, out var message)!;

			if (!dataOk)
			{
				ShowAlertDialog(message);
				return;
			}

			// Get the index of node to monitor displacement
			var uIndexn = model.GetMonitoredIndex();

			if (!uIndexn.HasValue)
				return;

			// Get analysis settings
			var settings = SPMModel.ActiveModel.Settings.Analysis;

			// Do analysis
			var analysis = new SPMAnalysis(input, settings);

			// Show window
			var plot = new PlotWindow(analysis, uIndexn.Value, simulate);
			ShowModalWindow(MainWindow.Handle, plot);

			// Show a message if analysis stopped
			// if (analysis.Stop)
			// 	ShowAlertDialog(analysis.StopMessage);

			// Updated plot
			// plot.UpdatePlot();

			var results = new SPMResults(model);
			results.DrawResults();
		}

		#endregion

	}
}