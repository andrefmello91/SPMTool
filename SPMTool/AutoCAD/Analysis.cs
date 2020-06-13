using System.Threading;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using SPMTool.Core;
using SPMTool.UserInterface;

[assembly: CommandClass(typeof(SPMTool.AutoCAD.Results))]

namespace SPMTool.AutoCAD
{
	public static partial class Results
	{
		[CommandMethod("DoLinearAnalysis")]
		public static void DoLinearAnalysis()
		{

            // Get input data
            InputData input = new InputData(Stringer.Behavior.Linear, Panel.Behavior.Linear);

			if (input.ConcreteParameters.IsSet)
			{
				// Do a linear analysis
				var analysis = new Analysis.Linear(input);

				// Draw results of analysis
				Draw(analysis);
			}

            else
				Application.ShowAlertDialog("Please set concrete parameters");
		}

		[CommandMethod("DoNonLinearAnalysis")]
		public static void DoNonLinearAnalysis()
		{
            // Get elements behavior
            var (strBehavior, pnlBehavior) = Config.ReadBehavior();

			// Get input data
			InputData input = new InputData(Stringer.Behavior.NonLinearMCFT, Panel.Behavior.NonLinearMCFT);

			if (input.ConcreteParameters.IsSet && (strBehavior, pnlBehavior) != default)
			{
				// Get the index of node to monitor displacement
				var uIndexn = MonitoredIndex();

				if(!uIndexn.HasValue)
					return;

				int uIndex = uIndexn.Value;

                // Do analysis
                var analysis = new Analysis.NonLinear(input, uIndex);

                // Show load-displacement diagram
                var u  = analysis.MonitoredDisplacements.ToArray();
                var lf = analysis.MonitoredLoadFactor.ToArray();
                Application.ShowModelessWindow(Application.MainWindow.Handle, new GraphWindow(u, lf));

                // Draw results of analysis
                Draw(analysis);
			}

			else
				Application.ShowAlertDialog("Please set concrete parameters and elements behavior");
		}

		// Select node to monitor and return index
		private static int? MonitoredIndex()
		{
			// Ask user to select a node
			var nd = UserInput.SelectEntity("Select a node to monitor displacement:", new [] { Layers.ExtNode, Layers.IntNode });

			if (nd == null)
				return null;

			// Ask direction to monitor
			var options = new []
			{
				Directions.X.ToString(),
				Directions.Y.ToString()
			};
			var res = UserInput.SelectKeyword("Select a direction to monitor displacement:", options, options[0]);

			if (!res.HasValue)
				return null;

			// Get the node global indexes
			var node  = new Node(nd.ObjectId);
			var index = node.DoFIndex;

			// Verify selected direction
			int dirIndex = res.Value.index;

			return
				index[dirIndex];
		}
	}
}
