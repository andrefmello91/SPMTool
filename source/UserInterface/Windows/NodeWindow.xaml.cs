using System;
using System.Windows;
using OnPlaneComponents;
using SPM.Elements;
using SPMTool.ApplicationSettings;
using SPMTool.Database;
using UnitsNet;

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para NodeWindow.xaml
    /// </summary>
    public partial class NodeWindow : Window
	{
		// Properties
		private Units        _units;
		private Node         _node;
		private PlaneForce        _planeForce;
		private PlaneDisplacement _displacement;


		public NodeWindow(Node node)
		{
			_node = node;

			// Read units
			_units = Settings.Units;

			// Get forces and displacements
			_planeForce = node.PlaneForce.Copy();
			_planeForce.ChangeUnit(_units.AppliedForces);

			_displacement = node.Displacement.Copy();
			_displacement.ChangeUnit(_units.Displacements);

            InitializeComponent();

			// Initiate text boxes
            InitiateBlocks();

            DataContext = this;
		}

		// Get combo boxes items
		private void InitiateBlocks()
		{
			NodeNumberBlock.Text = $"Node {_node.Number}";

			NodePositionBlock.Text = $"Position: ({_node.Position.X:0.00}, {_node.Position.Y:0.00})";

			//FxBlock.Text = "Fx = " + Fx;

			//FyBlock.Text = "Fy = " + Fy;

			//if (_node.DisplacementSet)
			//{
			//	UxBlock.Text = "ux = " + Ux;
			//	UyBlock.Text = "uy = " + Uy;
			//}
			//else
			//{
			//	UxBlock.Text = "ux = NOT CALCULATED";
			//	UyBlock.Text = "uy = NOT CALCULATED";
			//}
		}

		private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
