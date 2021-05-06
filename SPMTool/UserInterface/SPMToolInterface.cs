using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using SPMTool.Core;
using SPMTool.Editor.Commands;
using SPMTool.Extensions;

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
			Title = DataBase.AppName,
			Id    = DataBase.AppName
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
			var tab = Ribbon.FindTab(DataBase.AppName);

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

			splitButton.Items.Add(Command.Linear.GetRibbonButton());

			splitButton.Items.Add(Command.Nonlinear.GetRibbonButton());
			
			splitButton.Items.Add(Command.Simulation.GetRibbonButton());

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
			pnlSrc.Items.Add(Command.Parameters.GetRibbonButton());
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

			splitButton1.Items.Add(Command.AddStringer.GetRibbonButton());

			splitButton1.Items.Add(Command.AddPanel.GetRibbonButton());

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
			splitButton2.Items.Add(Command.EditStringer.GetRibbonButton());

			splitButton2.Items.Add(Command.EditPanel.GetRibbonButton());

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

			splitButton3.Items.Add(Command.AddConstraint.GetRibbonButton());

			splitButton3.Items.Add(Command.AddForce.GetRibbonButton());

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

			rbSpBtn3.Items.Add(Command.DivideStringer.GetRibbonButton(RibbonItemSize.Standard));

			rbSpBtn3.Items.Add(Command.DividePanel.GetRibbonButton(RibbonItemSize.Standard));

			// Add to the sub panel and create a new ribbon row
			subPnl3.Items.Add(rbSpBtn3);
			subPnl3.Items.Add(new RibbonRowBreak());

			// Update elements button
			subPnl3.Items.Add(Command.ElementData.GetRibbonButton(RibbonItemSize.Standard));
			subPnl3.Items.Add(new RibbonRowBreak());
			subPnl3.Items.Add(Command.UpdateElements.GetRibbonButton(RibbonItemSize.Standard));

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

			splitButton.Items.Add(Command.StringerForces.GetRibbonButton());

			splitButton.Items.Add(Command.PanelShear.GetRibbonButton());

			splitButton.Items.Add(Command.PanelStresses.GetRibbonButton());

			splitButton.Items.Add(Command.ConcreteStresses.GetRibbonButton());

			splitButton.Items.Add(Command.Displacements.GetRibbonButton());

			splitButton.Items.Add(Command.Cracks.GetRibbonButton());

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

			pnlSrc.Items.Add(Command.Units.GetRibbonButton());

			pnlSrc.Items.Add(Command.Analysis.GetRibbonButton());
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

			splitButton.Items.Add(Command.Nodes.GetRibbonButton());

			splitButton.Items.Add(Command.Stringers.GetRibbonButton());

			splitButton.Items.Add(Command.Panels.GetRibbonButton());

			splitButton.Items.Add(Command.Forces.GetRibbonButton());

			splitButton.Items.Add(Command.Supports.GetRibbonButton());

			// Add to the panel source
			pnlSrc.Items.Add(splitButton);
		}

		#endregion

	}
}