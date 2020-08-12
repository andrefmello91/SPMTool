using System.Collections.Generic;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.LinearAlgebra;
using SPMTool.Elements;

namespace SPMTool.Analysis
{
	public class NonLinearAnalysis : Analysis
	{
		// Properties
		public List<double> MonitoredDisplacements { get; set; }
		public List<double> MonitoredLoadFactor    { get; set; }

		public NonLinearAnalysis(InputData inputData, int monitoredIndex, int numLoadSteps = 50, double tolerance = 1E-2, int maxIterations = 1000) : base(inputData)
		{
			// Initiate lists
			MonitoredDisplacements = new List<double>();
			MonitoredLoadFactor    = new List<double>();

			// Get force vector
			var f = ForceVector;

			// Get the initial stiffness and force vector simplified
			var Kg = Global_Stiffness(f);

			//DelimitedWriter.Write("D:/Ki.csv", Kg, ";");

			// Solve the initial displacements
			double lf0 = (double) 1 / numLoadSteps;
			var ui = Kg.Solve(lf0 * f);

			var uMatrix = Matrix<double>.Build.Dense(100, numDoFs);
			var fiMatrix = Matrix<double>.Build.Dense(100, numDoFs);
			var fstr = Matrix<double>.Build.Dense(4, 3);
			var estr = Matrix<double>.Build.Dense(4, 2);
			var fPnl = Matrix<double>.Build.Dense(100, 8);
			var sigCPnl = Matrix<double>.Build.Dense(100, 12);
			var sigSPnl = Matrix<double>.Build.Dense(100, 12);
			var uPnl = Matrix<double>.Build.Dense(100, 8);
			var genStPnl = Matrix<double>.Build.Dense(100, 5);
			var epsPnl = Matrix<double>.Build.Dense(100, 12);
			var DcPnl = Matrix<double>.Build.Dense(1200, 12);
			var DsPnl = Matrix<double>.Build.Dense(1200, 12);
			var thetaPnl1 = Matrix<double>.Build.Dense(100, 4);

			// Initialize a loop for load steps
			for (int loadStep = 1; loadStep <= numLoadSteps; loadStep++)
			{
				// Calculate the current load factor
				double lf = (double)loadStep / numLoadSteps;

				// Get the force vector
				var fs = lf * f;

				Vector<double> fi = Vector<double>.Build.Dense(numDoFs);

				// Initiate iterations
				for (int it = 1; it <= maxIterations; it++)
				{
					// Calculate element displacements and forces
					ElementAnalysis(ui);

					// Get the internal force vector
					fi = InternalForces();

					// Calculate residual forces
					var fr = fs - fi;

					// Check convergence
					if (ConvergenceReached(fr, fs, tolerance, it))
					{
						AutoCAD.Current.edtr.WriteMessage("\nLS = " + loadStep + ": Iterations = " + it);
						MonitoredDisplacements.Add(ui[monitoredIndex]);
						MonitoredLoadFactor.Add(lf);
						break;
					}

					// Increment displacements
					ui += Kg.Solve(fr);
				}

				//var pnl = (Panel.NonLinear) Panels[0];

				fiMatrix.SetRow(loadStep - 1, fi);
				uMatrix.SetRow(loadStep - 1, ui);
				//fPnl.SetRow(loadStep - 1, pnl.Forces);
				//sigCPnl.SetRow(loadStep - 1, pnl.StressVector.sigmaC);
				//sigSPnl.SetRow(loadStep - 1, pnl.StressVector.sigmaS);
				//epsPnl.SetRow(loadStep - 1, pnl.StrainVector);
				//uPnl.SetRow(loadStep - 1, pnl.Displacements);
				//DcPnl.SetSubMatrix(12 * (loadStep - 1), 0, pnl.MaterialStiffness.Dc);
				//DsPnl.SetSubMatrix(12 * (loadStep - 1), 0, pnl.MaterialStiffness.Ds);

				//var thetaPnl = new double[4];

				//for (int i = 0; i < 4; i++)
				//    thetaPnl[i] = pnl.IntegrationPoints[i].Concrete.PrincipalAngles.theta2;

				//thetaPnl1.SetRow(loadStep - 1, thetaPnl);

				// Set the results to elements
				Results();

				// Update stiffness
				Kg = Global_Stiffness();


				//if (loadStep < 56)
				//{
				//    foreach (Stringer.NonLinear Stringer in Stringers)
				//    {
				//        fstr.SetRow(Stringer.Number - 1, Stringer.Forces);
				//        estr.SetRow(Stringer.Number - 1, new[] { Stringer.GenStrains.e1, Stringer.GenStrains.e3 });
				//    }
				//}
			}

			//uWindow.AddRange(uList.ToArray(), lfList.ToArray());

			// Set nodal displacements
			NodalDisplacements(ui);

			DelimitedWriter.Write("D:/K.csv", Kg, ";");
			DelimitedWriter.Write("D:/f.csv", f.ToColumnMatrix(), ";");
			DelimitedWriter.Write("D:/fi.csv", fiMatrix, ";");
			//DelimitedWriter.Write("D:/fstr.csv", fstr, ";");
			//DelimitedWriter.Write("D:/estr.csv", estr, ";");
			DelimitedWriter.Write("D:/u.csv", uMatrix, ";");
			//DelimitedWriter.Write("D:/genStPnl.csv", genStPnl, ";");
			DelimitedWriter.Write("D:/sigCPnl.csv", sigCPnl, ";");
			DelimitedWriter.Write("D:/sigSPnl.csv", sigSPnl, ";");
			DelimitedWriter.Write("D:/epsPnl.csv", epsPnl, ";");

			//for (int i = 0; i < 4; i++)
			//{
			//    int n = i + 1;
			//    DelimitedWriter.Write("D:/fStr" + n + ".csv", fStrs[i], ";");
			//}

			//DelimitedWriter.Write("D:/fPnl1.csv", fPnl, ";");
			//DelimitedWriter.Write("D:/uPnl1.csv", uPnl, ";");
			//DelimitedWriter.Write("D:/DcPnl1.csv", DcPnl, ";");
			//DelimitedWriter.Write("D:/DsPnl1.csv", DsPnl, ";");
			//DelimitedWriter.Write("D:/sigPnl1.csv", sigPnl, ";");
			//DelimitedWriter.Write("D:/f1Pnl1.csv", f1Pnl, ";");
			//DelimitedWriter.Write("D:/e1Pnl1.csv", e1Pnl, ";");
			DelimitedWriter.Write("D:/thetaPnl1.csv", thetaPnl1, ";");
			//DelimitedWriter.Write("D:/DPnl1.csv", DPnl, ";");

			//DelimitedWriter.Write("D:/KPnl1.csv", KPnl, ";");
		}

