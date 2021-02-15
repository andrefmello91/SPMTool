using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Extensions;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Data.Text;
using SPMTool.Core.Conditions;
using SPMTool.Core;
using UnitsNet;
using UnitsNet.Units;

namespace SPMTool.Application.UserInterface
{
    /// <summary>
    /// Lógica interna para GraphWindow.xaml
    /// </summary>
    public partial class GraphWindow : Window
    {
		/// <summary>
		/// Get/set the load-displacement <see cref="SeriesCollection"/>.
		/// </summary>
		public SeriesCollection LoadDisplacement  { get; set; }

		/// <summary>
		/// Get the <see cref="LengthUnit"/> of <see cref="Displacements"/>.
		/// </summary>
		private LengthUnit DisplacementUnit { get; }

		/// <summary>
		/// Get the displacements collection.
		/// </summary>
		public double[] Displacements { get; }

		/// <summary>
		/// Get the load factors collection.
		/// </summary>
		public double[] LoadFactors { get; }

		/// <summary>
		/// Get the displacement axis title.
		/// </summary>
		public string DisplacementTitle => $"Displacement ({DisplacementUnit.Abbrev()})";

		/// <summary>
		/// Get/set inverted displacement axis state.
		/// </summary>
		private bool Inverted { get; set; }

		/// <summary>
		/// <see cref="GraphWindow"/> constructor.
		/// </summary>
		/// <param name="displacements">The collection of displacements.</param>
		/// <param name="loadFactors">The collection of load factors.</param>
		/// <param name="displacementUnit">The <see cref="LengthUnit"/> of <paramref name="displacements"/>.</param>
		public GraphWindow(IEnumerable<double> displacements = null, IEnumerable<double> loadFactors = null, LengthUnit displacementUnit = LengthUnit.Millimeter)
        {
            InitializeComponent();

            Displacements    = displacements.ToArray();
            LoadFactors      = loadFactors.ToArray();
            DisplacementUnit = displacementUnit;

			// Initiate series
			LoadDisplacement = new SeriesCollection
			{
				new LineSeries
				{
					Title           = "Load Factor x Displacement", 
					Values          = GetValues(), 
					PointGeometry   = null,
					StrokeThickness = 3,
					Stroke          = Brushes.LightSkyBlue,
					Fill            = Brushes.Transparent,
					DataLabels      = false,
					LabelPoint      = Label
				}
			};

			// Set mapper
			SetMapper();

			// Set initial point
			LiveCharts.Wpf.CartesianChart.Series = LoadDisplacement;
            DataContext = this;
        }

		/// <summary>
		/// Get values to plot
		/// </summary>
		private ChartValues<ObservablePoint> GetValues()
		{
			var values = new ChartValues<ObservablePoint> { new ObservablePoint(0, 0) };

            if (Displacements is null || LoadFactors is null)
				return values;
			
			values.AddRange(Displacements.Zip(LoadFactors, ( d,  l) => new ObservablePoint(d, l)));

			return values;
		}

		/// <summary>
		/// Set mapper for inverting X axis.
		/// </summary>
		private void SetMapper()
		{
			// If there is a displacement bigger than zero, nothing is done
			if (Displacements.Any(d => !d.ApproxZero(1E-6) && d > 0))
			{
				Inverted = false;
				return;
			}

			// Invert x values
			Inverted = true;
			LoadDisplacement.Configuration = LiveCharts.Configurations.Mappers.Xy<ObservablePoint>()
				.X(point => -point.X)
				.Y(point =>  point.Y);

			// Correct the labels
			DisplacementAxis.LabelFormatter = x => $"{x * -1}";
		}

		/// <summary>
		/// Get the label of a <paramref name="point"/>.
		/// </summary>
		/// <param name="point">The <see cref="ChartPoint"/>.</param>
		private string Label(ChartPoint point)
		{
			return
				$"LF = {point.Y:0.00}\n" +
				$"u  = {Length.FromMillimeters(Inverted ? - point.X : point.X).ToUnit(DisplacementUnit)}";
		}

        private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
        {
	        Close();
        }

        private void ButtonExport_OnClick(object sender, RoutedEventArgs e)
        {
	        // Convert displacements
			var disp = DisplacementUnit is LengthUnit.Millimeter
				? Displacements
				: Displacements.Select(d => d.ConvertFromMillimeter(DisplacementUnit)).ToArray();

			// Get displacements and loadfactors as vectors
			var u  = disp.ToVector();
			var lf = LoadFactors.ToVector();

			// Get matrix
			var result = Matrix<double>.Build.DenseOfColumnVectors(lf, u);

			// Create headers
			var headers = new[] { "Load Factor", $"Displacement ({DisplacementUnit.Abbrev()})" };

			// Get location and name
			string
				path   = DataBase.GetFilePath(),
				name   = Path.GetFileNameWithoutExtension(DataBase.Document.Name),
				svName = path + name + "_SPMResult.csv";

            // Export
            DelimitedWriter.Write(svName, result, ";", headers);

            MessageBox.Show("Data exported to file location.");
        }
    }
}
