using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using andrefmello91.Extensions;
using SPMTool.Core;
using UnitsNet;
using UnitsNet.Units;
using static SPMTool.Application.Settings;

namespace SPMTool.Application.UserInterface
{
	/// <summary>
	///     Lógica interna para UnitsConfig.xaml
	/// </summary>
	public partial class UnitsConfig : Window
	{
		#region Properties

		/// <summary>
		///     Verify if factor box is filled.
		/// </summary>
		private bool FactorBoxFilled => FactorBox.Text.ParsedAndNotZero(out _);

		/// <summary>
		///     Get/set units.
		/// </summary>
		private Units Units
		{
			get => new Units
			{
				Geometry              = UnitParser.Default.Parse<LengthUnit>  ((string) GeometryBox.SelectedItem),
				Reinforcement         = UnitParser.Default.Parse<LengthUnit>  ((string) ReinforcementBox.SelectedItem),
				Displacements         = UnitParser.Default.Parse<LengthUnit>  ((string) DisplacementsBox.SelectedItem),
				CrackOpenings         = UnitParser.Default.Parse<LengthUnit>  ((string) CracksBox.SelectedItem),
				AppliedForces         = UnitParser.Default.Parse<ForceUnit>   ((string) AppliedForcesBox.SelectedItem),
				StringerForces        = UnitParser.Default.Parse<ForceUnit>   ((string) StringerForcesBox.SelectedItem),
				PanelStresses         = UnitParser.Default.Parse<PressureUnit>((string) PanelStressesBox.SelectedItem),
				MaterialStrength      = UnitParser.Default.Parse<PressureUnit>((string) MaterialBox.SelectedItem),
				DisplacementMagnifier = int.Parse(FactorBox.Text)
			};

			set
			{
				GeometryBox.SelectedItem       = value.Geometry.Abbrev();
				ReinforcementBox.SelectedItem  = value.Reinforcement.Abbrev();
				DisplacementsBox.SelectedItem  = value.Displacements.Abbrev();
				CracksBox.SelectedItem         = value.CrackOpenings.Abbrev();
				AppliedForcesBox.SelectedItem  = value.AppliedForces.Abbrev();
				StringerForcesBox.SelectedItem = value.StringerForces.Abbrev();
				PanelStressesBox.SelectedItem  = value.PanelStresses.Abbrev();
				MaterialBox.SelectedItem       = value.MaterialStrength.Abbrev();
				FactorBox.Text                 = $"{value.DisplacementMagnifier:0}";
			}
		}

		#endregion

		#region Constructors

		public UnitsConfig()
		{
			InitializeComponent();

			// Read units
			Units = DataBase.Settings.Units;

			// Get sources
			GetSources();

			DataContext = this;
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Get sources of combo boxes.
		/// </summary>
		private void GetSources()
		{
			GeometryBox.ItemsSource      = ReinforcementBox.ItemsSource  = DisplacementsBox.ItemsSource = CracksBox.ItemsSource = DimensionUnits;
			AppliedForcesBox.ItemsSource = StringerForcesBox.ItemsSource = ForceUnits;
			MaterialBox.ItemsSource      = PanelStressesBox.ItemsSource  = StressUnits;
		}

		private void IntValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			var regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		/// <summary>
		///     Close window if cancel button is clicked.
		/// </summary>
		private void ButtonCancel_OnClick(object sender, RoutedEventArgs e) => Close();

		/// <summary>
		///     Save units if OK button is clicked.
		/// </summary>
		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			if (!FactorBoxFilled)
			{
				MessageBox.Show("Please set valid displacement magnifier factor.");
				return;
			}

			// Set displacement factor
			Units.DisplacementMagnifier = int.Parse(FactorBox.Text);

			// Save units on database
			DataBase.Settings.Units = Units;

			Close();
		}

		/// <summary>
		///     Set default units.
		/// </summary>
		private void ButtonDefault_OnClick(object sender, RoutedEventArgs e) => Units = Units.Default;

		#endregion
	}
}