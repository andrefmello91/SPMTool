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
using Material.Reinforcement.Uniaxial;
using MathNet.Numerics;
using SPM.Elements.StringerProperties;
using SPMTool.ApplicationSettings;
using SPMTool.Database;
using SPMTool.Database.Elements;
using SPMTool.Database.Materials;
using SPMTool.Enums;
using SPMTool.Extensions;
using MessageBox = System.Windows.MessageBox;
using static SPMTool.Database.Elements.Stringers;
using static SPMTool.Database.Elements.ElementData;
using static SPMTool.Database.Materials.ReinforcementData;
using Window = System.Windows.Window;

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para StringerGeometryWindow.xaml
    /// </summary>
    public partial class StringerWindow : Window
    {
	    private readonly Line[] _stringers;
	    private StringerGeometry[] _savedGeometries;
	    private UniaxialReinforcement[] _savedReinforcements;
	    private Steel[] _savedSteel;

        // Properties
        public string GeometryUnit => Settings.Units.Geometry.Abbrev();

        public string DiameterUnit => Settings.Units.Reinforcement.Abbrev();

        public string StressUnit => Settings.Units.MaterialStrength.Abbrev();

        /// <summary>
        /// Get header text.
        /// </summary>
        public string HeaderText => _stringers.Length == 1
	        ? $"Stringer {_stringers[0].ReadXData()[(int) StringerIndex.Number].ToInt()}"
	        : $"{_stringers.Length} stringers selected";

        /// <summary>
        /// Gets and sets reinforcement checkbox state.
        /// </summary>
        private bool ReinforcementChecked
        {
	        get => ReinforcementCheck.IsChecked ?? false;
	        set
	        {
		        if (value)
		        {
			        ReinforcementBoxes.Enable();

					if (_savedSteel?.Any() ?? false)
						SavedSteel.Enable();

					if (_savedReinforcements?.Any() ?? false)
						SavedReinforcement.Enable();
		        }
		        else
		        {
			        ReinforcementBoxes.Disable();

			        if (_savedSteel?.Any() ?? false)
				        SavedSteel.Disable();

			        if (_savedReinforcements?.Any() ?? false)
				        SavedReinforcement.Disable();
		        }

                ReinforcementCheck.IsChecked = value;
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
        /// Get geometry <see cref="TextBox"/>'s.
        /// </summary>
        private IEnumerable<TextBox> GeometryBoxes => new[] { WBox, HBox };

        /// <summary>
		/// Get reinforcement <see cref="TextBox"/>'s.
		/// </summary>
		private IEnumerable<TextBox> ReinforcementBoxes => new [] { NumBox, PhiBox, FBox, EBox };

        /// <summary>
        /// Verify if geometry text boxes are filled.
        /// </summary>
        private bool GeometryFilled => CheckBoxes(GeometryBoxes);

        /// <summary>
        /// Verify if reinforcement text boxes are filled.
        /// </summary>
        private bool ReinforcementFilled => CheckBoxes(ReinforcementBoxes);

        /// <summary>
        /// Get/set width, in mm.
        /// </summary>
		private double StrWidth
		{
			get => double.Parse(WBox.Text).Convert(Settings.Units.Geometry);
			set => WBox.Text = $"{value.ConvertFromMillimeter(Settings.Units.Geometry):0.00}";
		}

		/// <summary>
        /// Get/set height, in mm.
        /// </summary>
		private double StrHeight
		{
			get => double.Parse(HBox.Text).Convert(Settings.Units.Geometry);
			set => HBox.Text = $"{value.ConvertFromMillimeter(Settings.Units.Geometry):0.00}";
		}

		/// <summary>
        /// Get/set number of bars.
        /// </summary>
		private int NumOfBars
		{
			get => int.Parse(NumBox.Text);
			set => NumBox.Text = $"{value:0}";
		}

		/// <summary>
		/// Get/set bar diameter, in mm.
		/// </summary>
		private double BarDiameter
		{
			get => double.Parse(PhiBox.Text).Convert(Settings.Units.Reinforcement);
			set => PhiBox.Text = $"{value.ConvertFromMillimeter(Settings.Units.Reinforcement):0.00}";
		}

		/// <summary>
		/// Get/set bar yield stress, in MPa.
		/// </summary>
		private double YieldStress
		{
			get => double.Parse(FBox.Text).Convert(Settings.Units.MaterialStrength);
			set => FBox.Text = $"{value.ConvertFromMPa(Settings.Units.MaterialStrength):0.00}";
		}

		/// <summary>
		/// Get/set bar elastic module, in MPa.
		/// </summary>
		private double ElasticModule
		{
			get => double.Parse(EBox.Text).Convert(Settings.Units.MaterialStrength);
			set => EBox.Text = $"{value.ConvertFromMPa(Settings.Units.MaterialStrength):0.00}";
		}

		/// <summary>
        /// Get geometry for output.
        /// </summary>
		private StringerGeometry OutputGeometry
		{
			get => new StringerGeometry(Point3d.Origin, Point3d.Origin, StrWidth, StrHeight);
			set
			{
				StrWidth  = value.Width;
				StrHeight = value.Height;
			}
		}

		/// <summary>
        /// Get steel for output.
        /// </summary>
		private Steel OutputSteel
		{
			get => new Steel(YieldStress, ElasticModule);
			set
			{
				YieldStress   = value?.YieldStress   ?? 500;
				ElasticModule = value?.ElasticModule ?? 210000;
			}
		}

		/// <summary>
        /// Get reinforcement for output.
        /// </summary>
		private UniaxialReinforcement OutputReinforcement
		{
			get => ReinforcementChecked ? new UniaxialReinforcement(NumOfBars, BarDiameter, OutputSteel) : null;
			set
			{
				NumOfBars   = value?.NumberOfBars ?? 2;
				BarDiameter = value?.BarDiameter  ?? 10;
			}
		}

		public StringerWindow(IEnumerable<Line> stringers)
        {
	        _stringers = stringers.ToArray();

			InitializeComponent();

            // Get stringer image
            CrossSection.Source = Icons.GetBitmap(Properties.Resources.stringer_cross_section);

            GetInitialGeometry();

			GetInitialReinforcement();

			DataContext = this;
		}

		/// <summary>
        /// Get the initial geometry data of stringers.
        /// </summary>
        private void GetInitialGeometry()
		{
			SetGeometry = true;

			_savedGeometries = StringerGeometries?.OrderBy(g => g.Height).ThenBy(g => g.Width).ToArray();

            if (_savedGeometries != null && _savedGeometries.Any())
			{
				SavedGeometries.ItemsSource = SavedGeoOptions();
				SavedGeometries.SelectedIndex = 0;

				if (_stringers.Length > 1)
	                OutputGeometry  = _savedGeometries[0];
            }
            else
            {
	            SavedGeometries.Disable();
	            StrWidth  = 100;
	            StrHeight = 100;
            }

            if (_stringers.Length > 1)
	            return;

			// Only 1 stringer, get it's geometry
			OutputGeometry = StringerObject.GetGeometry(_stringers[0]);
		}

        /// <summary>
        /// Get the initial reinforcement data stringers.
        /// </summary>
        private void GetInitialReinforcement()
        {
	        SetReinforcement = true;

			_savedSteel = ReinforcementData.SavedSteel?.OrderBy(s => s.ElasticModule).ThenBy(s => s.YieldStress).ToArray();

			if (_savedSteel != null && _savedSteel.Any())
			{
				SavedSteel.ItemsSource = SavedSteelOptions();
				SavedSteel.SelectedIndex = 0;

				if (_stringers.Length > 1)
					OutputSteel = _savedSteel[0];
			}
			else
			{
				SavedSteel.Disable();
				OutputSteel = null;
			}

            _savedReinforcements = SavedStringerReinforcement?.OrderBy(r => r.BarDiameter).ThenBy(r => r.NumberOfBars).ToArray();

			if (_savedReinforcements != null && _savedReinforcements.Any())
			{
				SavedReinforcement.ItemsSource = SavedRefOptions();
				SavedReinforcement.SelectedIndex = 0;

				if (_stringers.Length > 1)
					OutputReinforcement = _savedReinforcements[0];
			}
			else
			{
				SavedReinforcement.Disable();
				OutputReinforcement = null;
			}
			
            if (_stringers.Length == 1)
            {
	            var reinforcement = StringerObject.GetReinforcement(_stringers[0]);

	            ReinforcementChecked = !(reinforcement is null);

	            OutputReinforcement = reinforcement;
	            OutputSteel = reinforcement?.Steel;
            }
            else
	            ReinforcementChecked = false;
        }

        /// <summary>
        /// Get saved geometry options as string collection.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> SavedGeoOptions() => _savedGeometries.Select(geo => $"{geo.Width.ConvertFromMillimeter(Settings.Units.Geometry):0.0} {(char) Character.Times} {geo.Height.ConvertFromMillimeter(Settings.Units.Geometry):0.0}");
        
        /// <summary>
        /// Get saved reinforcement options as string collection.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> SavedRefOptions() => _savedReinforcements.Select(r => $"{r.NumberOfBars:0} {(char) Character.Phi} {r.BarDiameter.ConvertFromMillimeter(Settings.Units.Reinforcement):0.00}");
        
        /// <summary>
        /// Get saved steel options as string collection.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> SavedSteelOptions() => _savedSteel.Select(s => $"{s.YieldStress.ConvertFromMPa(Settings.Units.MaterialStrength):0.00} | {s.ElasticModule.ConvertFromMPa(Settings.Units.MaterialStrength):0.00}");

        /// <summary>
        /// Check if <paramref name="textBoxes"/> are filled and not zero.
        /// </summary>
        private bool CheckBoxes(IEnumerable<TextBox> textBoxes) => textBoxes.All(textBox => textBox.Text.ParsedAndNotZero(out _));

        private void IntValidationTextBox(object sender, TextCompositionEventArgs e)
        {
	        var regex = new Regex("[^0-9]+");
	        e.Handled = regex.IsMatch(e.Text);
        }

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
			var geometry = OutputGeometry;

			// Save on database
			Save(geometry);

			// Save width for panels
			Save(StrWidth);

			// Set to stringers
			foreach (var str in _stringers)
				StringerObject.SetGeometry(str, geometry);
		}

		/// <summary>
        /// Save data in the stringer object.
        /// </summary>
		private void SaveReinforcement()
		{
			var reinforcement = OutputReinforcement;

			Save(reinforcement?.Steel);
			Save(reinforcement);

			// Set to stringers
            foreach (var str in _stringers)
				StringerObject.SetReinforcement(str, reinforcement);
		}

        private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			// Check geometry
			if (SetGeometry)
			{
				if (!GeometryFilled)
				{
					MessageBox.Show("Please set stringer geometry.", "Alert");
					return;
				}

				SaveGeometry();
			}
            // Check if reinforcement is set
            if (SetReinforcement)
			{
				if (ReinforcementChecked && !ReinforcementFilled)
				{
					MessageBox.Show("Please set all reinforcement properties or uncheck reinforcement checkbox.", "Alert");
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

			if (i >= _savedGeometries.Length || i < 0)
				return;

            // Update textboxes
            OutputGeometry  = _savedGeometries[i];
		}

		private void SavedSteel_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= _savedSteel.Length || i < 0)
				return;

            // Update textboxes
            OutputSteel = _savedSteel[i];
		}

		private void SavedReinforcement_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox)sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= _savedReinforcements.Length || i < 0)
				return;

			// Update textboxes
			OutputReinforcement = _savedReinforcements[i];
		}

		private void ReinforcementCheck_OnCheck(object sender, RoutedEventArgs e) => ReinforcementChecked = ((CheckBox) sender).IsChecked ?? false;

		private void SetGeometry_OnCheck(object sender, RoutedEventArgs e) => SetGeometry = ((CheckBox)sender).IsChecked ?? false;

		private void SetReinforcement_OnCheck(object sender, RoutedEventArgs e) => SetReinforcement = ((CheckBox)sender).IsChecked ?? false;
    }
}
