using System;
using System.Windows;
using SPMTool.Model.Conditions;
using SPMTool.Database;
using SPMTool.Elements;
using UnitsNet;
using Force = UnitsNet.Force;

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


		public NodeWindow(Node node)
		{
			Node = node;

			// Read units
			Units = Database.Units;

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
