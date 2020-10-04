using System;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Data.Text;
using SPMTool.Database.Model.Conditions;
using SPMTool.Database;
using UnitsNet;
using UnitsNet.Units;

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para GraphWindow.xaml
    /// </summary>
    public partial class GraphWindow : Window
    {
        // Properties
		public SeriesCollection LoadDisplacement  { get; set; }
		private LengthUnit      DisplacementUnit  { get; }
		public double[]         Displacements     { get; }
		public double[]         LoadFactors       { get; }
		public string           DisplacementTitle { get; }

        public GraphWindow(double[] displacements = null, double[] loadFactors = null, LengthUnit displacementUnit = LengthUnit.Millimeter)
        {
            InitializeComponent();

            Displacements    = displacements;
            LoadFactors      = loadFactors;
            DisplacementUnit = displacementUnit;
            DisplacementTitle = "Displacement (" + Length.GetAbbreviation(DisplacementUnit) + ")";

            var values = GetValues();

			// Initiate series
			LoadDisplacement = new SeriesCollection
			{
				new LineSeries()
				{
					Title           = "Load Factor x Displacement", 
					Values          = values, 
					PointGeometry   = null,
					StrokeThickness = 3,
					Stroke          = Brushes.LightSkyBlue,
					Fill            = Brushes.Transparent,
					DataLabels      = false,
					LabelPoint      = Label
				}
			};

			// Set initial point
            DataContext = this;
        }

		// Add a point to plot
		public void AddPoint(double displacement, double loadFactor)
		{
			LoadDisplacement[0].Values.Add(new ObservablePoint(displacement, loadFactor));
        }

		// Add a range to plot
		public void AddRange(double[] displacements, double[] loadFactor)
		{
			var points = new ObservablePoint[displacements.Length];

			for (int i = 0; i < displacements.Length; i++)
				points[i] = new ObservablePoint(displacements[i], loadFactor[i]);

			LoadDisplacement[0].Values.AddRange(points);
		}

		// Get values to plot
		private ChartValues<ObservablePoint> GetValues()
		{
			var values = new ChartValues<ObservablePoint> { new ObservablePoint(0, 0) };

            if (Displacements == null || LoadFactors == null)
				return values;

			var points = new ObservablePoint[Displacements.Length];

			for (int i = 0; i < Displacements.Length; i++)
				points[i] = new ObservablePoint(Displacements[i], LoadFactors[i]);

			values.AddRange(points);

			return values;
		}

		// Get label
		private string Label(ChartPoint point)
		{
			return
				"LF = " + Math.Round(point.Y, 2) + "\n" +
				"u  = " + Length.FromMillimeters(point.X).ToUnit(DisplacementUnit);
		}

        // When a point is clicked
        private void Chart_OnDataClick(object sender, ChartPoint point)
        {
	        //point instance contains many useful information...
	        //sender is the shape that called the event.

	        MessageBox.Show("LF: " + point.X + " , Displacement: " + point.Y);
        }

        private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
        {
	        Close();
        }

        private void ButtonExport_OnClick(object sender, RoutedEventArgs e)
        {
			// Get displacements and loadfactors as vectors
			var u  = Vector<double>.Build.DenseOfArray(Displacements);
			var lf = Vector<double>.Build.DenseOfArray(LoadFactors);

			// Convert displacements
			if (DisplacementUnit != LengthUnit.Millimeter)
				u = u.Multiply(Auxiliary.ScaleFactor(DisplacementUnit));

			// Get matrix
			var result = Matrix<double>.Build.DenseOfColumnVectors(lf, u);

			// Create headers
			var headers = new[] { "Load Factor", "Displacement (" + Length.GetAbbreviation(DisplacementUnit) + ")" };
			var headerList = headers.ToList();

			// Get location and name
			string
				path   = DataBase.GetFilePath(),
				name   = Path.GetFileNameWithoutExtension(DataBase.Document.Name),
				svName = path + name + "_SPMResult.csv";

            // Export
            DelimitedWriter.Write(svName, result, ";", headerList);

            MessageBox.Show("Data exported to file location.");
        }
    }
}
