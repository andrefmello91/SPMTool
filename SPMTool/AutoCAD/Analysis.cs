using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using SPMTool.Elements;
using SPMTool.Analysis;

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
				var analysis = new Analysis.Analysis.Linear(input);

				// Draw results of analysis
				Results.Draw(analysis);
			}

			else
				Application.ShowAlertDialog("Please set concrete parameters");
		}

		[CommandMethod("DoNonLinearAnalysis")]
		public static void DoNonLinearAnalysis()
		{
			// Get input data
			InputData input = new InputData(Stringer.Behavior.NonLinearClassic, Panel.Behavior.MCFT);

			if (input.Concrete.IsSet)
			{
				// Do a linear analysis
				var analysis = new Analysis.Analysis.NonLinear(input);

				// Draw results of analysis
				AutoCAD.Results.Draw(analysis);
			}

			else
				Application.ShowAlertDialog("Please set concrete parameters");
		}


	}
}
