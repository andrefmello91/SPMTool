using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Extensions;
using Material.Reinforcement;
using Material.Reinforcement.Uniaxial;
using SPM.Elements.StringerProperties;
using SPMTool.Core;
using SPMTool.Core.Elements;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Core.Model;

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Lógica interna para StringerGeometryWindow.xaml
	/// </summary>
	public partial class StringerWindow : Window
	{
		#region Fields

		private readonly List<StringerObject> _stringers;
		private readonly LengthUnit _geometryUnit;
		private readonly LengthUnit _reinforcementUnit;
		private readonly PressureUnit _stressUnit;

		#endregion

		#region Properties

		/// <summary>
		///     Get/set bar diameter.
		/// </summary>
		private Length BarDiameter
		{
			get => Length.From(double.Parse(PhiBox.Text), _reinforcementUnit);
			set => PhiBox.Text = $"{value.Value:0.00}";
		}

		public string DiameterUnit => _reinforcementUnit.Abbrev();

		/// <summary>
		///     Get/set bar elastic module, in MPa.
		/// </summary>
		private Pressure ElasticModule
		{
			get => Pressure.From(double.Parse(EBox.Text), _stressUnit);
			set => EBox.Text = $"{value.Value:0.00}";
		}

		/// <summary>
		///     Get geometry <see cref="TextBox" />'s.
		/// </summary>
		private IEnumerable<TextBox> GeometryBoxes => new[] { WBox, HBox };

		/// <summary>
		///     Verify if geometry text boxes are filled.
		/// </summary>
		private bool GeometryFilled => CheckBoxes(GeometryBoxes);

		// Properties
		public string GeometryUnit => _geometryUnit.Abbrev();

		/// <summary>
		///     Get header text.
		/// </summary>
		public string HeaderText => _stringers.Count == 1
			? $"Stringer {_stringers[0].Number}"
			: $"{_stringers.Count} stringers selected";

		/// <summary>
		///     Get/set number of bars.
		/// </summary>
		private int NumOfBars
		{
			get => int.Parse(NumBox.Text);
			set => NumBox.Text = $"{value:0}";
		}

		/// <summary>
		///     Get cross section for output.
		/// </summary>
		private CrossSection OutputCrossSection
		{
			get => new CrossSection(StrWidth, StrHeight);
			set
			{
				StrWidth  = value.Width;
				StrHeight = value.Height;
			}
		}

		/// <summary>
		///     Get reinforcement for output.
		/// </summary>
		private UniaxialReinforcement? OutputReinforcement
		{
			get => ReinforcementChecked ? new UniaxialReinforcement(NumOfBars, BarDiameter, OutputSteel, Area.Zero) : null;
			set
			{
				NumOfBars   = value?.NumberOfBars ?? 2;
				BarDiameter = value?.BarDiameter  ?? Length.FromMillimeters(10);
			}
		}

		/// <summary>
		///     Get steel for output.
		/// </summary>
		private Steel? OutputSteel
		{
			get => new Steel(YieldStress, ElasticModule);
			set
			{
				YieldStress   = value?.YieldStress   ?? Pressure.FromMegapascals(500);
				ElasticModule = value?.ElasticModule ?? Pressure.FromMegapascals(210000);
			}
		}

		/// <summary>
		///     Get reinforcement <see cref="TextBox" />'s.
		/// </summary>
		private IEnumerable<TextBox> ReinforcementBoxes => new [] { NumBox, PhiBox, FBox, EBox };

		/// <summary>
		///     Gets and sets reinforcement checkbox state.
		/// </summary>
		private bool ReinforcementChecked
		{
			get => ReinforcementCheck.IsChecked ?? false;
			set
			{
				if (value)
				{
					ReinforcementBoxes.Enable();

					if (Steels.Any())
						SavedSteel.Enable();

					if (StringerReinforcements.Any())
						SavedReinforcement.Enable();
				}
				else
				{
					ReinforcementBoxes.Disable();

					if (Steels.Any())
						SavedSteel.Disable();

					if (StringerReinforcements.Any())
						SavedReinforcement.Disable();
				}

				ReinforcementCheck.IsChecked = value;
			}
		}

		/// <summary>
		///     Verify if reinforcement text boxes are filled.
		/// </summary>
		private bool ReinforcementFilled => CheckBoxes(ReinforcementBoxes);

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

		public string StressUnit => _stressUnit.Abbrev();

		/// <summary>
		///     Get/set height.
		/// </summary>
		private Length StrHeight
		{
			get => Length.From(double.Parse(HBox.Text), _geometryUnit);
			set => HBox.Text = $"{value.Value:0.00}";
		}

		/// <summary>
		///     Get/set width.
		/// </summary>
		private Length StrWidth
		{
			get => Length.From(double.Parse(WBox.Text), _geometryUnit);
			set => WBox.Text = $"{value.Value:0.00}";
		}

		/// <summary>
		///     Get/set bar yield stress.
		/// </summary>
		private Pressure YieldStress
		{
			get => Pressure.From(double.Parse(FBox.Text), _stressUnit);
			set => FBox.Text = $"{value.Value:0.00}";
		}

		#endregion

		#region Constructors

		public StringerWindow(IEnumerable<StringerObject> stringers)
		{
			_stringers = stringers.ToList();

			_stressUnit        = DataBase.Settings.Units.MaterialStrength;
			_geometryUnit      = DataBase.Settings.Units.Geometry;
			_reinforcementUnit = DataBase.Settings.Units.Reinforcement;

			InitializeComponent();

			// Get stringer image
			CrossSection.Source = Icons.GetBitmap(Properties.Resources.stringer_cross_section);

			GetInitialGeometry();

			GetInitialReinforcement();

			DataContext = this;
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Get saved geometry options as string collection.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<string> SavedGeoOptions() => StringerCrossSections.Select(geo => $"{geo.Width.Value:0.0} {(char) Character.Times} {geo.Height.Value:0.0}");

		/// <summary>
		///     Get saved reinforcement options as string collection.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<string> SavedRefOptions() => StringerReinforcements.Select(r => $"{r.NumberOfBars:0} {(char) Character.Phi} {r.BarDiameter.Value:0.00}");

		/// <summary>
		///     Get saved steel options as string collection.
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<string> SavedSteelOptions() => Steels.Select(s => $"{s.YieldStress.Value:0.00} | {s.ElasticModule.Value:0.00}");

		/// <summary>
		///     Get the initial geometry data of stringers.
		/// </summary>
		private void GetInitialGeometry()
		{
			SetGeometry = true;

			if (!StringerCrossSections.IsNullOrEmpty())
			{
				SavedGeometries.ItemsSource = SavedGeoOptions();
				SavedGeometries.SelectedIndex = 0;

				if (_stringers.Count > 1)
					OutputCrossSection  = StringerCrossSections[0];
			}
			else
			{
				SavedGeometries.Disable();
				StrWidth  = Length.FromMillimeters(100);
				StrHeight = Length.FromMillimeters(100);
			}

			if (_stringers.Count > 1)
				return;

			// Only 1 stringer, get it's geometry
			OutputCrossSection = _stringers[0].CrossSection;
		}

		/// <summary>
		///     Get the initial reinforcement data stringers.
		/// </summary>
		private void GetInitialReinforcement()
		{
			SetReinforcement = true;

			if (!Steels.IsNullOrEmpty())
			{
				SavedSteel.ItemsSource = SavedSteelOptions();
				SavedSteel.SelectedIndex = 0;

				if (_stringers.Count > 1)
					OutputSteel = Steels[0];
			}
			else
			{
				SavedSteel.Disable();
				OutputSteel = null;
			}

			if (!StringerReinforcements.IsNullOrEmpty())
			{
				SavedReinforcement.ItemsSource = SavedRefOptions();
				SavedReinforcement.SelectedIndex = 0;

				if (_stringers.Count > 1)
					OutputReinforcement = StringerReinforcements[0];
			}
			else
			{
				SavedReinforcement.Disable();
				OutputReinforcement = null;
			}

			if (_stringers.Count == 1)
			{
				var reinforcement = _stringers[0].Reinforcement;

				ReinforcementChecked = !(reinforcement is null);

				OutputReinforcement = reinforcement;
				OutputSteel = reinforcement?.Steel;
			}
			else
			{
				ReinforcementChecked = false;
			}
		}

		/// <summary>
		///     Check if <paramref name="textBoxes" /> are filled and not zero.
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
		///     Save data in the stringer object.
		/// </summary>
		private void SaveGeometry()
		{
			var crossSection = OutputCrossSection;

			StringerCrossSections.Add(crossSection, false);

			// Set to stringers
			foreach (var str in _stringers)
				str.CrossSection = crossSection;
		}

		/// <summary>
		///     Save data in the stringer object.
		/// </summary>
		private void SaveReinforcement()
		{
			var reinforcement = OutputReinforcement;

			StringerReinforcements.Add(reinforcement, false);

			// Set to stringers
			foreach (var str in _stringers)
				str.Reinforcement = reinforcement;
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

			if (i >= StringerCrossSections.Count || i < 0)
				return;

			// Update textboxes
			OutputCrossSection  = StringerCrossSections[i];
		}

		private void SavedSteel_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= Steels.Count || i < 0)
				return;

			// Update textboxes
			OutputSteel = Steels[i];
		}

		private void SavedReinforcement_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = (ComboBox) sender;

			// Get index
			var i = box.SelectedIndex;

			if (i >= StringerReinforcements.Count || i < 0)
				return;

			// Update textboxes
			OutputReinforcement = StringerReinforcements[i];
		}

		private void ReinforcementCheck_OnCheck(object sender, RoutedEventArgs e) => ReinforcementChecked = ((CheckBox) sender).IsChecked ?? false;

		private void SetGeometry_OnCheck(object sender, RoutedEventArgs e) => SetGeometry = ((CheckBox) sender).IsChecked ?? false;

		private void SetReinforcement_OnCheck(object sender, RoutedEventArgs e) => SetReinforcement = ((CheckBox) sender).IsChecked ?? false;

		#endregion
	}
}