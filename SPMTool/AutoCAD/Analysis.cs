using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using SPMTool.Core;

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

			if (input.Concrete.IsSet)
			{
				// Do a linear analysis
				var analysis = new Analysis.Linear(input);

				// Draw results of analysis
				Results.Draw(analysis);
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
			InputData input = new InputData(strBehavior, pnlBehavior);

			if (input.Concrete.IsSet && (strBehavior, pnlBehavior) != default)
			{
				// Do a linear analysis
				var analysis = new Analysis.NonLinear(input);

				// Draw results of analysis
				AutoCAD.Results.Draw(analysis);
			}

			else
				Application.ShowAlertDialog("Please set concrete parameters and elements behavior");
		}


	}
}
