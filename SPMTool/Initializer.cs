using System;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;
using SPMTool.Database;
using SPMTool.UserInterface;

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
		public void Initialize()
		{
			Application.Idle += On_ApplicationIdle;

			// Open the Registered Applications table and check if custom app exists. If it doesn't, then it's created:
			DataBase.RegisterApp();

			// Create layers and blocks
			DataBase.CreateLayers();
			Model.CreateBlocks();

			// Add command event handler
			DataBase.Document.CommandEnded += Model.On_UndoOrRedo;
		}

        /// <summary>
        /// Terminate application.
        /// </summary>
        public void Terminate() => Application.SystemVariableChanged -= ColorThemeChanged;

		/// <summary>
		/// Initialize user interface and create layers and blocks.
		/// </summary>
		public void On_ApplicationIdle(object sender, EventArgs e)
		{
            Ribbon.AddButtons();

			Application.SystemVariableChanged += ColorThemeChanged;

			Application.Idle -= On_ApplicationIdle;
		}

        /// <summary>
        /// Alternate colors if theme is changed.
        /// </summary>
        public void ColorThemeChanged(object senderObj, Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs sysVarChEvtArgs)
		{
			// Check if it's a theme change
			if (sysVarChEvtArgs.Name != "COLORTHEME")
				return;

			// Reinitialize the ribbon buttons
			Ribbon.AddButtons();
		}
	}
}