using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
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

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para NodeWindow.xaml
    /// </summary>
    public partial class NodeWindow : Window
	{
		// Properties
		private Units  Units { get; }
		public  Node   Node  { get; }
		private Force  Fx    { get; }
		private Force  Fy    { get; }
		private Length Ux    { get; }
		private Length Uy    { get; }


		public NodeWindow(Node node, Units units = null)
		{
			Node = node;

			// Read units
			Units = (units ?? Config.ReadUnits()) ?? new Units();

			// Get forces and displacements
			double
				fx = 0,
				fy = 0;

			if (Node.Forces.X != null)
				fx = Node.Forces.X.Value;

			if (Node.Forces.X != null)
				fy = Node.Forces.Y.Value;

			Fx = Force.FromNewtons(fx).ToUnit(Units.AppliedForces);
			Fy = Force.FromNewtons(fy).ToUnit(Units.AppliedForces);
			Ux = Length.FromMillimeters(Node.Displacement.X).ToUnit(Units.Displacements);
			Uy = Length.FromMillimeters(Node.Displacement.Y).ToUnit(Units.Displacements);

            InitializeComponent();

			// Initiate text boxes
            InitiateBlocks();

            DataContext = this;
		}

		// Get combo boxes items
		private void InitiateBlocks()
		{
			NodeNumberBlock.Text = "Node " + Node.Number;

			NodePositionBlock.Text = "Position: (" + Math.Round(Node.Position.X, 2) + ", " + Math.Round(Node.Position.Y, 2) + ")";

			FxBlock.Text = "Fx = " + Fx;

			FyBlock.Text = "Fy = " + Fy;

			if (Node.DisplacementSet)
			{
				UxBlock.Text = "ux = " + Ux;
				UyBlock.Text = "uy = " + Uy;
			}
			else
			{
				UxBlock.Text = "ux = NOT CALCULATED";
				UyBlock.Text = "uy = NOT CALCULATED";
			}
		}

		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
