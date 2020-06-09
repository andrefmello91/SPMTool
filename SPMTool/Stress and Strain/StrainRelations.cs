using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Complex;
using MathNet.Numerics.LinearAlgebra.Storage;
using SPMTool.AutoCAD;

namespace SPMTool.Core
{
    public static class StrainRelations
    {
		// Calculate tensile strain angle
        public static (double theta1, double theta2) PrincipalAngles(Vector<double> strains, (double e1, double e2) principalStrains)
        {
	        double theta1 = Constants.PiOver4;

	        // Get the strains
	        var e   = strains;
	        var ec2 = principalStrains.e2;

	        // Verify the strains
	        if (e.Exists(GlobalAuxiliary.NotZero))
	        {
		        // Calculate the strain slope
		        if (e[2] == 0)
			        theta1 = 0;

		        else if (Math.Abs(e[0] - e[1]) <= 1E-9 && e[2] < 0)
			        theta1 = -Constants.PiOver4;

		        else
			        //theta1 = 0.5 * Trig.Atan(e[2] / (e[0] - e[1]));
			        theta1 = Constants.PiOver2 - Trig.Atan(2 * (e[0] - ec2) / e[2]);
	        }

	        // Calculate theta2
	        double theta2 = Constants.PiOver2 - theta1;

	        //if (theta2 > Constants.PiOver2)
	        //	theta2 -= Constants.Pi;

	        return
		        (theta1, theta2);
        }

        public static (double theta1, double theta2) PrincipalAngles(Vector<double> strains)
        {
	        double theta1 = Constants.PiOver4;

	        // Get the strains
	        var e = strains;

	        // Verify the strains
	        if (e.Exists(GlobalAuxiliary.NotZero))
	        {
		        // Calculate the strain slope
		        if (e[2] == 0)
			        theta1 = 0;

		        else if (e[0] - e[1] == 0 && e[2] < 0)
			        theta1 = -Constants.PiOver4;

		        else
			        theta1 = 0.5 * Trig.Atan(e[2] / (e[0] - e[1]));

		        if (double.IsNaN(theta1))
			        theta1 = Constants.PiOver4;
	        }

	        // Calculate theta2
	        double theta2 = Constants.PiOver2 - theta1;

	        //if (theta2 > Constants.PiOver2)
	        //	theta2 -= Constants.Pi;

	        return
		        (theta1, theta2);
        }

        // Calculate principal strains
        public static (double e1, double e2) PrincipalStrains(Vector<double> strains)
        {
            // Get the strains
            var e = strains;

	        // Calculate radius and center of Mohr's Circle
	        double
		        cen = 0.5 * (e[0] + e[1]),
		        rad = 0.5 * Math.Sqrt((e[1] - e[0]) * (e[1] - e[0]) + e[2] * e[2]);

	        // Calculate principal strains in concrete
	        double
		        e1 = cen + rad,
		        e2 = cen - rad;

	        return
		        (e1, e2);
        }

        // Calculate stresses from principal
        public static Vector<double> StrainsFromPrincipal((double e1, double e2) principalStrains, double theta2)
        {
	        // Get principal stresses
	        var (e1, e2) = principalStrains;

	        // Calculate theta2 (fc2 angle)
	        var (cos, sin) = GlobalAuxiliary.DirectionCosines(2 * theta2);

	        // Calculate stresses by Mohr's Circle
	        double
		        cen  = 0.5 * (e1 + e2),
		        rad  = 0.5 * (e1 - e2),
		        ex   = cen - rad * cos,
		        ey   = cen + rad * cos,
		        exy  = 2 * rad * sin;

	        return
		        CreateVector.DenseOfArray(new[] { ex, ey, exy });
        }

        // Calculate stresses/strains transformation matrix
        // This matrix transforms from x-y to 1-2 coordinates
        public static Matrix<double> TransformationMatrix(double theta1) => StressRelations.TransformationMatrix(theta1);
    }
}
