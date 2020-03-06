using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.LinearAlgebra;

namespace SPMTool
{
	public class Stringer
    {
	    // Stringer properties
	    public ObjectId               ObjectId        { get; }
	    public int                    Number          { get; }
	    public int[]                  Grips           { get; }
	    public Point3d[]              PointsConnected { get; }
	    public double                 Length          { get; }
	    public double                 Angle           { get; }
	    public double                 Width           { get; }
	    public double                 Height          { get; }
	    public int                    NumberOfBars    { get; }
	    public double                 BarDiameter     { get; }
	    public virtual Matrix<double> LocalStiffness  { get; set; }
        public Vector<double>         Forces          { get; set; }

        // Constructor
        public Stringer(ObjectId stringerObject, Material.Concrete concrete, Material.Steel steel)
		{
			ObjectId = stringerObject;

            // Start a transaction
            using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
            {
	            // Read the object as a line
	            Line strLine = trans.GetObject(stringerObject, OpenMode.ForRead) as Line;

	            // Get the length and angles
	            Length = strLine.Length;
	            Angle  = strLine.Angle;

	            // Calculate midpoint
	            var midPt = Auxiliary.MidPoint(strLine.StartPoint, strLine.EndPoint);

	            // Get the points
	            PointsConnected = new[] { strLine.StartPoint, midPt, strLine.EndPoint };

	            // Read the XData and get the necessary data
	            ResultBuffer rb = strLine.GetXDataForApplication(AutoCAD.appName);
	            TypedValue[] data = rb.AsArray();

	            // Get the stringer number
	            Number = Convert.ToInt32(data[(int) XData.Stringer.Number].Value);

	            // Get reinforcement
	            NumberOfBars = Convert.ToInt32(data[(int) XData.Stringer.NumOfBars].Value);
	            BarDiameter  = Convert.ToDouble(data[(int) XData.Stringer.BarDiam].Value);

	            // Create the list of grips
	            Grips = new []
	            {
		            Convert.ToInt32(data[(int) XData.Stringer.Grip1].Value),
		            Convert.ToInt32(data[(int) XData.Stringer.Grip2].Value),
		            Convert.ToInt32(data[(int) XData.Stringer.Grip3].Value)
	            };

	            // Get geometry
	            Width  = Convert.ToDouble(data[(int) XData.Stringer.Width].Value);
	            Height = Convert.ToDouble(data[(int) XData.Stringer.Height].Value);
            }
		}

        // Set global indexes from grips
        public int[] Index => GlobalIndexes();
        private int[] GlobalIndexes()
		{
			// Initialize the array
			int[] ind = new int[Grips.Length];

			// Get the indexes
			for (int i = 0; i < Grips.Length; i++)
				ind[i] = 2 * Grips[i] - 2;

			return ind;
		}

        // Calculate steel area
        public double SteelArea => Reinforcement.StringerReinforcement(NumberOfBars, BarDiameter);

		// Calculate concrete area
		public double ConcreteArea => Width * Height - SteelArea;

		// Calculate reinforcement ratio
		private double ps => SteelArea / ConcreteArea;

		// Private parameters
		private double EcAc { get; set; }
		private double EsAs { get; set; }
		private double xi => EsAs / EcAc;
		private double t1 => EcAc * (1 + xi);

        // Calculate the transformation matrix
        public  Matrix<double>       TransMatrix => transMatrix.Value;
		private Lazy<Matrix<double>> transMatrix => new Lazy<Matrix<double>>(TransformationMatrix);
        private Matrix<double> TransformationMatrix()
		{
			// Get the direction cosines
			double[] dirCos = Auxiliary.DirectionCosines(Angle);
			double
				l = dirCos[0],
				m = dirCos[1];

			// Obtain the transformation matrix
			return Matrix<double>.Build.DenseOfArray(new double[,]
			{
				{l, m, 0, 0, 0, 0 },
				{0, 0, l, m, 0, 0 },
				{0, 0, 0, 0, l, m }
			});
		}

        // Calculate global stiffness
        public Matrix<double>        GlobalStiffness => globalStiffness.Value;
        private Lazy<Matrix<double>> globalStiffness => new Lazy<Matrix<double>>(GloballStiffness);
        public Matrix<double> GloballStiffness()
        {
	        var T = TransMatrix;

	        return T.Transpose() * LocalStiffness * T;
        }

        // Calculate stringer forces
        public void StringerForces(Vector<double> displacementVector)
        {
	        // Get the parameters
	        int[] ind = Index;
	        var Kl = LocalStiffness;
	        var T = TransMatrix;
	        var u = displacementVector;

            // Get the displacements
            var uStr = Vector<double>.Build.DenseOfArray(new []
	        {
		        u[ind[0]] , u[ind[0] + 1], u[ind[1]], u[ind[1] + 1], u[ind[2]] , u[ind[2] + 1]
	        });

	        // Get the displacements in the direction of the stringer
	        var ul = T * uStr;

	        // Calculate the vector of normal forces (in kN)
	        var fl = 0.001 * Kl * ul;

	        // Aproximate small values to zero
	        fl.CoerceZero(0.000001);

	        // Save to the stringer
	        Forces = fl;
        }

        public class Linear : Stringer
		{
			// Private properties
			private double L     => Length;
			private double Ac    => ConcreteArea;
			private double Ec    { get; }

            // Constructor
            public Linear(ObjectId stringerObject, Material.Concrete concrete, Material.Steel steel = null) : base(stringerObject, concrete, steel)
            {
	            Ec = concrete.Eci;
            }

            // Calculate local stiffness
            public override Matrix<double> LocalStiffness => localStiffness.Value;
            private Lazy<Matrix<double>>   localStiffness => new Lazy<Matrix<double>>(Stiffness);
            private Matrix<double> Stiffness()
			{
				// Calculate the constant factor of stiffness
				double EcAOverL = Ec * Ac / L;

				// Calculate the local stiffness matrix
				return EcAOverL * Matrix<double>.Build.DenseOfArray(new double[,]
				{
					{  4, -6,  2 },
					{ -6, 12, -6 },
					{  2, -6,  4 }
				});
            }
		}
    }
}
