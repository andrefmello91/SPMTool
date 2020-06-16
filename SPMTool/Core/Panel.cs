using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.LinearAlgebra;
using SPMTool.AutoCAD;
using Material;
using Concrete           = Material.Concrete.Biaxial;
using ConcreteParameters = Material.Concrete.Parameters;
using Reinforcement      = Material.Reinforcement.Biaxial;
using PanelData          = SPMTool.XData.Panel;
using Behavior           = Material.Concrete.Behavior;

namespace SPMTool.Core
{
	public partial class Panel : SPMElement
	{
		// Panel parameters
		public int[]                                         Grips             { get; }
		public Point3d[]                                     Vertices          { get; }
		public (double[] x, double[] y)                      VertexCoordinates { get; }
		public (double a, double b, double c, double d)      Dimensions        { get; }
		public (double[] Length, double[] Angle)             Edges             { get; }
		public double                                        Width             { get; }
		public Concrete                                      Concrete          { get; }
		public Reinforcement                                 Reinforcement     { get; }
		public Matrix<double>                                LocalStiffness    { get; set; }
		public virtual Matrix<double>                        GlobalStiffness   { get; }
		public Vector<double>                                Displacements     { get; set; }
		public Vector<double>                                Forces            { get; set; }
		public virtual Vector<double>                        AverageStresses   { get; }
		public virtual (Vector<double> sigma, double theta)  PrincipalStresses { get; }

		// Constructor
		public Panel(ObjectId panelObject, ConcreteParameters concreteParameters = null,
			Behavior behavior = null)
		{
			ObjectId      = panelObject;

			// Get concrete
			Concrete = new Concrete(concreteParameters, behavior);

			// Read as a solid
			var pnl = Geometry.Panel.ReadPanel(panelObject);

			// Read the XData and get the necessary data
			var pnlData = Auxiliary.ReadXData(pnl);

			// Get the panel parameters
			Number = Convert.ToInt32 (pnlData[(int) PanelData.Number].Value);
			Width  = Convert.ToDouble(pnlData[(int) PanelData.Width] .Value);

			// Create the list of grips
			Grips = new[]
			{
				Convert.ToInt32(pnlData[(int) PanelData.Grip1].Value),
				Convert.ToInt32(pnlData[(int) PanelData.Grip2].Value),
				Convert.ToInt32(pnlData[(int) PanelData.Grip3].Value),
				Convert.ToInt32(pnlData[(int) PanelData.Grip4].Value)
			};

			// Create the list of vertices
			Vertices = Geometry.Panel.PanelVertices(pnl);

			// Calculate vertex coordinates and dimensions
			VertexCoordinates = Vertex_Coordinates();
			Dimensions        = CalculateDimensions();
			Edges             = EdgesLengthAndAngles();

			// Get reinforcement
			double
				phiX = Convert.ToDouble(pnlData[(int) PanelData.XDiam].Value),
				phiY = Convert.ToDouble(pnlData[(int) PanelData.YDiam].Value),
				sx   = Convert.ToDouble(pnlData[(int) PanelData.Sx].Value),
				sy   = Convert.ToDouble(pnlData[(int) PanelData.Sy].Value);

			// Get steel data
			double
				fyx = Convert.ToDouble(pnlData[(int) PanelData.fyx].Value),
				Esx = Convert.ToDouble(pnlData[(int) PanelData.Esx].Value),
				fyy = Convert.ToDouble(pnlData[(int) PanelData.fyy].Value),
				Esy = Convert.ToDouble(pnlData[(int) PanelData.Esy].Value);

			var steel =
			(
				new Steel(fyx, Esx),
				new Steel(fyy, Esy)
			);

			// Set reinforcement
			Reinforcement = new Reinforcement((phiX, phiY), (sx, sy), steel, Width);
		}

		// Set global indexes from grips
		public override int[] DoFIndex => GlobalAuxiliary.GlobalIndexes(Grips);

