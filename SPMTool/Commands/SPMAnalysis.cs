using System.Linq;
using andrefmello91.Extensions;
using andrefmello91.FEMAnalysis;
using andrefmello91.SPMElements;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Application.UserInterface;
using SPMTool.Core;
using SPMTool.Core.Elements;
using SPMTool.Enums;
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
		public static void NonLinearAnalysis() => ExecuteNonlinearAnalysis(true);


		[CommandMethod(Command.Simulation)]
		public static void Simulation() => ExecuteNonlinearAnalysis(true, true);

		/// <summary>
		///     Execute the nonlinear analysis.
		/// </summary>
		/// <param name="monitorElements">Prompt to choose elements other than the monitored node?.</param>
		/// <param name="simulate">Execute a simulation until failure?</param>
		private static void ExecuteNonlinearAnalysis(bool monitorElements = false, bool simulate = false)
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

			// Get elements to monitor
			if (monitorElements)
				SetMonitors(model, input);

			// Get analysis settings
			var settings = SPMModel.ActiveModel.Settings.Analysis;

			// Do analysis
			var analysis = new SPMAnalysis(input, settings, uIndexn.Value, simulate);

			var plot = new PlotWindow(analysis, simulate);
			ShowModalWindow(MainWindow.Handle, plot);

			var results = new SPMResults(model);
			results.DrawResults();
		}

		/// <summary>
		///     Set monitors to elements.
		/// </summary>
		/// <param name="model">The SPM model.</param>
		/// <param name="input">The finite element input.</param>
		private static void SetMonitors(SPMModel model, IFEMInput input)
		{
			var elements = model.AcadDatabase.GetObjects("Select stringers and panels to monitor. Press ESC to disable element monitoring", new[] { Layer.Stringer, Layer.Panel });

			if (elements.IsNullOrEmpty())
				return;

			// Get SPM object names
			var names = elements
				.Select(e => model.GetSPMObject(e) is ISPMObject spmElement ? spmElement.Name : null)
				.Where(s => s is not null);

			var mds = input.Cast<INumberedElement>()
				.Concat(input.Grips)
				.Where(e => e is IMonitoredElement && names.Contains(e.Name))
				.Cast<IMonitoredElement>()
				.ToList();

			foreach (var element in mds)
				element.Monitored = true;
		}

		#endregion

	}
}