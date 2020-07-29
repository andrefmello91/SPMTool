using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Material;
using Parameters = Material.Concrete.Parameters;
using ParameterModel = Material.Concrete.ParameterModel;
using Behavior = Material.Concrete.Behavior;
using BehaviorModel = Material.Concrete.BehaviorModel;
using SPMTool.AutoCAD;
using SPMTool.Core;
using UnitsNet;
using UnitsNet.Units;
using ComboBox = System.Windows.Controls.ComboBox;
using Force = UnitsNet.Force;
using TextBox = System.Windows.Controls.TextBox;
using Reinforcement = Material.Reinforcement.Uniaxial;

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
		private Reinforcement Reinforcement         { get; set; }
		private Steel         Steel                 { get; set; }
		public  string        GeometryUnit          { get; set; }
		public  string        ReinforcementUnit     { get; set; }
		public  string        StressUnit            { get; set; }
		public  string        ReinforcementAreaUnit { get; set; }

		public StringerWindow(Stringer stringer, Units units = null)
		{
			Stringer      = stringer;
			Reinforcement = Stringer.Reinforcement;
			Steel         = Reinforcement.Steel;

			// Read units
			Units = (units ?? Config.ReadUnits()) ?? new Units();
			GetUnits();

            InitializeComponent();

            GetInitialData();

            InitiateBoxes();

            DataContext = this;
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
			LengthBox.Text  = Units.ConvertFromMillimeter(Stringer.Length, Units.Geometry).ToString();
			WidthBox.Text   = Units.ConvertFromMillimeter(Stringer.Width,  Units.Geometry).ToString();
			HeigthBox.Text  = Units.ConvertFromMillimeter(Stringer.Height, Units.Geometry).ToString();
			NumBarsBox.Text = Reinforcement.NumberOfBars.ToString();
			BarDiamBox.Text = Units.ConvertFromMillimeter(Reinforcement.BarDiameter, Units.Reinforcement).ToString();
			AreaBox.Text    = UnitConverter.Convert(Reinforcement.Area, AreaUnit.SquareMillimeter, Units.ReinforcementArea).ToString();
			YieldBox.Text   = Units.ConvertFromMPa(Steel.YieldStress, Units.MaterialStrength).ToString();
			ModuleBox.Text  = Units.ConvertFromMPa(Steel.ElasticModule, Units.MaterialStrength).ToString();
		}

		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
