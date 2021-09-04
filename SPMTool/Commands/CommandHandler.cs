using System;
using System.Windows.Input;
using Autodesk.Windows;
using SPMTool.Core;

namespace SPMTool.Commands
{
	/// <summary>
	///     Command Handler class.
	/// </summary>
	public class CommandHandler : ICommand
	{

		#region Methods

		/// <summary>
		///     Escape running commands.
		/// </summary>
		private static string CommandEscape()
		{
			var cmds = (string) Autodesk.AutoCAD.ApplicationServices.Core.Application.GetSystemVariable("CMDNAMES");

			if (cmds.Length == 0)
				return string.Empty;

			var cmdNum = cmds.Split('\'').Length;

			var esc = string.Empty;
			for (var i = 0; i < cmdNum; i++)
				esc += '\x03';

			return esc;
		}

		#endregion

		public event EventHandler? CanExecuteChanged;

		#region Interface Implementations

		public bool CanExecute(object parameter) => true;

		/// <summary>
		///     Execute a command.
		/// </summary>
		public void Execute(object parameter)
		{
			if (parameter is not RibbonButton button)
				return;

			// Get escape command
			var esc = CommandEscape();

			//Make sure the command text either ends with ";", or a " "
			var cmdText = ((string) button.CommandParameter).Trim();

			if (!cmdText.EndsWith(";"))
				cmdText += " ";

			SPMModel.ActiveModel.AcadDocument.SendStringToExecute(esc + cmdText, true, false, true);
		}

		#endregion

	}
}