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
        public class DSFM : Membrane
        {
            // Properties
            public Vector<double> ConcreteStrains { get; set; }
            public Vector<double> CrackSlipStrains { get; set; }

            // Constructor
            public DSFM(Concrete concrete, PanelReinforcement reinforcement, double panelWidth, double referenceLength) : base(concrete,
                reinforcement, panelWidth)
            {
                // Get concrete parameters
                double
                    fc    = concrete.Strength,
                    phiAg = concrete.AggregateDiameter;

                // Initiate new concrete
                Concrete = new Concrete.DSFM(fc, phiAg, referenceLength);
            }

            public override void Analysis(Vector<double> appliedStrains, int loadStep = 0)
            {
                // Get strains
                var e = appliedStrains;

                // Get concrete strains from last iteration
                Vector<double> ec;
                if (CrackSlipStrains != null)
                    ec = appliedStrains - CrackSlipStrains;
                else
                    ec = appliedStrains;

                // Calculate principal strains
                var (e1, e2) = PrincipalStrains(e);
                var (ec1, ec2) = PrincipalStrains(ec);

                // Calculate thetaC and thetaE
                var (thetaE1, _) = StrainAngles(e, (e1, e2));
                var (thetaC1, thetaC2) = StrainAngles(ec, (ec1, ec2));

                // Calculate reinforcement angles
                var (thetaNx, thetaNy) = ReinforcementAngles(thetaC1);

                // Calculate and set concrete and steel stresses
                Concrete.SetStrainsAndStresses((ec1, ec2), Reinforcement, (thetaNx, thetaNy));
                Reinforcement.SetStrainsAndStresses(appliedStrains);

                // Calculate concrete stiffness
                var Dc = Concrete_Stiffness(thetaC1);

                // Initiate crack slip strains and pseudo-prestress
                var es = Vector<double>.Build.Dense(3);
                var sig0 = Vector<double>.Build.Dense(3);

                // Verify if concrete is cracked
                if (Concrete.Cracked)
                {
                    // Calculate crack local stresses
                    var (_, _, vci) = CrackLocalStresses(thetaC1);

                    // Calculate crack slip strains
                    es = Crack_Slip_Strains(e, thetaE1, thetaC1, vci);

                    // Calculate pseudo-prestress
                    sig0 = PseudoPrestress(Dc, es);
                }

                // Set strain and stress states
                Strains = appliedStrains;
                CrackSlipStrains = es;
                ConcreteStrains = appliedStrains - es;
                PrincipalAngles = (thetaC1, thetaC2);
                ConcreteStresses = Concrete_Stresses(Dc, sig0, e);
                ReinforcementStresses = Reinforcement_Stresses();
            }

            // Calculate concrete stiffness matrix
            public override Matrix<double> Concrete_Stiffness(double thetaC1)
            {
                var (Ec1, Ec2) = Concrete.SecantModule;
                double Gc = Ec1 * Ec2 / (Ec1 + Ec2);

                // Concrete matrix
                var Dc1 = Matrix<double>.Build.Dense(3, 3);
                Dc1[0, 0] = Ec1;
                Dc1[1, 1] = Ec2;
                Dc1[2, 2] = Gc;

                // Get transformation matrix
                var T = Transformation_Matrix(thetaC1);

                // Calculate Dc
                return
                    T.Transpose() * Dc1 * T;
            }

            // Calculate stresses/strains transformation matrix
            // This matrix transforms from x-y to 1-2 coordinates
            public override Matrix<double> Transformation_Matrix(double thetaC1)
            {
                var (cos, sin) = GlobalAuxiliary.DirectionCosines(thetaC1);
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

            public override Vector<double> Concrete_Stresses(double theta)
            {
                throw new NotImplementedException();
            }

            // Calculate concrete stresses
            public Vector<double> Concrete_Stresses(Matrix<double> Dc, Vector<double> sig0, Vector<double> apparentStrains)
            {
                return
                    Dc * apparentStrains - sig0;
            }

            // Calculate crack local stresses
            private (double fscrx, double fscry, double vci) CrackLocalStresses(double thetaC1)
            {
                // Initiate stresses
                double
                    fscrx = 0,
                    fscry = 0,
                    vci = 0;

                // Get the strains
                double
                    ex = Strains[0],
                    ey = Strains[1];

                // Get concrete tensile stress
                double fc1 = Concrete.PrincipalStresses.fc1;

                // Get reinforcement angles and stresses
                var (thetaNx, thetaNy) = ReinforcementAngles(thetaC1);
                var (fsx, fsy) = Reinforcement.Stresses;

                // Calculate cosines and sines
                var (cosNx, sinNx) = GlobalAuxiliary.DirectionCosines(thetaNx);
                var (cosNy, sinNy) = GlobalAuxiliary.DirectionCosines(thetaNy);
                double
                    cosNx2 = cosNx * cosNx,
                    cosNy2 = cosNy * cosNy;

                // Function to check equilibrium
                Func<double, double> crackEquilibrium = de1crIt =>
                {
                    // Calculate local strains
                    double
                        escrx = ex + de1crIt * cosNx2,
                        escry = ey + de1crIt * cosNy2;

                    // Calculate reinforcement stresses
                    fscrx = Math.Min(escrx * Esxi, fyx);
                    fscry = Math.Min(escry * Esyi, fyy);

                    // Check equilibrium (must be zero)
                    double equil = psx * (fscrx - fsx) * cosNx2 + psy * (fscry - fsy) * cosNy2 - fc1;

                    return equil;
                };

                // Solve the nonlinear equation by Brent Method
                double de1cr;
                bool solution = Brent.TryFindRoot(crackEquilibrium, 1E-9, 0.01, 1E-6, 1000, out de1cr);

                // Verify if it reached convergence
                if (solution)
                {
                    // Calculate local strains
                    double
                        escrx = ex + de1cr * cosNx2,
                        escry = ey + de1cr * cosNy2;

                    // Calculate reinforcement stresses
                    fscrx = Math.Min(escrx * Esxi, fyx);
                    fscry = Math.Min(escry * Esyi, fyy);

                    // Calculate shear stress
                    vci = psx * (fscrx - fsx) * cosNx * sinNx + psy * (fscry - fsy) * cosNy * sinNy;
                }

                // Analysis must stop
                else
                    Stop = (true, "Equilibrium on crack not reached at step ");

                return (fscrx, fscry, vci);
            }

            // Calculate crack slip
            private Vector<double> Crack_Slip_Strains(Vector<double> apparentStrains, double thetaE1, double thetaC1, double vci)
            {
                // Get concrete principal tensile strain
                double ec1 = Concrete.PrincipalStrains.ec1;
                double fc = Concrete.Strength;

                // Get the strains
                double
                    ex = apparentStrains[0],
                    ey = apparentStrains[1],
                    yxy = apparentStrains[2];

                // Get the angles
                var (cosThetaC, sinThetaC) = GlobalAuxiliary.DirectionCosines(thetaC1);

                // Calculate crack spacings and width
                double s = 1 / (sinThetaC / smx + cosThetaC / smy);

                // Calculate crack width
                double w = ec1 * s;

                // Calculate shear slip strain by stress-based approach
                double
                    ds = vci / (1.8 * Math.Pow(w, -0.8) + (0.234 * Math.Pow(w, -0.707) - 0.2) * fc),
                    ysa = ds / s;

                // Calculate shear slip strain by rotation lag approach
                double
                    thetaIc = Constants.PiOver4,
                    dThetaE = thetaE1 - thetaIc,
                    thetaL = Trig.DegreeToRadian(5),
                    dThetaS;

                if (Math.Abs(dThetaE) > thetaL)
                    dThetaS = dThetaE - thetaL;

                else
                    dThetaS = dThetaE;

                double
                    thetaS = thetaIc + dThetaS;

                var (cos2ThetaS, sin2ThetaS) = GlobalAuxiliary.DirectionCosines(2 * thetaS);

                double ysb = yxy * cos2ThetaS + (ey - ex) * sin2ThetaS;

                // Calculate shear slip strains
                var (cos2ThetaC, sin2ThetaC) = GlobalAuxiliary.DirectionCosines(2 * thetaC1);

                double
                    ys = Math.Max(ysa, ysb),
                    exs = -ys / 2 * sin2ThetaC,
                    eys = ys / 2 * sin2ThetaC,
                    yxys = ys * cos2ThetaC;

                // Calculate the vector of shear slip strains
                return
                    Vector<double>.Build.DenseOfArray(new[] { exs, eys, yxys });
            }

            // Calculate the pseudo-prestress
            private Vector<double> PseudoPrestress(Matrix<double> Dc, Vector<double> es)
            {
                return
                    Dc * es;
            }

            // Set results
            public override void Results()
            {
                // Set results for stiffness
                TransformationMatrix = Transformation_Matrix(PrincipalAngles.theta1);
                ConcreteStiffness = Concrete_Stiffness(PrincipalAngles.theta1);
                ReinforcementStiffness = Reinforcement_Stiffness();
            }
        }
    }

}