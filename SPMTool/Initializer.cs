﻿using System;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Application.UserInterface;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SPMTool
{
	/// <summary>
	///     Initializer class.
	/// </summary>
	public class Initializer : IExtensionApplication
	{

		#region Methods

		/// <summary>
		///     Initialize application.
		/// </summary>
		public void Initialize() => Idle += On_ApplicationIdle;

		/// <summary>
		///     Terminate application.
		/// </summary>
		public void Terminate() => SystemVariableChanged -= SPMToolInterface.ColorThemeChanged;

		/// <summary>
		///     Initialize user interface and create layers and blocks.
		/// </summary>
		private static void On_ApplicationIdle(object sender, EventArgs e)
		{
			// Add application buttons
			SPMToolInterface.AddButtons();

			SystemVariableChanged += SPMToolInterface.ColorThemeChanged;

			Idle -= On_ApplicationIdle;
		}

		#endregion

	}
}