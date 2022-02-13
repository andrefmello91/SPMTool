using System.Diagnostics;
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
		///     View SPMTool Wiki.
		/// </summary>
		[CommandMethod(Command.SPMToolHelp)]
		public static void ViewHelp() => Process.Start(SPMToolInterface.SPMToolWiki);
		/// <summary>
		///     View information.
		/// </summary>
		[CommandMethod(Command.SPMToolInfo)]
		public static void ViewInfo() => SPMToolInterface.ShowWindow(new InfoWindow());

		#endregion

	}
}