using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.LinearAlgebra;
using SPMTool.AutoCAD;
using Material;
using UnitsNet;
using ConcreteParameters = Material.Concrete.Parameters;
using Concrete           = Material.Concrete.Uniaxial;
using Reinforcement      = Material.Reinforcement.Uniaxial;
using StringerData       = SPMTool.XData.Stringer;
using Behavior           = Material.Concrete.Behavior;

namespace SPMTool.Core
{
	/// <summary>
    /// Stringer base class;
    /// </summary>
	public partial class Stringer : SPMElement
	{
		/// <summary>
        /// Type of forces that stringer can be loaded.
        /// </summary>
		public enum ForceState
		{
			Unloaded,
			PureTension,
			PureCompression,
			Combined
		}

        // Stringer properties
		public Units                   Units            { get; }
		public int[]                   Grips            { get; }
		public Point3d[]               PointsConnected  { get; }
		public Length				   DrawingLength    { get; }
		public double                  Length           { get; }
		public double                  Angle            { get; }
		public double                  Width            { get; }
		public double                  Height           { get; }
		public Concrete                Concrete         { get; }
        public Reinforcement           Reinforcement    { get; }
        public Matrix<double>          TransMatrix      { get; }
        public virtual Matrix<double>  LocalStiffness   { get; }
		public virtual Vector<double>  Forces           { get; set; }
		public Vector<double>          Displacements    { get; set; }

		/// <summary>
        /// Stringer base object.
        /// </summary>
        /// <param name="stringerObject">The object ID from AutoCAD drawing.</param>
        /// <param name="units">Units current in use.</param>
        /// <param name="concreteParameters">The concrete parameters.</param>
        /// <param name="concreteBehavior">The concrete behavior.</param>
		public Stringer(ObjectId stringerObject, Units units, ConcreteParameters concreteParameters = null, Behavior concreteBehavior = null)
		{
			ObjectId = stringerObject;
			Units    = units;

			// Read the object as a line
			Line strLine = Geometry.Stringer.ReadStringer(stringerObject);

			// Get the length and angles
			DrawingLength = UnitsNet.Length.From(strLine.Length, Units.Geometry);
			Length        = DrawingLength.Millimeters;
			Angle         = strLine.Angle;

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

			// Get concrete
			Concrete = new Concrete(concreteParameters, Area, concreteBehavior);

            // Get reinforcement
            int numOfBars = Convert.ToInt32 (data[(int) StringerData.NumOfBars].Value);
			double phi    = Convert.ToDouble(data[(int) StringerData.BarDiam].Value);

			if (numOfBars > 0 && phi > 0)
			{
				// Get steel data
				double
					fy = Convert.ToDouble(data[(int) StringerData.Steelfy].Value),
					Es = Convert.ToDouble(data[(int) StringerData.SteelEs].Value);

				// Set steel data
				var steel = new Steel(fy, Es);

				// Set reinforcement
				Reinforcement = new Reinforcement(numOfBars, phi, Area, steel);
			}

			// Calculate transformation matrix
			TransMatrix = TransformationMatrix();
		}

		// Get points
		public Point3d StartPoint => PointsConnected[0];
		public Point3d MidPoint   => PointsConnected[1];
		public Point3d EndPoint   => PointsConnected[2];

		// Set global indexes from grips
		public override int[] DoFIndex => GlobalAuxiliary.GlobalIndexes(Grips);

		// Calculate direction cosines
		public (double cos, double sin) DirectionCosines => GlobalAuxiliary.DirectionCosines(Angle);

		// Calculate steel area
		public double SteelArea => Reinforcement?.Area ?? 0;

		// Calculate concrete area
		public double Area         => Width * Height;
        public double ConcreteArea => Area - SteelArea;

		// Calculate global stiffness
		public Matrix<double> GlobalStiffness => TransMatrix.Transpose() * LocalStiffness * TransMatrix;

		// Calculate local displacements
		public Vector<double> LocalDisplacements => TransMatrix * Displacements;

		/// <summary>
        /// Get normal forces acting in the stringer, in kN.
        /// </summary>
		public (double N1, double N3) NormalForces => (Forces[0], -Forces[2]);

		/// <summary>
		/// Get the state of forces acting on the stringer.
		/// </summary>
		public ForceState State
		{
			get
			{
				var (N1, N3) = NormalForces;

				if (N1 == 0 && N3 == 0)
					return ForceState.Unloaded;

				if (N1 > 0 && N3 > 0)
					return ForceState.PureTension;

				if (N1 < 0 && N3 < 0)
					return ForceState.PureCompression;

				return ForceState.Combined;
			}
		}

        // Global Stringer forces
        public Vector<double> GlobalForces => TransMatrix.Transpose() * Forces;

        // Maximum Stringer force
        public double MaxForce => Forces.AbsoluteMaximum();

        /// <summary>
        /// Calculate the transformation matrix.
        /// </summary>
        private Matrix<double> TransformationMatrix()
        {
	        // Get the direction cosines
	        var (l, m) = DirectionCosines;

	        // Obtain the transformation matrix
	        return Matrix<double>.Build.DenseOfArray(new [,]
	        {
		        {l, m, 0, 0, 0, 0 },
		        {0, 0, l, m, 0, 0 },
		        {0, 0, 0, 0, l, m }
	        });
        }

        /// <summary>
        /// Set Stringer displacements from global displacement vector.
        /// </summary>
        public void SetDisplacements(Vector<double> globalDisplacementVector)
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

		/// <summary>
        /// Do analysis of stringer.
        /// </summary>
        /// <param name="globalDisplacements">The global displacement vector.</param>
        /// <param name="numStrainSteps">The number of strain increments (for nonlinear analysis) (default: 5).</param>
        public virtual void Analysis(Vector<double> globalDisplacements = null, int numStrainSteps = 5)
		{
		}

		public override string ToString()
		{
			// Convert units
			Length
				w = UnitsNet.Length.FromMillimeters(Width).ToUnit(Units.Geometry),
				h = UnitsNet.Length.FromMillimeters(Height).ToUnit(Units.Geometry);

            string msgstr =
				"Stringer " + Number + "\n\n" +
				"Grips: (" + Grips[0] + " - " + Grips[1] + " - " + Grips[2] + ")" + "\n" +
				"Lenght = " + DrawingLength + "\n" +
				"Width = "  + w  + "\n" +
				"Height = " + h;

			if (Reinforcement != null)
			{
				msgstr += "\n\n" + Reinforcement.ToString(Units.Reinforcement, Units.MaterialStrength);
			}

			return msgstr;
		}
    }
}