		// Get the center point
		public Point3d CenterPoint
		{
			get
			{
				// Calculate the approximated center point
				var Pt1 = GlobalAuxiliary.MidPoint(Vertices[0], Vertices[2]);
				var Pt2 = GlobalAuxiliary.MidPoint(Vertices[1], Vertices[3]);

				return
					GlobalAuxiliary.MidPoint(Pt1, Pt2);
			}
		}

		// Calculate direction cosines of each edge
		public (double cos, double sin)[] DirectionCosines
		{
			get
			{
				var directionCosines = new (double cos, double sin)[4];

				var angles = Edges.Angle;

				for (var i = 0; i < 4; i++)
					directionCosines[i] = GlobalAuxiliary.DirectionCosines(angles[i]);

				return
					directionCosines;
			}
		}

		// Maximum panel force
		public double MaxForce => Forces.AbsoluteMaximum();

		// Function to verify if a panel is rectangular
		public bool Rectangular
		{
			get
			{
				// Calculate the angles between the edges
				var ang2 = Edges.Angle[1] - Edges.Angle[0];
				var ang4 = Edges.Angle[3] - Edges.Angle[2];

				if (ang2 == Constants.PiOver2 && ang4 == Constants.PiOver2)
					return true;

				return false;
			}
		}

        // Get X and Y coordinates of a panel vertices
        public (double[] x, double[] y) Vertex_Coordinates()
		{
			double[]
				x = new double[4],
				y = new double[4];

			// Get X and Y coordinates of the vertices
			for (var i = 0; i < 4; i++)
			{
				x[i] = Vertices[i].X;
				y[i] = Vertices[i].Y;
			}

			return (x, y);
		}

		// Panel dimensions
		public (double a, double b, double c, double d) CalculateDimensions()
		{
			var (x, y) = VertexCoordinates;

			// Calculate the necessary dimensions of the panel
			double
				a = 0.5 * (x[1] + x[2] - x[0] - x[3]),
				b = 0.5 * (y[2] + y[3] - y[0] - y[1]),
				c = 0.5 * (x[2] + x[3] - x[0] - x[1]),
				d = 0.5 * (y[1] + y[2] - y[0] - y[3]);

			return
				(a, b, c, d);
		}

		// Get edge lengths and angles
		public (double[] Length, double[] Angle) EdgesLengthAndAngles()
		{
			double[]
				l = new double[4],
				a = new double[4];

			// Create lines to measure the angles between the edges and dimensions
			Line[] ln =
			{
				new Line(Vertices[0], Vertices[1]),
				new Line(Vertices[1], Vertices[2]),
				new Line(Vertices[2], Vertices[3]),
				new Line(Vertices[3], Vertices[0])
			};

			// Create the list of dimensions
			for (var i = 0; i < 4; i++)
			{
				l[i] = ln[i].Length;
				a[i] = ln[i].Angle;
			}

			return (l, a);
		}

		// Get panel displacements from global displacement vector
		public void SetDisplacements(Vector<double> globalDisplacementVector)
		{
			var u = globalDisplacementVector;
			var ind = DoFIndex;

			// Get the displacements
			var up = Vector<double>.Build.Dense(8);
			for (var i = 0; i < ind.Length; i++)
			{
				// Indexers
				var j = ind[i];

				// Set values
				up[i] = u[j];
			}

			// Set
			Displacements = up;
		}

		public virtual void Analysis(Vector<double> globalDisplacements = null)
		{
		}

		public override string ToString()
		{
			var msgstr =
				"Panel " + Number + "\n\n" +
				"Grips: (" + Grips[0] + " - " + Grips[1] + " - " + Grips[2] + " - " + Grips[3] +
				")" + "\n" +
				"Width = " + Width + " mm";

			if (Reinforcement.IsSet)
				msgstr += "\n\n" + Reinforcement;

			return msgstr;
		}
	}
}