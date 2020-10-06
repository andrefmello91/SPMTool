using System.Threading;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using SPM.Analysis;
using SPM.Elements;
using SPMTool.UserInterface;
using SPMTool.Database;
using SPMTool.Enums;
using static SPMTool.Database.Data;


[assembly: CommandClass(typeof(SPMTool.Model.Conditions.Results))]

namespace SPMTool.Model.Conditions
{
	public static partial class Results
	{
		[CommandMethod("DoLinearAnalysis")]
		public static void DoLinearAnalysis()
		{
            // Get input data
            var input = ReadInput(AnalysisType.Linear, out var dataOk, out var message);

            if (!dataOk)
            {
	            Application.ShowAlertDialog(message);
				return;
            }
			// Do a linear analysis
			var analysis = new LinearAnalysis(input);

			// Draw results of analysis
			Draw(analysis, input.Units);
		}

		[CommandMethod("DoNonLinearAnalysis")]
		public static void DoNonLinearAnalysis()
		{
			// Get input data
			InputData input = new InputData(AnalysisType.Nonlinear);

			if (input.ConcreteParameters.IsSet)
			{
				// Get the index of node to monitor displacement
				var uIndexn = MonitoredIndex();

				if(!uIndexn.HasValue)
					return;

				int uIndex = uIndexn.Value;

                // Do analysis
                var analysis = new NonLinearAnalysis(input, uIndex);

                // Show load-displacement diagram
                var u  = analysis.MonitoredDisplacements.ToArray();
                var lf = analysis.MonitoredLoadFactor.ToArray();
                Application.ShowModelessWindow(Application.MainWindow.Handle, new GraphWindow(u, lf, input.Units.Displacements));

                // Draw results of analysis
                Draw(analysis, input.Units);
			}

			else
				Application.ShowAlertDialog("Please set concrete parameters and elements behavior");
		}

		/// <summary>
        /// Ask the user to select a node to monitor and return the DoF index.
        /// </summary>
		private static int? MonitoredIndex()
		{
			// Ask user to select a node
			var nd = UserInput.SelectEntity("Select a node to monitor displacement:", new [] { Layer.ExtNode, Layer.IntNode });

			if (nd is null)
				return null;

			// Ask direction to monitor
			var options = new []
			{
				Direction.X.ToString(),
				Direction.Y.ToString()
			};
			var res = UserInput.SelectKeyword("Select a direction to monitor displacement:", options, options[0]);

			if (!res.HasValue)
				return null;

			// Get the node global indexes
			var node  = Nodes.Read(nd.ObjectId, Units.Default);
			var index = node.DoFIndex;

			// Verify selected direction
			int dirIndex = res.Value.index;

			return
				index[dirIndex];
		}
	}
}
