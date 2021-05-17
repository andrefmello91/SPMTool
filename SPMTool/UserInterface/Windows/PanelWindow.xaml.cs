using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using andrefmello91.Extensions;
using andrefmello91.Material.Reinforcement;
using MathNet.Numerics;
using SPMTool.Core;
using SPMTool.Core.Elements;
using SPMTool.Enums;
using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Core.SPMModel;

#nullable enable

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Lógica interna para StringerGeometryWindow.xaml
	/// </summary>
	public partial class PanelWindow
	{

		#region Fields

		private readonly LengthUnit _geometryUnit;
		private readonly List<PanelObject> _panels;
		private readonly LengthUnit _reinforcementUnit;
		private readonly PressureUnit _stressUnit;

		#endregion

		#region Properties

		public string DiameterUnit => _reinforcementUnit.Abbrev();

		// Properties
		public string GeometryUnit => _geometryUnit.Abbrev();

		/// <summary>
		///     Get header text.
		/// </summary>
		public string HeaderText => _panels.Count == 1
			? $"Panel {_panels[0].Number}"
			: $"{_panels.Count} panels selected";

		public string StressUnit => _stressUnit.Abbrev();

		/// <summary>
		///     Get/set reinforcement for output (steel is not set).
		/// </summary>
		private WebReinforcement? OutputReinforcement
		{
			get => !ReinforcementXChecked && !ReinforcementYChecked ? null : new WebReinforcement(OutputReinforcementX, OutputReinforcementY, PnlWidth);
			set
			{
				OutputReinforcementX = value?.DirectionX;
				OutputReinforcementY = value?.DirectionY;
			}
		}

		/// <summary>
		///     Get/set reinforcement for X output (steel is not set).
		/// </summary>
		private WebReinforcementDirection? OutputReinforcementX
		{
			get => ReinforcementXChecked ? new WebReinforcementDirection(XBarDiameter, XSpacing, OutputSteelX, PnlWidth, 0) : null;
			set
			{
				XSpacing     = value?.BarSpacing ?? Length.FromMillimeters(100);
				XBarDiameter = value?.BarDiameter ?? Length.FromMillimeters(8);
			}
		}

		/// <summary>
		///     Get/set reinforcement for Y output (steel is not set).
		/// </summary>
		private WebReinforcementDirection? OutputReinforcementY
		{
			get => ReinforcementYChecked ? new WebReinforcementDirection(YBarDiameter, YSpacing, OutputSteelY, PnlWidth, Constants.PiOver2) : null;
			set
			{
				YSpacing     = value?.BarSpacing ?? Length.FromMillimeters(100);
				YBarDiameter = value?.BarDiameter ?? Length.FromMillimeters(8);
			}
		}

		/// <summary>
		///     Get/set steel for X output.
		/// </summary>
		private Steel? OutputSteelX
		{
			get => new(XYieldStress, XElasticModule);
			set
			{
				XYieldStress   = value?.YieldStress ?? Pressure.FromMegapascals(500);
				XElasticModule = value?.ElasticModule ?? Pressure.FromMegapascals(210000);
			}
		}

		/// <summary>
		///     Get/set steel for Y output.
		/// </summary>
		private Steel? OutputSteelY
		{
			get => new(YYieldStress, YElasticModule);
			set
			{
				YYieldStress   = value?.YieldStress ?? Pressure.FromMegapascals(500);
				YElasticModule = value?.ElasticModule ?? Pressure.FromMegapascals(210000);
			}
		}

		/// <summary>
		///     Get/set width.
		/// </summary>
		private Length PnlWidth
		{
			get => Length.From(double.Parse(WBox.Text), _geometryUnit);
			set => WBox.Text = $"{value.Value:0.00}";
		}

		/// <summary>
		///     Get X reinforcement <see cref="TextBox" />'s.
		/// </summary>
		private IEnumerable<TextBox> ReinforcementXBoxes => new[] { SxBox, PhiXBox, FxBox, ExBox };

		/// <summary>
		///     Gets and sets X reinforcement checkbox state.
		/// </summary>
		private bool ReinforcementXChecked
		{
			get => ReinforcementXCheck.IsChecked ?? false;
			set
			{
				if (value)
				{
					ReinforcementXBoxes.Enable();

					if (Steels.Any())
						SavedSteelX.Enable();

					if (PanelReinforcements.Any())
						SavedReinforcementX.Enable();
				}
				else
				{
					ReinforcementXBoxes.Disable();

					if (Steels.Any())
						SavedSteelX.Disable();

					if (PanelReinforcements.Any())
						SavedReinforcementX.Disable();
				}

				ReinforcementXCheck.IsChecked = value;
			}
		}

		/// <summary>
		///     Verify if X reinforcement text boxes are filled.
		/// </summary>
		private bool ReinforcementXFilled => CheckBoxes(ReinforcementXBoxes);

		/// <summary>
		///     Get Y reinforcement <see cref="TextBox" />'s.
		/// </summary>
		private IEnumerable<TextBox> ReinforcementYBoxes => new[] { SyBox, PhiYBox, FyBox, EyBox };

		/// <summary>
		///     Gets and sets Y reinforcement checkbox state.
		/// </summary>
		private bool ReinforcementYChecked
		{
			get => ReinforcementYCheck.IsChecked ?? false;
			set
			{
				if (value)
				{
					ReinforcementYBoxes.Enable();

					if (Steels.Any())
						SavedSteelY.Enable();

					if (PanelReinforcements.Any())
						SavedReinforcementY.Enable();
				}
				else
				{
					ReinforcementYBoxes.Disable();

					if (Steels.Any())
						SavedSteelY.Disable();

					if (PanelReinforcements.Any())
						SavedReinforcementY.Disable();
				}

				ReinforcementYCheck.IsChecked = value;
			}
		}

		/// <summary>
		///     Verify if Y reinforcement text boxes are filled.
		/// </summary>
		private bool ReinforcementYFilled => CheckBoxes(ReinforcementYBoxes);

		/// <summary>
		///     Get and set option to save geometry to all stringers.
		/// </summary>
		private bool SetGeometry
		{
			get => SetGeometryBox.IsChecked ?? false;
			set => SetGeometryBox.IsChecked = value;
		}

		/// <summary>
		///     Get and set option to save reinforcement to all stringers.
		/// </summary>
		private bool SetReinforcement
		{
			get => SetReinforcementBox.IsChecked ?? false;
			set => SetReinforcementBox.IsChecked = value;
		}

		/// <summary>
		///     Verify if geometry text boxes are filled.
		/// </summary>
		private bool WidthFilled => CheckBoxes(new[] { WBox });

		/// <summary>
		///     Get/set X bar diameter.
		/// </summary>
		private Length XBarDiameter
		{
			get => Length.From(double.Parse(PhiXBox.Text), _reinforcementUnit);
			set => PhiXBox.Text = $"{value.Value:0.00}";
		}

		/// <summary>
		///     Get/set X bar elastic module.
		/// </summary>
		private Pressure XElasticModule
		{
			get => Pressure.From(double.Parse(ExBox.Text), _stressUnit);
			set => ExBox.Text = $"{value.Value:0.00}";
		}

		/// <summary>
		///     Get/set X spacing.
		/// </summary>
		private Length XSpacing
		{
			get => Length.From(double.Parse(SxBox.Text), _geometryUnit);
			set => SxBox.Text = $"{value.Value:0.00}";
		}

		/// <summary>
		///     Get/set X bar yield stress.
		/// </summary>
		private Pressure XYieldStress
		{
			get => Pressure.From(double.Parse(FxBox.Text), _stressUnit);
			set => FxBox.Text = $"{value.Value:0.00}";
		}

		/// <summary>
		///     Get/set X bar diameter.
		/// </summary>
		private Length YBarDiameter
		{
			get => Length.From(double.Parse(PhiYBox.Text), _reinforcementUnit);
			set => PhiYBox.Text = $"{value.Value:0.00}";
		}

		/// <summary>
		///     Get/set Y bar elastic module.
		/// </summary>
		private Pressure YElasticModule
		{
			get => Pressure.From(double.Parse(EyBox.Text), _stressUnit);
			set => EyBox.Text = $"{value.Value:0.00}";
		}

		/// <summary>
		///     Get/set Y spacing.
		/// </summary>
		private Length YSpacing
		{
			get => Length.From(double.Parse(SyBox.Text), _geometryUnit);
			set => SyBox.Text = $"{value.Value:0.00}";
		}

		/// <summary>
		///     Get/set Y bar yield stress.
		/// </summary>
		private Pressure YYieldStress
		{
			get => Pressure.From(double.Parse(FyBox.Text), _stressUnit);
			set => FyBox.Text = $"{value.Value:0.00}";
		}

		#endregion

		#region Constructors

		public PanelWindow(IEnumerable<PanelObject> panels)
		{
			_panels = panels.ToList();

			_geometryUnit      = SPMDatabase.Settings.Units.Geometry;
			_reinforcementUnit = SPMDatabase.Settings.Units.Reinforcement;
			_stressUnit        = SPMDatabase.Settings.Units.MaterialStrength;
			
			DataContext        = this;

			InitializeComponent();

			// Get stringer image
			Geometry.Source = Icons.GetBitmap(Properties.Resources.panel_geometry);

			GetInitialGeometry();

			GetInitialReinforcement();

		}

		#endregion

		#region Methods

		/// <summary>
		///     Get saved geometry options as string collection.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<string> SavedGeoOptions() => ElementWidths.Select(geo => $"{geo.Value:0.00}");

		/// <summary>
		///     Get saved reinforcement options as string collection.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<string> SavedRefOptions() => PanelReinforcements.Select(r => $"{(char) Character.Phi} {r.BarDiameter.Value:0.0} at {r.BarSpacing.Value:0.0}");

		/// <summary>
		///     Get saved steel options as string collection.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<string> SavedSteelOptions() => StringerWindow.SavedSteelOptions();

		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			// Check geometry
			if (SetGeometry)
			{
				if (!WidthFilled)
				{
					MessageBox.Show("Please set  positive and non zero values for panel geometry.", "Alert");
					return;
				}

				SaveGeometry();
			}

			// Check if reinforcement is set
			if (SetReinforcement)
			{
				if (ReinforcementXChecked && !ReinforcementXFilled)
				{
					MessageBox.Show("Please set positive and non zero values for reinforcement properties or uncheck X reinforcement checkbox.", "Alert");
					return;
				}

				if (ReinforcementYChecked && !ReinforcementYFilled)
				{
					MessageBox.Show("Please set positive and non zero values for reinforcement properties or uncheck Y reinforcement checkbox.", "Alert");
					return;
				}

				SaveReinforcement();
			}

			Close();
		}

		/// <summary>
		///     Get the initial geometry data of panels.
		/// </summary>
		private void GetInitialGeometry()
		{
			SetGeometry = true;

			if (ElementWidths.Any())
			{
				SavedGeometries.ItemsSource   = SavedGeoOptions();
				
				SavedGeometries.SelectedIndex = _panels.Count == 1
					? ElementWidths.IndexOf(_panels[0].Width)
					: 0;

				PnlWidth = _panels.Count == 1
					? _panels[0].Width
					: ElementWidths[0];
				
				return;
				
			}
			SavedGeometries.Disable();
			PnlWidth = Length.FromMillimeters(100);
		}

		/// <summary>
		///     Get the initial reinforcement data of panels.
		/// </summary>
		private void GetInitialReinforcement()
		{
			SetReinforcement = true;

			if (Steels.Any())
			{
				SavedSteelX.ItemsSource   = SavedSteelY.ItemsSource   = SavedSteelOptions();
				SavedSteelX.SelectedIndex = SavedSteelY.SelectedIndex = 0;

				if (_panels.Count > 1)
					OutputSteelX = OutputSteelY = Steels[0];
			}
			else
			{
				SavedSteelX.Disable();
				SavedSteelY.Disable();
				OutputSteelX = OutputSteelY = null;
			}

			if (PanelReinforcements.Any())
			{
				SavedReinforcementX.ItemsSource   = SavedReinforcementY.ItemsSource   = SavedRefOptions();
				SavedReinforcementX.SelectedIndex = SavedReinforcementY.SelectedIndex = 0;

				if (_panels.Count > 1)
					OutputReinforcementX = OutputReinforcementY = PanelReinforcements[0];
			}
			else
			{
				SavedReinforcementX.Disable();
				SavedReinforcementY.Disable();
				OutputReinforcementX = OutputReinforcementY = null;
			}

			if (_panels.Count == 1)
			{
				var reinforcement = _panels[0].Reinforcement;

				ReinforcementXChecked = reinforcement?.DirectionX is not null;
				ReinforcementYChecked = reinforcement?.DirectionY is not null;

				OutputReinforcement = reinforcement;
				OutputSteelX        = reinforcement?.DirectionX?.Steel;
				OutputSteelY        = reinforcement?.DirectionY?.Steel;
			}

			else
				ReinforcementXChecked = ReinforcementYChecked = false;
		}

		private void ReinforcementXCheck_OnCheck(object sender, RoutedEventArgs e) => ReinforcementXChecked = ((CheckBox) sender).IsChecked ?? false;

		private void ReinforcementYCheck_OnCheck(object sender, RoutedEventArgs e) => ReinforcementYChecked = ((CheckBox) sender).IsChecked ?? false;

		private void SavedGeometries_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= ElementWidths.Count || i < 0)
				return;

			// Update textboxes
			PnlWidth = ElementWidths[i];
		}

		private void SavedReinforcementX_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= PanelReinforcements.Count || i < 0)
				return;

			// Update textboxes
			OutputReinforcementX = PanelReinforcements[i];
		}

		private void SavedReinforcementY_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= PanelReinforcements.Count || i < 0)
				return;

			// Update textboxes
			OutputReinforcementY = PanelReinforcements[i];
		}

		private void SavedSteelX_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= Steels.Count || i < 0)
				return;

			// Update textboxes
			OutputSteelX = Steels[i];
		}

		private void SavedSteelY_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= Steels.Count || i < 0)
				return;

			// Update textboxes
			OutputSteelY = Steels[i];
		}

		/// <summary>
		///     Save data in the stringer object.
		/// </summary>
		private void SaveGeometry()
		{
			// Convert values
			var width = PnlWidth;

			// Save on database
			ElementWidths.Add(width);

			// Set to panels
			foreach (var pnl in _panels)
				pnl.Width = width;
		}

		/// <summary>
		///     Save data in the stringer object.
		/// </summary>
		private void SaveReinforcement()
		{
			// Set to panels
			var reinforcement = OutputReinforcement;

			PanelReinforcements.Add(reinforcement?.DirectionX);
			PanelReinforcements.Add(reinforcement?.DirectionY);

			foreach (var pnl in _panels)
				pnl.Reinforcement = reinforcement;
		}

		private void SetGeometry_OnCheck(object sender, RoutedEventArgs e) => SetGeometry = ((CheckBox) sender).IsChecked ?? false;

		private void SetReinforcement_OnCheck(object sender, RoutedEventArgs e) => SetReinforcement = ((CheckBox) sender).IsChecked ?? false;

		#endregion

	}
}