using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace SPMTool.UserInterface
{
    /// <summary>
    /// Lógica interna para GraphWindow.xaml
    /// </summary>
    public partial class GraphWindow : Window
    {
        // Properties
		public SeriesCollection LoadDisplacement { get; set; }

        public GraphWindow()
        {
            InitializeComponent();

			// Initiate series
			LoadDisplacement = new SeriesCollection
			{
				new LineSeries()
				{
					Title = "Load Factor x Displacement", 
					Values = new ChartValues<ObservablePoint>{ new ObservablePoint(0, 0) }, 
					PointGeometry = null,
					StrokeThickness = 3,
					Stroke = Brushes.LightSkyBlue,
					Fill = Brushes.Transparent,
					DataLabels = false,
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

        // When a point is clicked
        private void Chart_OnDataClick(object sender, ChartPoint point)
        {
	        //point instance contains many useful information...
	        //sender is the shape that called the event.

	        MessageBox.Show("LF: " + point.X + " , Displacement: " + point.Y);
        }

    }
}
