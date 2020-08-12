using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Material;
using Material.Reinforcement;
using SPMTool.AutoCAD;
using SPMTool.Elements;
using UnitsNet;
using UnitsNet.Units;
using MessageBox = System.Windows.MessageBox;
using Reinforcement = Material.Reinforcement.UniaxialReinforcement;

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para NodeWindow.xaml
    /// </summary>
    public partial class StringerWindow : Window
	{
		// Properties
		private Units         Units                 { get; }
		private Stringer      Stringer              { get; }
		public  string        GeometryUnit          { get; set; }
		public  string        ReinforcementUnit     { get; set; }
		public  string        StressUnit            { get; set; }
		public  string        ReinforcementAreaUnit { get; set; }

		/// <summary>
        /// Gets and sets reinforcement checkbox state.
        /// </summary>
		private bool ReinforcementChecked
		{
			get => ReinforcementCheck.IsChecked.Value;
			set
			{
				if (value)
					EnableReinforcementBoxes();
				else
					DisableReinforcementBoxes();

				ReinforcementCheck.IsChecked = value;
			}
		}

		private Reinforcement Reinforcement => Stringer.Reinforcement;
		private Steel         Steel         => Reinforcement?.Steel;

        public StringerWindow(Stringer stringer, Units units = null)
		{
			Stringer      = stringer;

			// Read units
			Units = (units ?? Config.ReadUnits()) ?? new Units();
			GetUnits();

            InitializeComponent();

            // Get stringer image
            StringerImage.Source = AutoCAD.UserInterface.getBitmap(Properties.Resources.stringer_cross_section);

            GetInitialData();

            InitiateBoxes();

            DataContext = this;
		}

        /// <summary>
        /// Verify if geometry text boxes are filled.
        /// </summary>
        private bool GeometrySet
        {
	        get
	        {
		        var textBoxes = new[] { WidthBox, HeigthBox };
		        foreach (var textBox in textBoxes)
		        {
			        if (!GlobalAuxiliary.ParsedAndNotZero(textBox.Text))
				        return false;
		        }

                return true;
	        }
        }

		/// <summary>
        /// Verify if reinforcement text boxes are filled.
        /// </summary>
        private bool ReinforcementSet
        {
			get
			{
				var textBoxes = new[] { NumBarsBox, BarDiamBox, YieldBox, ModuleBox };
				foreach (var textBox in textBoxes)
				{
					if (!GlobalAuxiliary.ParsedAndNotZero(textBox.Text))
						return false;
				}

                return true;
			}
		}

        private void GetInitialData()
		{
			StringerNumberBlock.Text = "Stringer " + Stringer.Number;
			StringerGripsBlock.Text  = "Grips: " + Stringer.Grips[0] + " - " + Stringer.Grips[1] + " - " + Stringer.Grips[2];
		}

		private void GetUnits()
		{
			GeometryUnit          = Length.GetAbbreviation(Units.Geometry);
			ReinforcementUnit     = Length.GetAbbreviation(Units.Reinforcement);
			StressUnit            = Pressure.GetAbbreviation(Units.MaterialStrength);
			ReinforcementAreaUnit = Area.GetAbbreviation(Units.ReinforcementArea);
		}

		private void InitiateBoxes()
		{
			LengthBox.Text  = $"{Units.ConvertFromMillimeter(Stringer.Length, Units.Geometry):0.00}";
			WidthBox.Text   = $"{Units.ConvertFromMillimeter(Stringer.Width,  Units.Geometry):0.00}";
			HeigthBox.Text  = $"{Units.ConvertFromMillimeter(Stringer.Height, Units.Geometry):0.00}";

			// Get checkbox state
			if (Reinforcement is null || Reinforcement.NumberOfBars == 0 || Reinforcement.BarDiameter == 0)
				ReinforcementChecked = false;

			else
			{
				ReinforcementChecked = true;
				NumBarsBox.Text = Reinforcement.NumberOfBars.ToString();
				BarDiamBox.Text = $"{Units.ConvertFromMillimeter(Reinforcement.BarDiameter, Units.Reinforcement):0.00}";

				AreaBox.Text =
					Reinforcement.Area > 0 ?
						$"{UnitConverter.Convert(Reinforcement.Area, AreaUnit.SquareMillimeter, Units.ReinforcementArea):0.00}" : "0.00";

				YieldBox.Text  = $"{Units.ConvertFromMPa(Steel.YieldStress, Units.MaterialStrength):0.00}";
				ModuleBox.Text = $"{Units.ConvertFromMPa(Steel.ElasticModule, Units.MaterialStrength):0.00}";
			}
		}

		/// <summary>
		/// Calculated reinforcement area.
		/// </summary>
		private double ReinforcementArea(int numberOfBars, double barDiameter)
		{
			if (numberOfBars > 0 && barDiameter > 0)
				return
					0.25 * numberOfBars * Constants.Pi * barDiameter * barDiameter;

			return 0;
		}

		private void EnableReinforcementBoxes()
		{
			NumBarsBox.IsEnabled = true;
			BarDiamBox.IsEnabled = true;
			YieldBox.IsEnabled   = true;
			ModuleBox.IsEnabled  = true;
		}

		private void DisableReinforcementBoxes()
		{
			NumBarsBox.IsEnabled = false;
			BarDiamBox.IsEnabled = false;
			YieldBox.IsEnabled   = false;
			ModuleBox.IsEnabled  = false;
		}

        private void DoubleValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9.]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		private void IntValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		private void SaveData()
		{
			// Get values
			int.TryParse(NumBarsBox.Text, out var numOfBars);
			double.TryParse(WidthBox.Text, out var width);
			double.TryParse(HeigthBox.Text, out var height);
			double.TryParse(BarDiamBox.Text, out var barDiameter);
			double.TryParse(YieldBox.Text, out var fy);
			double.TryParse(ModuleBox.Text, out var Es);
			
			// Convert values
			if (Units.Geometry != LengthUnit.Millimeter)
			{
				Stringer.Width  = Units.ConvertToMillimeter(width,  Units.Geometry);
				Stringer.Height = Units.ConvertToMillimeter(height, Units.Geometry);
			}

			if (ReinforcementChecked && barDiameter > 0 && numOfBars > 0)
			{
				if (Units.Reinforcement != LengthUnit.Millimeter)
					barDiameter = Units.ConvertToMillimeter(barDiameter, Units.Reinforcement);

				if (Units.MaterialStrength != PressureUnit.Megapascal)
				{
					fy = Units.ConvertToMPa(fy, Units.MaterialStrength);
					Es = Units.ConvertToMPa(Es, Units.MaterialStrength);
				}

				Steel steel = null;

				if (fy > 0 || Es > 0)
					steel = new Steel(fy, Es);

				Stringer.Reinforcement = new Reinforcement(numOfBars, barDiameter, 0, steel);
			}
			else
				Stringer.Reinforcement = null;

			Auxiliary.SaveStringerData(Stringer);
        }

        private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			// Check geometry
			if (!GeometrySet)
			{
				MessageBox.Show("Please set stringer geometry.", "Alert");
			}

            // Check if reinforcement is set
            else if (ReinforcementChecked && !ReinforcementSet)
			{
				MessageBox.Show("Please set all reinforcement properties or uncheck reinforcement checkbox.", "Alert");
			}

			else
			{
				SaveData();
				Close();
			}
		}

		private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
		{

            Close();
		}

		private void Reinforcement_OnTextChanged(object sender, TextChangedEventArgs e)
		{
            if (NumBarsBox.Text != string.Empty && BarDiamBox.Text != string.Empty)
			{
				// Get values
				int    numOfBars   = int.Parse(NumBarsBox.Text);
				double barDiameter = double.Parse(BarDiamBox.Text);

				// Set area value
				AreaBox.Text = $"{ReinforcementArea(numOfBars, barDiameter):0.00}";
			}
			else
				AreaBox.Text = "0.00";
		}

		private void ReinforcementCheck_OnChecked(object sender, RoutedEventArgs e) => ReinforcementChecked = true;
		
		private void ReinforcementCheck_OnUnchecked(object sender, RoutedEventArgs e) => ReinforcementChecked = false;
	}
}
