using Autodesk.AutoCAD.Runtime;
using SPMTool.Application.UserInterface;
using SPMTool.UserInterface.Windows;

namespace SPMTool.Commands
{
	/// <summary>
	///     Settings command class.
	/// </summary>
	public static partial class AcadCommands
	{

		#region Methods

		/// <summary>
		///     Set concrete parameters to model.
		/// </summary>
		[CommandMethod(Command.Parameters)]
		public static void ConcreteParameters() => SPMToolInterface.ShowWindow(new ConcreteConfig());

		/// <summary>
		///     Set analysis settings.
		/// </summary>
		[CommandMethod(Command.Analysis)]
		public static void SetAnalysisSettings() => SPMToolInterface.ShowWindow(new AnalysisConfig());

		/// <summary>
		///     Set display settings.
		/// </summary>
		[CommandMethod(Command.Display)]
		public static void SetDisplaySettings() => SPMToolInterface.ShowWindow(new DisplayConfig());

		/// <summary>
		///     Set units.
		/// </summary>
		[CommandMethod(Command.Units)]
		public static void SetUnits() => SPMToolInterface.ShowWindow(new UnitsConfig());

		/// <summary>
		///     View information.
		/// </summary>
		[CommandMethod(Command.Info)]
		public static void ViewInfo() => SPMToolInterface.ShowWindow(new InfoWindow());

		#endregion

	}
}