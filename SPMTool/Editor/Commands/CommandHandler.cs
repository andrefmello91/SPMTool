using System;
using Autodesk.Windows;
using SPMTool.Core;

namespace SPMTool.Editor.Commands
{
	/// <summary>
	/// Command Handler class.
	/// </summary>
	public class CommandHandler : System.Windows.Input.ICommand
	{
		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter) => true;

		/// <summary>
		/// Execute a command.
		/// </summary>
		public void Execute(object parameter)
		{
			if (parameter is null || !(parameter is RibbonButton button))
				return;

			// Get escape command
			var esc = CommandEscape();

			//Make sure the command text either ends with ";", or a " "
			var cmdText = ((string) button.CommandParameter).Trim();

			if (!cmdText.EndsWith(";"))
				cmdText += " ";

			DataBase.Document.SendStringToExecute(esc + cmdText, true, false, true);
		}

		/// <summary>
		/// Escape running commands.
		/// </summary>
		private static string CommandEscape()
		{
			var cmds = (string) Autodesk.AutoCAD.ApplicationServices.Core.Application.GetSystemVariable("CMDNAMES");

			if (cmds.Length == 0)
				return String.Empty;

			var cmdNum = cmds.Split('\'').Length;

			var esc = String.Empty;
			for (int i = 0; i < cmdNum; i++)
				esc += '\x03';

			return esc;
		}
	}
}