		// Calculate convergence
		private double Convergence(Vector<double> residualForces, Vector<double> appliedForces)
		{
			double
				num = 0,
				den = 1;

			for (int i = 0; i < residualForces.Count; i++)
			{
				num += residualForces[i] * residualForces[i];
				den += appliedForces[i] * appliedForces[i];
			}

			return
				num / den;
		}

		// Verify if convergence is reached
		private bool ConvergenceReached(double convergence, double tolerance, int iteration, int minIterations = 5) => convergence <= tolerance && iteration >= minIterations;

		// Verify if convergence is reached
		private bool ConvergenceReached(Vector<double> residualForces, Vector<double> appliedForces, double tolerance,
			int iteration, int minIterations = 5) => ConvergenceReached(Convergence(residualForces, appliedForces), tolerance, iteration, minIterations);

		// Verify convergence
		private bool EquilibriumConvergence(Vector<double> residualForces, Vector<double> residualDisplacements, int iteration)
		{
			double
				maxForce = residualForces.AbsoluteMaximum(),
				maxDisp  = residualDisplacements.AbsoluteMaximum(),
				fTol     = 0.01,
				uTol     = 0.01;

			// Check convergence
			if (maxForce <= fTol && maxDisp <= uTol && iteration > 1)
				return true;

			// Else
			return false;
		}

		// Get the internal force vector
		private Vector<double> InternalForces()
		{
			var fi = Vector<double>.Build.Dense(numDoFs);

			foreach (NonLinearStringer stringer in Stringers)
			{
				// Get index and forces
				int[] index = stringer.DoFIndex;
				var fs = stringer.IterationGlobalForces;

				// Add values
				AddForce(fs, index);
			}

			if (Panels.Length > 0)
			{
				foreach (NonLinearPanel panel in Panels)
				{
					// Get index and forces
					int[] index = panel.DoFIndex;
					var fp = panel.Forces;

					// Add values
					AddForce(fp, index);
				}
			}

			// Add element forces to global vector
			void AddForce(Vector<double> elementForces, int[] dofIndex)
			{
				for (int i = 0; i < dofIndex.Length; i++)
				{
					// DoF index
					int j = dofIndex[i];

					// Add values
					fi[j] += elementForces[i];
				}
			}

			// Simplify for constraints
			foreach (var i in Constraints)
				fi[i] = 0;

			return fi;
		}

		// Set the results for each Stringer
		private void Results()
		{
			foreach (NonLinearStringer stringer in Stringers)
				stringer.Results();

			foreach (NonLinearPanel panel in Panels)
				panel.UpdateStiffness();
		}
	}
}