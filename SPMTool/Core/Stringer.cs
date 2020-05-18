using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.LinearAlgebra;
using SPMTool.AutoCAD;
using SPMTool.Material;
using StringerData = SPMTool.XData.Stringer;

namespace SPMTool.Core
{
	public partial class Stringer
	{
		// Enum for setting Stringer behavior
		public enum Behavior
		{
			Linear,
			NonLinearClassic,
			NonLinearMC2010
		}

        // Stringer properties
        public ObjectId                ObjectId         { get; }
		public int                     Number           { get; }
		public int[]                   Grips            { get; }
		public Point3d[]               PointsConnected  { get; }
		public double                  Length           { get; }
		public double                  Angle            { get; }
		public double                  Width            { get; }
		public double                  Height           { get; }
		public Concrete                Concrete         { get; }
        public StringerReinforcement   Reinforcement    { get; }
        public virtual Matrix<double>  LocalStiffness   { get; }
		public virtual Vector<double>  Forces           { get; }
		public Vector<double>          Displacements    { get; set; }

		// Constructor
		public Stringer(ObjectId stringerObject, Concrete concrete = null)
		{
			ObjectId = stringerObject;

			// Get concrete
			if (concrete == null)
				Concrete = AutoCAD.Material.ReadConcreteData();
			else
				Concrete = concrete;

			// Read the object as a line
			Line strLine = Geometry.Stringer.ReadStringer(stringerObject);

			// Get the length and angles
			Length = strLine.Length;
			Angle  = strLine.Angle;

			// Calculate midpoint
			var midPt = GlobalAuxiliary.MidPoint(strLine.StartPoint, strLine.EndPoint);

			// Get the points
			PointsConnected = new[] { strLine.StartPoint, midPt, strLine.EndPoint };

			// Read the XData and get the necessary data
			TypedValue[] data = Auxiliary.ReadXData(strLine);

			// Get the Stringer number
			Number = Convert.ToInt32(data[(int) StringerData.Number].Value);

			// Create the list of grips
			Grips = new []
			{
				Convert.ToInt32(data[(int) StringerData.Grip1].Value),
				Convert.ToInt32(data[(int) StringerData.Grip2].Value),
				Convert.ToInt32(data[(int) StringerData.Grip3].Value)
			};

			// Get geometry
			Width  = Convert.ToDouble(data[(int) StringerData.Width].Value);
			Height = Convert.ToDouble(data[(int) StringerData.Height].Value);

			// Get reinforcement
			int numOfBars = Convert.ToInt32(data[(int) StringerData.NumOfBars].Value);
			double phi    = Convert.ToDouble(data[(int) StringerData.BarDiam].Value);

			// Get steel data
			double
				fy = Convert.ToDouble(data[(int) StringerData.Steelfy].Value),
				Es = Convert.ToDouble(data[(int) StringerData.SteelEs].Value);

			// Set steel data
			var steel = new Steel(fy, Es);

			// Set reinforcement
			Reinforcement = new StringerReinforcement(numOfBars, phi, steel);

			// Calculate transformation matrix
			TransMatrix = TransformationMatrix();
		}

		// Set global indexes from grips
		public int[] DoFIndex => GlobalAuxiliary.GlobalIndexes(Grips);

		// Calculate direction cosines
		public (double cos, double sin) DirectionCosines => GlobalAuxiliary.DirectionCosines(Angle);

		// Calculate steel area
		public double SteelArea => Reinforcement.Area;

		// Calculate concrete area
		public double ConcreteArea => Width * Height - SteelArea;

		// Calculate the transformation matrix
		public  Matrix<double> TransMatrix { get; }
		private Matrix<double> TransformationMatrix()
		{
			// Get the direction cosines
			var (l, m) = DirectionCosines;

			// Obtain the transformation matrix
			return Matrix<double>.Build.DenseOfArray(new double[,]
			{
				{l, m, 0, 0, 0, 0 },
				{0, 0, l, m, 0, 0 },
				{0, 0, 0, 0, l, m }
			});
		}

		// Calculate global stiffness
		public Matrix<double> GlobalStiffness
		{
			get
			{
				var T = TransMatrix;

				return
					T.Transpose() * LocalStiffness * T;
			}
		}

		// Get Stringer displacements from global displacement vector
		public void Displacement(Vector<double> globalDisplacementVector)
		{
			var u = globalDisplacementVector;
			int[] ind = DoFIndex;

			// Get the displacements
			var us = Vector<double>.Build.Dense(6);
			for (int i = 0; i < ind.Length; i++)
			{
				// Global index
				int j = ind[i];

				// Set values
				us[i] = u[j];
			}

			// Set
			Displacements = us;
		}

		// Calculate local displacements
		public Vector<double> LocalDisplacements => TransMatrix * Displacements;

        // Global Stringer forces
        public Vector<double> GlobalForces => TransMatrix.Transpose() * Forces;

        // Maximum Stringer force
        public double MaxForce => Forces.AbsoluteMaximum();
	}
}

