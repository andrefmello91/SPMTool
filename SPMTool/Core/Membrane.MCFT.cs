using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.RootFinding;
using SPMTool.Material;

namespace SPMTool.Core
{
    public abstract partial class Membrane
    {
        public class MCFT : Membrane
        {
            // Constructor
            public MCFT(Concrete concrete, PanelReinforcement reinforcement, double panelWidth) : base(concrete, reinforcement, panelWidth)
            {
                // Get concrete parameters
                double
                    fc    = concrete.fc,
                    phiAg = concrete.AggregateDiameter;

                // Initiate new concrete
                Concrete = new Concrete.MCFT(fc, phiAg);
            }

            // Tolerances
            private double fTol = 1E-3;
            private double eTol = 1E-9;

            // Do analysis by MCFT with applied strains
            public override void Analysis(Vector<double> appliedStrains, int loadStep = 0)
            {
                // Calculate new principal strains
                var (ec1, ec2) = PrincipalStrains(appliedStrains);
                double theta2 = StrainAngles(appliedStrains, (ec1, ec2)).theta2;

                // Calculate and set concrete and steel stresses
                Concrete.SetStrainsAndStresses((ec1, ec2));
                Reinforcement.SetStrainsAndStresses(appliedStrains);

                // Verify if concrete is cracked and check crack stresses to limit fc1
                if (Concrete.Cracked)
                    CrackCheck(theta2);

                // Set strain and stress states
                Strains = appliedStrains;
                PrincipalAngles = (Constants.PiOver2 - theta2, theta2);
                ConcreteStresses = Concrete_Stresses(theta2);
                ReinforcementStresses = Reinforcement_Stresses();
            }

            // Check convergence
            private bool CheckConvergence(Vector<double> residualStrain, Vector<double> residualStress)
            {
                // Calculate maximum residuals
                double
                    erMax = residualStrain.AbsoluteMaximum(),
                    frMax = residualStress.AbsoluteMaximum();

                if (erMax <= eTol && frMax <= fTol)
                    return true;

                return false;
            }

            // Calculate concrete stiffness matrix
            public override Matrix<double> Concrete_Stiffness(double theta2)
            {
                var (Ec1, Ec2) = Concrete.SecantModule;
                double Gc = Ec1 * Ec2 / (Ec1 + Ec2);

                // Concrete matrix
                var Dc1 = Matrix<double>.Build.Dense(3, 3);
                Dc1[0, 0] = Ec2;
                Dc1[1, 1] = Ec1;
                Dc1[2, 2] = Gc;

                // Get transformation matrix
                var T = Transformation_Matrix(theta2);

                // Calculate Dc
                return
                    T.Transpose() * Dc1 * T;
            }

            // Calculate stresses/strains transformation matrix
            // This matrix transforms from x-y to 1-2 coordinates
            public override Matrix<double> Transformation_Matrix(double theta2)
            {
                double psi = Constants.Pi - theta2;
                var (cos, sin) = GlobalAuxiliary.DirectionCosines(psi);
                double
                    cos2 = cos * cos,
                    sin2 = sin * sin,
                    cosSin = cos * sin;

                return
                    Matrix<double>.Build.DenseOfArray(new[,]
                    {
                        {        cos2,       sin2,      cosSin },
                        {        sin2,       cos2,     -cosSin },
                        { -2 * cosSin, 2 * cosSin, cos2 - sin2 }
                    });
            }

            // Calculate concrete stresses
            public override Vector<double> Concrete_Stresses(double theta2)
            {
                // Get principal stresses
                var (fc1, fc2) = Concrete.PrincipalStresses;

                // Calculate theta2 (fc2 angle)
                var (cos, sin) = GlobalAuxiliary.DirectionCosines(2 * theta2);

                // Calculate stresses by Mohr's Circle
                double
                    cen  = 0.5 * (fc1 + fc2),
                    rad  = 0.5 * (fc1 - fc2),
                    fcx  = cen - rad * cos,
                    fcy  = cen + rad * cos,
                    vcxy = rad * sin;

                return
                    CreateVector.DenseOfArray(new[] { fcx, fcy, vcxy });
            }

            // Set results
            public override void Results()
            {
                // Set results for stiffness
                TransformationMatrix = Transformation_Matrix(PrincipalAngles.theta2);
                ConcreteStiffness = Concrete_Stiffness(PrincipalAngles.theta2);
                ReinforcementStiffness = Reinforcement_Stiffness();
            }

            // Calculate tensile principal strain by equilibrium in a crack
            private double CrackEquilibrium()
            {
                // Get the values
                double ec1 = Concrete.PrincipalStrains.ec1;
                double theta = PrincipalAngles.theta2;
                var (fsx, fsy) = Reinforcement.Stresses;
                double fc = Concrete.fc;
                double fcr = Concrete.fcr;
                double phiAg = Concrete.AggregateDiameter;

                // Constitutive relation
                double f1a = fcr / (1 + Math.Sqrt(500 * ec1));

                // Calculate thetaC sine and cosine
                var (cosTheta, sinTheta) = GlobalAuxiliary.DirectionCosines(theta);
                double
                    tanTheta = GlobalAuxiliary.Tangent(theta),
                    cosTheta2 = cosTheta * cosTheta,
                    sinTheta2 = sinTheta * sinTheta;

                // Average crack spacing and opening
                double
                    smTheta = 1 / (sinTheta / smx + cosTheta / smy),
                    w = smTheta * ec1;

                // Calculate maximum shear stress on crack
                double vcimax = Math.Sqrt(fc) / (0.31 + 24 * w / (phiAg + 16));

                // Equilibrium Systems
                double EquilibriumSystem1()
                {
                    double vci = (psx * (fyx - fsx) - psy * (fyy - fsy)) * sinTheta * cosTheta;

                    // Calculate fci
                    double fci = 0;

                    if (Math.Abs(vci) >= 0.18 * vcimax)
                        fci = vcimax * (1 - Math.Sqrt(1.22 * (1 - Math.Abs(vci) / vcimax)));

                    return
                        psx * (fyx - fsx) * sinTheta2 + psy * (fyy - fsy) * cosTheta2 - fci;
                }

                double EquilibriumSystem2()
                {
                    return
                        psx * (fyx - fsx) - vcimax * (1 / tanTheta + 1);
                }

                double EquilibriumSystem3()
                {
                    return
                        psx * (fyx - fsx) + vcimax * (1 / tanTheta - 1);
                }

                double EquilibriumSystem4()
                {
                    return
                        psy * (fyy - fsy) + vcimax * (tanTheta - 1);
                }

                double EquilibriumSystem5()
                {
                    return
                        psy * (fyy - fsy) - vcimax * (tanTheta + 1);
                }

                // Create a list and add all the equilibrium systems
                var f1List = new List<double>();
                f1List.Add(EquilibriumSystem1());
                f1List.Add(EquilibriumSystem2());
                f1List.Add(EquilibriumSystem3());
                f1List.Add(EquilibriumSystem4());
                f1List.Add(EquilibriumSystem5());

                // Get maximum value
                return
                    f1List.Max();
            }
        }
    }

}