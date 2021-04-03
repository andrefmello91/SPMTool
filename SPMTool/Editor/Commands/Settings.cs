using Autodesk.AutoCAD.Runtime;
using SPMTool.Application.UserInterface;
using SPMTool.Editor.Commands;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(Settings))]

namespace SPMTool.Editor.Commands
{
	/// <summary>
	///     Settings command class.
	/// </summary>
	public static class Settings
	{

		#region Methods

		/// <summary>
		///     Set analysis settings.
		/// </summary>
		[CommandMethod(CommandName.Analysis)]
		public static void SetAnalysisSettings()
		{
			// Start the window of units configuration
			var analysisConfig = new AnalysisConfig();
			ShowModalWindow(MainWindow.Handle, analysisConfig, false);
		}

		/// <summary>
		///     Set units.
		/// </summary>
		[CommandMethod(CommandName.Units)]
		public static void SetUnits()
		{
			// Start the window of units configuration
			var unitConfig = new UnitsConfig();
			ShowModalWindow(MainWindow.Handle, unitConfig, false);
		}

		#endregion

	}
}