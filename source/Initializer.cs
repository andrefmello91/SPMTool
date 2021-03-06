using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Core;
using SPMTool.Application.UserInterface;

using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SPMTool
{
	/// <summary>
    /// Initializer class.
    /// </summary>
	public class Initializer : IExtensionApplication
	{
		/// <summary>
        /// Initialize application.
        /// </summary>
		public void Initialize() => Idle += On_ApplicationIdle;

		/// <summary>
		/// Terminate application.
		/// </summary>
		public void Terminate() => SystemVariableChanged -= ColorThemeChanged;

		/// <summary>
		/// Initialize user interface and create layers and blocks.
		/// </summary>
		public void On_ApplicationIdle(object sender, EventArgs e)
		{
			// Add application buttons
			Ribbon.AddButtons();

			SystemVariableChanged += ColorThemeChanged;

			Idle -= On_ApplicationIdle;
		}

        /// <summary>
        /// Alternate colors if theme is changed.
        /// </summary>
        public void ColorThemeChanged(object senderObj, SystemVariableChangedEventArgs sysVarChEvtArgs)
		{
			// Check if it's a theme change
			if (sysVarChEvtArgs.Name != "COLORTHEME")
				return;

			// Reinitialize the ribbon buttons
			Ribbon.AddButtons();
		}
	}
}