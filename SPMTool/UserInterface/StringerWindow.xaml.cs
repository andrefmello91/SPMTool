using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions.Number;
using Material;
using Material.Reinforcement;
using SPM.Elements;
using SPM.Elements.StringerProperties;
using SPMTool.Database.Model.Conditions;
using SPMTool.Database;
using UnitsNet;
using UnitsNet.Units;
using MessageBox = System.Windows.MessageBox;
using static SPMTool.Auxiliary;

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para NodeWindow.xaml
    /// </summary>
    public partial class StringerWindow : Window
    {
	    private StringerGeometry _geometry;
	    private UniaxialReinforcement _reinforcement;
	    private readonly ObjectId _objectId;
	    private readonly Units _units;

        // Properties
        public string GeometryUnit => Length.GetAbbreviation(_units.Geometry);

        public string ReinforcementUnit => Length.GetAbbreviation(_units.Reinforcement);

        public string StressUnit => Pressure.GetAbbreviation(_units.MaterialStrength);

        public string ReinforcementAreaUnit => Area.GetAbbreviation(_units.ReinforcementArea);

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

        public StringerWindow(Stringer stringer)
        {
	        _geometry = stringer.Geometry;
	        _reinforcement    = stringer.Reinforcement;
	        _objectId         = stringer.ObjectId;

            // Read units
            _units = DataBase.Units;

            InitializeComponent();

            // Get stringer image
            StringerImage.Source = Database.Model.Conditions.UserInterface.getBitmap(Properties.Resources.stringer_cross_section);

            GetInitialData(stringer);

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
			        if (!ParsedAndNotZero(textBox.Text))
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
					if (!ParsedAndNotZero(textBox.Text))
						return false;
				}

                return true;
			}
		}

        private void GetInitialData(Stringer stringer)
		{
			StringerNumberBlock.Text = $"Stringer {stringer.Number}";
			StringerGripsBlock.Text  = $"Grips: {stringer.Grips[0]} - {stringer.Grips[1]} - {stringer.Grips[2]}";
		}

		//private void GetUnits()
		//{
  //          GeometryUnit          = Length.GetAbbreviation(_units.Geometry);
		//	ReinforcementUnit     = Length.GetAbbreviation(_units.Reinforcement);
		//	StressUnit            = Pressure.GetAbbreviation(_units.MaterialStrength);
		//	ReinforcementAreaUnit = Area.GetAbbreviation(_units.ReinforcementArea);
		//}

		private void InitiateBoxes()
		{
			LengthBox.Text  = $"{_geometry.Length.ConvertFromMillimeter(_units.Geometry):0.00}";
			WidthBox.Text   = $"{_geometry.Width.ConvertFromMillimeter(_units.Geometry):0.00}";
			HeigthBox.Text  = $"{_geometry.Height.ConvertFromMillimeter(_units.Geometry):0.00}";

			// Get checkbox state
			if (_reinforcement is null || _reinforcement.NumberOfBars == 0 || _reinforcement.BarDiameter.ApproxZero())
				ReinforcementChecked = false;

			else
			{
				ReinforcementChecked = true;
				NumBarsBox.Text = $"{_reinforcement.NumberOfBars}";
				BarDiamBox.Text = $"{_units.ConvertFromMillimeter(_reinforcement.BarDiameter, _units.Reinforcement):0.00}";

				AreaBox.Text =
					_reinforcement.Area > 0 ?
						$"{_reinforcement.Area.ConvertFromSquareMillimeter(_units.ReinforcementArea):0.00}" : "0.00";

				YieldBox.Text  = $"{_reinforcement.Steel.YieldStress.ConvertFromMPa(_units.MaterialStrength):0.00}";
				ModuleBox.Text = $"{_reinforcement.Steel.ElasticModule.ConvertFromMPa(_units.MaterialStrength):0.00}";
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
			_geometry = new StringerGeometry(Point3d.Origin, Point3d.Origin, width, height, _units.Geometry);

			if (!ReinforcementChecked || barDiameter.ApproxZero() || numOfBars == 0)
				return;

			Steel steel = null;

			if (fy > 0 || Es > 0)
				steel = new Steel(Pressure.From(fy, _units.MaterialStrength), Pressure.From(Es, _units.MaterialStrength));

			_reinforcement = new UniaxialReinforcement(numOfBars, Length.From(barDiameter, _units.Reinforcement), steel, Area.Zero);

			Stringers.SaveStringerData(_objectId, _geometry, _reinforcement);
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
