using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Complex;
using MathNet.Numerics.LinearAlgebra.Storage;

namespace SPMTool.Core
{
    public static class StressRelations
    {
        // Calculate tensile strain angle
        public static (double theta1, double theta2) PrincipalAngles(Vector<double> stresses, (double f1, double f2) principalStresses)
        {
	        double theta1 = Constants.PiOver4;

	        // Get the strains
	        var f   = stresses;
	        var f2 = principalStresses.f2;

	        // Verify the strains
	        if (f.Exists(GlobalAuxiliary.NotZero))
	        {
		        // Calculate the strain slope
		        if (f[2] == 0)
			        theta1 = 0;

		        else if (Math.Abs(f[0] - f[1]) <= 1E-9 && f[2] < 0)
			        theta1 = -Constants.PiOver4;

		        else
			        //theta1 = 0.5 * Trig.Atan(e[2] / (e[0] - e[1]));
			        theta1 = Constants.PiOver2 - Trig.Atan((f[0] - f2) / f[2]);
	        }

	        // Calculate theta2
	        double theta2 = Constants.PiOver2 - theta1;

	        //if (theta2 > Constants.PiOver2)
	        //	theta2 -= Constants.Pi;

	        return
		        (theta1, theta2);
        }

        // Calculate principal strains
        public static (double e1, double e2) PrincipalStresses(Vector<double> stresses)
        {
            // Get the strains
            var f = stresses;

	        // Calculate radius and center of Mohr's Circle
	        double
		        cen = 0.5 * (f[0] + f[1]),
		        rad = Math.Sqrt(0.25 * (f[1] - f[0]) * (f[1] - f[0]) + f[2] * f[2]);

	        // Calculate principal strains in concrete
	        double
		        f1 = cen + rad,
		        f2 = cen - rad;

	        return
		        (f1, f2);
        }

		// Calculate stresses from principal
		public static Vector<double> StressesFromPrincipal((double f1, double f2) principalStresses, double theta2)
		{
			// Get principal stresses
			var (f1, f2) = principalStresses;

			// Calculate theta2 (fc2 angle)
			var (cos, sin) = GlobalAuxiliary.DirectionCosines(2 * theta2);

			// Calculate stresses by Mohr's Circle
			double
				cen  = 0.5 * (f1 + f2),
				rad  = 0.5 * (f1 - f2),
				fx   = cen - rad * cos,
				fy   = cen + rad * cos,
				fxy  = rad * sin;

			return
				CreateVector.DenseOfArray(new[] { fx, fy, fxy });
        }

		// Calculate stresses/strains transformation matrix
		// This matrix transforms from x-y to 1-2 coordinates
		public static Matrix<double> TransformationMatrix(double theta1)
		{
			var (cos, sin) = GlobalAuxiliary.DirectionCosines(theta1);
			double
				cos2   = cos * cos,
				sin2   = sin * sin,
				cosSin = cos * sin;

			return
				Matrix<double>.Build.DenseOfArray(new[,]
				{
					{        cos2,       sin2,      cosSin },
					{        sin2,       cos2,     -cosSin },
					{ -2 * cosSin, 2 * cosSin, cos2 - sin2 }
				});
		}
    }
}
