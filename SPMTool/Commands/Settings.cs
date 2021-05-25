using System.Windows;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Application.UserInterface;
using SPMTool.Commands;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SPMTool.Commands
{
	/// <summary>
	///     Settings command class.
	/// </summary>
	public static partial class AcadCommands
	{

		#region Methods

		/// <summary>
		///     Set analysis settings.
		/// </summary>
		[CommandMethod(Command.Analysis)]
		public static void SetAnalysisSettings() => SPMToolInterface.ShowWindow(new AnalysisConfig());

		/// <summary>
		///     Set units.
		/// </summary>
		[CommandMethod(Command.Units)]
		public static void SetUnits() => SPMToolInterface.ShowWindow(new UnitsConfig());
		
		/// <summary>
		///     Set display settings.
		/// </summary>
		[CommandMethod(Command.Display)]
		public static void SetDisplaySettings() => SPMToolInterface.ShowWindow(new DisplayConfig());

		#endregion

		/// <summary>
		///     Set concrete parameters to model.
		/// </summary>
		[CommandMethod(Command.Parameters)]
		public static void ConcreteParameters() => SPMToolInterface.ShowWindow(new ConcreteConfig());
	}
}