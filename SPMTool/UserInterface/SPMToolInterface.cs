using System.Reflection;
using System.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using SPMTool.Attributes;
using SPMTool.Commands;
using SPMTool.Core;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Ribbon class.
	/// </summary>
	public class SPMToolInterface
	{

		#region Fields

		/// <summary>
		///     Icons for user interface.
		/// </summary>
		public static readonly Icons Icons = new();

		/// <summary>
		///     Create the application <see cref="RibbonTab" />.
		/// </summary>
		private readonly RibbonTab _tab = new()
		{
			Title = SPMModel.AppName,
			Id    = SPMModel.AppName
		};

		#endregion

		#region Properties

		/// <summary>
		///     Get the Ribbon Control from AutoCAD.
		/// </summary>
		public static RibbonControl Ribbon => ComponentManager.Ribbon;

		#endregion

		#region Methods

		/// <summary>
		///     Add ribbon buttons to user interface.
		/// </summary>
		public static void AddButtons()
		{
			var spmInt = new SPMToolInterface();

			// Check if the tab already exists
			var tab = Ribbon.FindTab(SPMModel.AppName);

			if (tab != null)
				Ribbon.Tabs.Remove(tab);

			Ribbon.Tabs.Add(spmInt._tab);

			// Update Icons
			Icons.GetIcons();

			// Create the Ribbon panels
			spmInt.CreatePanels();

			// Activate tab
			spmInt._tab.IsActive = true;
		}

		/// <summary>
		///     Create a <see cref="RibbonButton" /> based in a command name, contained in <see cref="Command" />.
		/// </summary>
		public static RibbonButton? GetRibbonButton(string commandName, RibbonItemSize size = RibbonItemSize.Large, bool showText = true) =>
			((CommandAttribute?) typeof(Command).GetField(commandName)?.GetCustomAttribute(typeof(CommandAttribute)))?.CreateRibbonButton(size, showText);

		/// <summary>
		///     Show a modal window in AutoCAD interface.
		/// </summary>
		/// <param name="window">The <see cref="Window" /> to show.</param>
		/// <param name="modeless">Show as a modeless window?</param>
		public static void ShowWindow(Window window, bool modeless = false)
		{
			if (modeless)
			{
				ShowModelessWindow(MainWindow.Handle, window, false);
				return;
			}

			ShowModalWindow(MainWindow.Handle, window, false);
		}

		/// <summary>
		///     Create Analysis Panel.
		/// </summary>
		private void AnalysisPanel()
		{
			var pnlSrc = new RibbonPanelSource { Title = "Analysis" };
			_tab.Panels.Add(new RibbonPanel { Source   = pnlSrc });

			var splitButton = new RibbonSplitButton
			{
				ShowText                      = true,
				IsSplit                       = true,
				Size                          = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

			splitButton.Items.Add(GetRibbonButton(Command.Linear));

			splitButton.Items.Add(GetRibbonButton(Command.Nonlinear));

			splitButton.Items.Add(GetRibbonButton(Command.Simulation));

			// Add to the panel source
			pnlSrc.Items.Add(splitButton);
		}

		/// <summary>
		///     Create Concrete Panel.
		/// </summary>
		private void ConcretePanel()
		{
			var pnlSrc = new RibbonPanelSource { Title = "Concrete" };
			_tab.Panels.Add(new RibbonPanel { Source   = pnlSrc });

			// Material parameters button
			pnlSrc.Items.Add(GetRibbonButton(Command.Parameters));
		}

		/// <summary>
		///     Create the Ribbon panels.
		/// </summary>
		private void CreatePanels()
		{
			ModelPanel();
			ConcretePanel();
			AnalysisPanel();
			ViewPanel();
			ResultsPanel();
			SettingsPanel();
		}

		/// <summary>
		///     Create Model Panel.
		/// </summary>
		private void ModelPanel()
		{
			var pnlSrc = new RibbonPanelSource { Title = "Model" };
			_tab.Panels.Add(new RibbonPanel { Source   = pnlSrc });

			// Create a split button for geometry creation
			var splitButton1 = new RibbonSplitButton
			{
				ShowText                      = true,
				Text                          = "Add element",
				IsSplit                       = true,
				Size                          = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

			splitButton1.Items.Add(GetRibbonButton(Command.AddStringer));

			splitButton1.Items.Add(GetRibbonButton(Command.AddPanel));

			// Add to the panel source
			pnlSrc.Items.Add(splitButton1);

			// Create a secondary panel
			var subPnl1 = new RibbonRowPanel();

			// Create a split button for geometry
			var splitButton2 = new RibbonSplitButton
			{
				ShowText                      = true,
				IsSplit                       = true,
				Size                          = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

			// Material parameters buttons
			splitButton2.Items.Add(GetRibbonButton(Command.EditStringer));

			splitButton2.Items.Add(GetRibbonButton(Command.EditPanel));

			subPnl1.Items.Add(splitButton2);
			pnlSrc.Items.Add(subPnl1);

			// Create a secondary panel
			var subPnl2 = new RibbonRowPanel();

			// Create a split button for constraints and forces
			var splitButton3 = new RibbonSplitButton
			{
				ShowText                      = true,
				Text                          = "Constraints / forces",
				IsSplit                       = true,
				Size                          = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

			splitButton3.Items.Add(GetRibbonButton(Command.AddConstraint));

			splitButton3.Items.Add(GetRibbonButton(Command.AddForce));

			subPnl2.Items.Add(splitButton3);
			pnlSrc.Items.Add(subPnl2);

			// Create a secondary panel
			var subPnl3 = new RibbonRowPanel();

			// Create a split button for Element division
			var rbSpBtn3 = new RibbonSplitButton
			{
				ShowText                      = true,
				Text                          = "Divide element",
				IsSplit                       = true,
				IsSynchronizedWithCurrentItem = true
			};

			rbSpBtn3.Items.Add(GetRibbonButton(Command.DivideStringer, RibbonItemSize.Standard));

			rbSpBtn3.Items.Add(GetRibbonButton(Command.DividePanel, RibbonItemSize.Standard));

			// Add to the sub panel and create a new ribbon row
			subPnl3.Items.Add(rbSpBtn3);
			subPnl3.Items.Add(new RibbonRowBreak());

			// Update elements button
			subPnl3.Items.Add(GetRibbonButton(Command.ElementData, RibbonItemSize.Standard));
			subPnl3.Items.Add(new RibbonRowBreak());
			subPnl3.Items.Add(GetRibbonButton(Command.UpdateElements, RibbonItemSize.Standard));

			// Add the sub panel to the panel source
			pnlSrc.Items.Add(subPnl3);
		}

		/// <summary>
		///     Create Results Panel.
		/// </summary>
		private void ResultsPanel()
		{
			var pnlSrc = new RibbonPanelSource { Title = "Results" };
			_tab.Panels.Add(new RibbonPanel { Source   = pnlSrc });

			// Create a split button
			var splitButton = new RibbonSplitButton
			{
				ShowText                      = true,
				IsSplit                       = true,
				Size                          = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

			splitButton.Items.Add(GetRibbonButton(Command.StringerForces));

			splitButton.Items.Add(GetRibbonButton(Command.PanelShear));

			splitButton.Items.Add(GetRibbonButton(Command.PanelStresses));

			splitButton.Items.Add(GetRibbonButton(Command.ConcreteStresses));

			splitButton.Items.Add(GetRibbonButton(Command.Displacements));

			splitButton.Items.Add(GetRibbonButton(Command.Cracks));

			// Add to the panel source
			pnlSrc.Items.Add(splitButton);
		}

		/// <summary>
		///     Create Settings Panel.
		/// </summary>
		private void SettingsPanel()
		{
			var pnlSrc = new RibbonPanelSource { Title = "Settings" };
			_tab.Panels.Add(new RibbonPanel { Source   = pnlSrc });

			pnlSrc.Items.Add(GetRibbonButton(Command.Units));

			pnlSrc.Items.Add(GetRibbonButton(Command.Analysis));

			pnlSrc.Items.Add(GetRibbonButton(Command.Display));
		}

		/// <summary>
		///     Create View Panel.
		/// </summary>
		private void ViewPanel()
		{
			var pnlSrc = new RibbonPanelSource { Title = "View" };
			_tab.Panels.Add(new RibbonPanel { Source   = pnlSrc });

			// Create a split button
			var splitButton = new RibbonSplitButton
			{
				ShowText                      = true,
				IsSplit                       = true,
				Size                          = RibbonItemSize.Large,
				IsSynchronizedWithCurrentItem = true
			};

			splitButton.Items.Add(GetRibbonButton(Command.Nodes));

			splitButton.Items.Add(GetRibbonButton(Command.Stringers));

			splitButton.Items.Add(GetRibbonButton(Command.Panels));

			splitButton.Items.Add(GetRibbonButton(Command.Forces));

			splitButton.Items.Add(GetRibbonButton(Command.Supports));

			// Add to the panel source
			pnlSrc.Items.Add(splitButton);
		}

		/// <summary>
		///     Alternate colors if theme is changed.
		/// </summary>
		public static void ColorThemeChanged(object senderObj, SystemVariableChangedEventArgs sysVarChEvtArgs)
		{
			// Check if it's a theme change
			if (sysVarChEvtArgs.Name != "COLORTHEME")
				return;

			// Reinitialize the ribbon buttons
			AddButtons();
		}

		#endregion

	}
}