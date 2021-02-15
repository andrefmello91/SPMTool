using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using Extensions.AutoCAD;
using Extensions.Interface;
using Extensions.Number;
using Material.Reinforcement;
using Material.Reinforcement.Biaxial;
using MathNet.Numerics;
using SPM.Elements.StringerProperties;
using SPMTool.ApplicationSettings;
using SPMTool.Database;
using SPMTool.Database.Elements;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using MessageBox = System.Windows.MessageBox;
using Stringer = SPM.Elements.Stringer;
using static SPMTool.Database.Elements.PanelList;
using static SPMTool.Database.Elements.ElementData;
using static SPMTool.Database.Materials.ReinforcementData;
using Window = System.Windows.Window;

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para StringerGeometryWindow.xaml
    /// </summary>
    public partial class PanelWindow : Window
    {
	    private readonly Solid[] _panels;
	    private double[] _savedWidths;
	    private WebReinforcementDirection[] _savedReinforcements;
	    private Steel[] _savedSteel;

        // Properties
        public string GeometryUnit => Settings.Units.Geometry.Abbrev();

        public string DiameterUnit => Settings.Units.Reinforcement.Abbrev();

        public string StressUnit => Settings.Units.MaterialStrength.Abbrev();

        /// <summary>
        /// Get header text.
        /// </summary>
        public string HeaderText => _panels.Length == 1
	        ? $"Panel {_panels[0].ReadXData()[(int) PanelIndex.Number].ToInt()}"
	        : $"{_panels.Length} panels selected";

        /// <summary>
        /// Gets and sets X reinforcement checkbox state.
        /// </summary>
        private bool ReinforcementXChecked
        {
	        get => ReinforcementXCheck.IsChecked ?? false;
	        set
	        {
		        if (value)
		        {
			        ReinforcementXBoxes.Enable();

					if (_savedSteel?.Any() ?? false)
						SavedSteelX.Enable();

					if (_savedReinforcements?.Any() ?? false)
						SavedReinforcementX.Enable();
		        }
		        else
		        {
			        ReinforcementXBoxes.Disable();

			        if (_savedSteel?.Any() ?? false)
				        SavedSteelX.Disable();

			        if (_savedReinforcements?.Any() ?? false)
				        SavedReinforcementX.Disable();
		        }

                ReinforcementXCheck.IsChecked = value;
	        }
        }

        /// <summary>
        /// Gets and sets Y reinforcement checkbox state.
        /// </summary>
        private bool ReinforcementYChecked
        {
	        get => ReinforcementYCheck.IsChecked ?? false;
	        set
	        {
		        if (value)
		        {
			        ReinforcementYBoxes.Enable();

					if (_savedSteel?.Any() ?? false)
						SavedSteelY.Enable();

					if (_savedReinforcements?.Any() ?? false)
						SavedReinforcementY.Enable();
		        }
		        else
		        {
			        ReinforcementYBoxes.Disable();

			        if (_savedSteel?.Any() ?? false)
				        SavedSteelY.Disable();

			        if (_savedReinforcements?.Any() ?? false)
				        SavedReinforcementY.Disable();
		        }

                ReinforcementYCheck.IsChecked = value;
	        }
        }

        /// <summary>
        /// Get and set option to save geometry to all stringers.
        /// </summary>
        private bool SetGeometry
        {
	        get => SetGeometryBox.IsChecked ?? false; 
	        set => SetGeometryBox.IsChecked = value;
        }

        /// <summary>
        /// Get and set option to save reinforcement to all stringers.
        /// </summary>
        private bool SetReinforcement
        {
	        get => SetReinforcementBox.IsChecked ?? false;
	        set => SetReinforcementBox.IsChecked = value;
        }
        /// <summary>
		/// Get X reinforcement <see cref="TextBox"/>'s.
		/// </summary>
		private IEnumerable<TextBox> ReinforcementXBoxes => new [] { SxBox, PhiXBox, FxBox, ExBox };

        /// <summary>
		/// Get Y reinforcement <see cref="TextBox"/>'s.
		/// </summary>
		private IEnumerable<TextBox> ReinforcementYBoxes => new [] { SyBox, PhiYBox, FyBox, EyBox };

        /// <summary>
        /// Verify if geometry text boxes are filled.
        /// </summary>
        private bool WidthFilled => CheckBoxes(new [] {WBox});

        /// <summary>
        /// Verify if X reinforcement text boxes are filled.
        /// </summary>
        private bool ReinforcementXFilled => CheckBoxes(ReinforcementXBoxes);

        /// <summary>
        /// Verify if Y reinforcement text boxes are filled.
        /// </summary>
        private bool ReinforcementYFilled => CheckBoxes(ReinforcementYBoxes);

        /// <summary>
        /// Get/set width, in mm.
        /// </summary>
		private double PnlWidth
		{
			get => double.Parse(WBox.Text).Convert(Settings.Units.Geometry);
			set => WBox.Text = $"{value.ConvertFromMillimeter(Settings.Units.Geometry):0.00}";
		}

		/// <summary>
        /// Get/set X spacing.
        /// </summary>
		private double XSpacing
		{
			get => double.Parse(SxBox.Text).Convert(Settings.Units.Geometry);
			set => SxBox.Text = $"{value.ConvertFromMillimeter(Settings.Units.Geometry):0.00}";
		}

		/// <summary>
        /// Get/set Y spacing.
        /// </summary>
		private double YSpacing
		{
			get => double.Parse(SyBox.Text).Convert(Settings.Units.Geometry);
			set => SyBox.Text = $"{value.ConvertFromMillimeter(Settings.Units.Geometry):0.00}";
		}

		/// <summary>
		/// Get/set X bar diameter, in mm.
		/// </summary>
		private double XBarDiameter
		{
			get => double.Parse(PhiXBox.Text).Convert(Settings.Units.Reinforcement);
			set => PhiXBox.Text = $"{value.ConvertFromMillimeter(Settings.Units.Reinforcement):0.00}";
		}

		/// <summary>
		/// Get/set X bar diameter, in mm.
		/// </summary>
		private double YBarDiameter
		{
			get => double.Parse(PhiYBox.Text).Convert(Settings.Units.Reinforcement);
			set => PhiYBox.Text = $"{value.ConvertFromMillimeter(Settings.Units.Reinforcement):0.00}";
		}

		/// <summary>
		/// Get/set X bar yield stress, in MPa.
		/// </summary>
		private double XYieldStress
		{
			get => double.Parse(FxBox.Text).Convert(Settings.Units.MaterialStrength);
			set => FxBox.Text = $"{value.ConvertFromMPa(Settings.Units.MaterialStrength):0.00}";
		}

		/// <summary>
		/// Get/set Y bar yield stress, in MPa.
		/// </summary>
		private double YYieldStress
		{
			get => double.Parse(FyBox.Text).Convert(Settings.Units.MaterialStrength);
			set => FyBox.Text = $"{value.ConvertFromMPa(Settings.Units.MaterialStrength):0.00}";
		}

		/// <summary>
		/// Get/set X bar elastic module, in MPa.
		/// </summary>
		private double XElasticModule
		{
			get => double.Parse(ExBox.Text).Convert(Settings.Units.MaterialStrength);
			set => ExBox.Text = $"{value.ConvertFromMPa(Settings.Units.MaterialStrength):0.00}";
		}

		/// <summary>
		/// Get/set Y bar elastic module, in MPa.
		/// </summary>
		private double YElasticModule
		{
			get => double.Parse(EyBox.Text).Convert(Settings.Units.MaterialStrength);
			set => EyBox.Text = $"{value.ConvertFromMPa(Settings.Units.MaterialStrength):0.00}";
		}

		/// <summary>
        /// Get/set steel for X output.
        /// </summary>
		private Steel OutputSteelX
		{
			get => new Steel(XYieldStress, XElasticModule);
			set
			{
				XYieldStress   = value?.YieldStress   ?? 500;
				XElasticModule = value?.ElasticModule ?? 210000;
			}
		}

		/// <summary>
        /// Get/set steel for Y output.
        /// </summary>
		private Steel OutputSteelY
		{
			get => new Steel(YYieldStress, YElasticModule);
			set
			{
				YYieldStress   = value?.YieldStress   ?? 500;
				YElasticModule = value?.ElasticModule ?? 210000;
			}
		}

        /// <summary>
        /// Get/set reinforcement for X output (steel is not set).
        /// </summary>
        private WebReinforcementDirection OutputReinforcementX
		{
			get => ReinforcementXChecked ? new WebReinforcementDirection(XBarDiameter, XSpacing, OutputSteelX, PnlWidth, 0) : null;
			set
			{
				XSpacing     = value?.BarSpacing   ?? 100;
				XBarDiameter = value?.BarDiameter  ?? 8;
			}
		}

		/// <summary>
        /// Get/set reinforcement for Y output (steel is not set).
        /// </summary>
		private WebReinforcementDirection OutputReinforcementY
		{
			get => ReinforcementYChecked ? new WebReinforcementDirection(YBarDiameter, YSpacing, OutputSteelY, PnlWidth, Constants.PiOver2) : null;
			set
			{
				YSpacing     = value?.BarSpacing   ?? 100;
				YBarDiameter = value?.BarDiameter  ?? 8;
			}
		}

		/// <summary>
        /// Get/set reinforcement for output (steel is not set).
        /// </summary>
		private WebReinforcement OutputReinforcement
		{
			get => !ReinforcementXChecked && !ReinforcementYChecked ? null : new WebReinforcement(OutputReinforcementX, OutputReinforcementY, PnlWidth);
			set
			{
				OutputReinforcementX = value?.DirectionX;
				OutputReinforcementY = value?.DirectionY;
			}
		}

		public PanelWindow(IEnumerable<Solid> panels)
        {
	        _panels = panels.ToArray();

			InitializeComponent();

            // Get stringer image
            CrossSection.Source = Icons.GetBitmap(Properties.Resources.panel_geometry);

            GetInitialGeometry();

			GetInitialReinforcement();

			DataContext = this;
		}

		/// <summary>
        /// Get the initial geometry data of panels.
        /// </summary>
        private void GetInitialGeometry()
		{
			SetGeometry = true;

			_savedWidths = PanelWidths?.OrderBy(g => g).ToArray();

            if (_savedWidths != null && _savedWidths.Any())
			{
				SavedGeometries.ItemsSource = SavedGeoOptions();
				SavedGeometries.SelectedIndex = 0;

				if (_panels.Length > 1)
	                PnlWidth  = _savedWidths[0];
            }
            else
            {
	            SavedGeometries.Disable();
	            PnlWidth  = 100;
            }

            if (_panels.Length > 1)
	            return;

			// Only 1 stringer, get it's geometry
			PnlWidth = GetWidth(_panels[0]);
		}

        /// <summary>
        /// Get the initial reinforcement data of panels.
        /// </summary>
        private void GetInitialReinforcement()
        {
	        SetReinforcement = true;

			_savedSteel = SavedSteel?.OrderBy(s => s.ElasticModule).ThenBy(s => s.YieldStress).ToArray();

			if (_savedSteel != null && _savedSteel.Any())
			{
				SavedSteelX.ItemsSource   = SavedSteelY.ItemsSource = SavedSteelOptions();
				SavedSteelX.SelectedIndex = SavedSteelY.SelectedIndex = 0;

				if (_panels.Length > 1)
					OutputSteelX = OutputSteelY = _savedSteel[0];
			}
			else
			{
				SavedSteelX.Disable();
				SavedSteelY.Disable();
				OutputSteelX = OutputSteelY = null;
            }

            _savedReinforcements = SavedPanelReinforcement?.OrderBy(r => r.BarSpacing).ThenBy(r => r.BarDiameter).ToArray();

			if (_savedReinforcements != null && _savedReinforcements.Any())
			{
				SavedReinforcementX.ItemsSource = SavedReinforcementY.ItemsSource = SavedRefOptions();
				SavedReinforcementX.SelectedIndex = SavedReinforcementY.SelectedIndex = 0;

				if (_panels.Length > 1)
					OutputReinforcementX = OutputReinforcementY = _savedReinforcements[0];
			}
			else
			{
				SavedReinforcementX.Disable();
				SavedReinforcementY.Disable();
				OutputReinforcementX = OutputReinforcementY = null;
			}

            if (_panels.Length == 1)
            {
	            var reinforcement = GetReinforcement(_panels[0]);

	            ReinforcementXChecked = !(reinforcement?.DirectionX is null);
	            ReinforcementYChecked = !(reinforcement?.DirectionY is null);

	            OutputReinforcement = reinforcement;
	            OutputSteelX = reinforcement?.DirectionX?.Steel;
	            OutputSteelY = reinforcement?.DirectionY?.Steel;
            }
            else
	            ReinforcementXChecked = ReinforcementYChecked = false;
        }

        /// <summary>
        /// Get saved geometry options as string collection.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> SavedGeoOptions() => _savedWidths.Select(geo => $"{geo.ConvertFromMillimeter(Settings.Units.Geometry):0.00}");
        
        /// <summary>
        /// Get saved reinforcement options as string collection.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> SavedRefOptions() => _savedReinforcements.Select(r => $"{(char)Character.Phi} {r.BarDiameter.ConvertFromMillimeter(Settings.Units.Reinforcement):0.0} at {r.BarSpacing.ConvertFromMillimeter(Settings.Units.Geometry):0.0}");
        
        /// <summary>
        /// Get saved steel options as string collection.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> SavedSteelOptions() => _savedSteel.Select(s => $"{s.YieldStress.ConvertFromMPa(Settings.Units.MaterialStrength):0.00} | {s.ElasticModule.ConvertFromMPa(Settings.Units.MaterialStrength):0.00}");

        /// <summary>
        /// Check if <paramref name="textBoxes"/> are filled and not zero.
        /// </summary>
        private bool CheckBoxes(IEnumerable<TextBox> textBoxes) => textBoxes.All(textBox => textBox.Text.ParsedAndNotZero(out _));

        private void DoubleValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			var regex = new Regex("[^0-9.]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		/// <summary>
        /// Save data in the stringer object.
        /// </summary>
		private void SaveGeometry()
		{
			// Convert values
			var width = PnlWidth;

			// Save on database
			Save(width);

			// Set to panels
			foreach (var pnl in _panels)
				SetWidth(pnl, width);
		}

		/// <summary>
        /// Save data in the stringer object.
        /// </summary>
		private void SaveReinforcement()
		{
			Save(OutputSteelX);
			Save(OutputSteelY);
			Save(OutputReinforcementX);
			Save(OutputReinforcementY);

            // Set to panels
            var reinforcement = OutputReinforcement;
            foreach (var pnl in _panels)
				SetReinforcement(pnl, reinforcement);
		}

        private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			// Check geometry
			if (SetGeometry)
			{
				if (!WidthFilled)
				{
					MessageBox.Show("Please set stringer geometry.", "Alert");
					return;
				}

				SaveGeometry();
			}
            // Check if reinforcement is set
            if (SetReinforcement)
			{
				if (ReinforcementXChecked && !ReinforcementXFilled)
				{
					MessageBox.Show("Please set all reinforcement properties or uncheck X reinforcement checkbox.", "Alert");
					return;
				}

				if (ReinforcementYChecked && !ReinforcementYFilled)
				{
					MessageBox.Show("Please set all reinforcement properties or uncheck Y reinforcement checkbox.", "Alert");
					return;
				}

				SaveReinforcement();
			}

			Close();
		}

		private void ButtonCancel_OnClick(object sender, RoutedEventArgs e) => Close();

		private void SavedGeometries_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= _savedWidths.Length || i < 0)
				return;

            // Update textboxes
            PnlWidth  = _savedWidths[i];
		}

		private void SavedSteelX_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= _savedSteel.Length || i < 0)
				return;

            // Update textboxes
            OutputSteelX = _savedSteel[i];
		}

		private void SavedSteelY_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= _savedSteel.Length || i < 0)
				return;

            // Update textboxes
            OutputSteelY = _savedSteel[i];
		}

		private void SavedReinforcementX_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox)sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= _savedReinforcements.Length || i < 0)
				return;

			// Update textboxes
			OutputReinforcementX = _savedReinforcements[i];
		}

		private void SavedReinforcementY_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox)sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= _savedReinforcements.Length || i < 0)
				return;

			// Update textboxes
			OutputReinforcementY = _savedReinforcements[i];
		}

		private void ReinforcementXCheck_OnCheck(object sender, RoutedEventArgs e) => ReinforcementXChecked = ((CheckBox) sender).IsChecked ?? false;

		private void ReinforcementYCheck_OnCheck(object sender, RoutedEventArgs e) => ReinforcementYChecked = ((CheckBox) sender).IsChecked ?? false;

		private void SetGeometry_OnCheck(object sender, RoutedEventArgs e) => SetGeometry = ((CheckBox)sender).IsChecked ?? false;

		private void SetReinforcement_OnCheck(object sender, RoutedEventArgs e) => SetReinforcement = ((CheckBox)sender).IsChecked ?? false;
    }
}
