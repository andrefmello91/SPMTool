using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.LinearAlgebra;
using SPMTool.AutoCAD;
using SPMTool.Material;
using PanelData = SPMTool.XData.Panel;

namespace SPMTool.Core
{
    public partial class Panel
    {
        // Enum for panel Stringer behavior
        public enum Behavior
        {
            Linear,
            NonLinearMCFT,
            NonLinearDSFM
        }

        // Panel parameters
        public Behavior                                      PanelBehavior     { get; }
        public ObjectId                                      ObjectId          { get; }
        public int                                           Number            { get; }
        public int[]                                         Grips             { get; }
        public Point3d[]                                     Vertices          { get; }
        public (double[] x, double[] y)                      VertexCoordinates { get; }
        public (double a, double b, double c, double d)      Dimensions        { get; }
        public (double[] Length, double[] Angle)             Edges             { get; }
        public double                                        Width             { get; }
        public Concrete                                      Concrete          { get; }
        public PanelReinforcement                            Reinforcement     { get; }
        public Matrix<double>                                LocalStiffness    { get; set; }
        public virtual Matrix<double>                        GlobalStiffness   { get; }
        public Vector<double>                                Displacements     { get; set; }
        public Vector<double>                                Forces            { get; set; }
        public virtual Vector<double>                        AverageStresses   { get; }
        public virtual (Vector<double> sigma, double theta)  PrincipalStresses { get; }

        // Constructor
        public Panel(ObjectId panelObject, Concrete concrete = null, Behavior behavior = Behavior.Linear)
        {
	        ObjectId      = panelObject;
	        PanelBehavior = behavior;

	        // Get concrete
	        if (concrete == null)
		        Concrete = AutoCAD.Material.ReadConcreteData();
	        else
		        Concrete = concrete;

	        // Read as a solid
	        Solid pnl = Geometry.Panel.ReadPanel(panelObject);

	        // Read the XData and get the necessary data
	        TypedValue[] pnlData = Auxiliary.ReadXData(pnl);

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
	        Reinforcement = new PanelReinforcement((phiX, phiY), (sx, sy), steel, Width);
        }

        // Set global indexes from grips
        public int[] DoFIndex => GlobalAuxiliary.GlobalIndexes(Grips);

        // Get X and Y coordinates of a panel vertices
        public (double[] x, double[] y) Vertex_Coordinates()
        {
	        double[]
		        x = new double[4],
		        y = new double[4];

	        // Get X and Y coordinates of the vertices
	        for (int i = 0; i < 4; i++)
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

        // Calculate reference length
		public double ReferenceLength
		{
			get
			{
				var (a, b, _, _) = Dimensions;

				return
					Math.Min(a, b);
			}
		}

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
	        for (int i = 0; i < 4; i++)
	        {
		        l[i] = ln[i].Length;
		        a[i] = ln[i].Angle;
	        }

	        return (l, a);
        }

        // Calculate direction cosines of each edge
        public (double cos, double sin)[] DirectionCosines
        {
            get
            {
                (double cos, double sin)[] directionCosines = new (double cos, double sin)[4];

                var angles = Edges.Angle;

                for (int i = 0; i < 4; i++)
                    directionCosines[i] = GlobalAuxiliary.DirectionCosines(angles[i]);

                return
                    directionCosines;
            }
        }

        // Get panel displacements from global displacement vector
        public void Displacement(Vector<double> globalDisplacementVector)
        {
            var u = globalDisplacementVector;
            int[] ind = DoFIndex;

            // Get the displacements
            var up = Vector<double>.Build.Dense(8);
            for (int i = 0; i < ind.Length; i++)
            {
                // Indexers
                int j = ind[i];

                // Set values
                up[i] = u[j];
            }

            // Set
            Displacements = up;
        }

        // Maximum panel force
        public double MaxForce => Forces.AbsoluteMaximum();

        // Function to verify if a panel is rectangular
        public bool Rectangular
        {
	        get
	        {
		        // Calculate the angles between the edges
		        double ang2 = Edges.Angle[1] - Edges.Angle[0];
		        double ang4 = Edges.Angle[3] - Edges.Angle[2];

		        if (ang2 == Constants.PiOver2 && ang4 == Constants.PiOver2)
			        return true;

		        return false;
	        }
        }

        public virtual void Analysis()
        {
	    }
    }
}
