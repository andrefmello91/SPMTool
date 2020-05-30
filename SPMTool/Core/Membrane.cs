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
        // Properties
        public Concrete                       Concrete               { get; set; }
        public PanelReinforcement             Reinforcement          { get; }
        public (bool S, string Message)       Stop                   { get; set; }
        public int                            LSCrack                { get; set; }
        public (int X, int Y)                 LSYield                { get; set; }
        public int                            LSPeak                 { get; set; }
        public Vector<double>                 Strains                { get; set; }
        public (double theta1, double theta2) PrincipalAngles        { get; set; }
        public Matrix<double>                 ConcreteStiffness      { get; set; }
        public Matrix<double>                 ReinforcementStiffness { get; set; }
        public Matrix<double>                 TransformationMatrix   { get; set; }
        public Vector<double>                 ConcreteStresses       { get; set; }
        public Vector<double>                 ReinforcementStresses  { get; set; }
        private int                           LoadStep               { get; set; }
        public int                            Iteration              { get; set; }

        // Constructor
        public Membrane(Concrete concrete, PanelReinforcement reinforcement, double panelWidth)
        {
            // Get reinforcement
            var diams = reinforcement.BarDiameter;
            var spcs  = reinforcement.BarSpacing;
            var steel = reinforcement.Steel;

            // Initiate new materials
            Reinforcement = new PanelReinforcement(diams, spcs, steel, panelWidth);

            // Set initial strains
            Strains = Vector<double>.Build.Dense(3);
        }

        // Get steel parameters
        private double fyx  => Reinforcement.Steel.X.YieldStress;
        private double Esxi => Reinforcement.Steel.X.ElasticModule;
        private double fyy  => Reinforcement.Steel.Y.YieldStress;
        private double Esyi => Reinforcement.Steel.Y.ElasticModule;

        // Get reinforcement
        private double phiX => Reinforcement.BarDiameter.X;
        private double phiY => Reinforcement.BarDiameter.Y;
        private double psx  => Reinforcement.Ratio.X;
        private double psy  => Reinforcement.Ratio.Y;

        // Calculate crack spacings
        private double smx => phiX / (5.4 * psx);
        private double smy => phiY / (5.4 * psy);

        public abstract void Analysis(Vector<double> appliedStrains, int loadStep = 0);

        // Calculate tensile strain angle
        public (double theta1, double theta2) StrainAngles(Vector<double> strains, (double ec1, double ec2) principalStrains)
        {
            double theta1 = Constants.PiOver4;

            // Get the strains
            var e = strains;
            var ec2 = principalStrains.ec2;

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

        // Calculate principal strains
        public (double ec1, double ec2) PrincipalStrains(Vector<double> strains)
        {
            // Get the strains
            var e = strains;

            // Calculate radius and center of Mohr's Circle
            double
                cen = 0.5 * (e[0] + e[1]),
                rad = 0.5 * Math.Sqrt((e[1] - e[0]) * (e[1] - e[0]) + e[2] * e[2]);

            // Calculate principal strains in concrete
            double
                ec1 = cen + rad,
                ec2 = cen - rad;

            return
                (ec1, ec2);
        }

        // Get current Stiffness
        public Matrix<double> Stiffness
        {
            get
            {
                // Check if strains are set
                if (Strains != null || Strains.Exists(GlobalAuxiliary.NotZero))
                    return
                        ConcreteStiffness + ReinforcementStiffness;

                // Calculate initial stiffness
                return
                    InitialStiffness;
            }
        }

        // Get current stresses
        public Vector<double> Stresses
        {
            get
            {
                if (Strains != null)
                    return
                        ConcreteStresses + ReinforcementStresses;

                return
                    CreateVector.DenseOfArray(new double[] { 0, 0, 0 });
            }
        }

        // Calculate initial stiffness
        public Matrix<double> InitialConcreteStiffness
        {
            get
            {
                // Concrete matrix
                double Ec = Concrete.Ec;
                var Dc1 = Matrix<double>.Build.Dense(3, 3);
                Dc1[0, 0] = Ec;
                Dc1[1, 1] = Ec;
                Dc1[2, 2] = 0.5 * Ec;

                // Get transformation matrix
                var T = Transformation_Matrix(Constants.PiOver4);

                // Calculate Dc
                return
                    T.Transpose() * Dc1 * T;
            }
        }

        // Initial reinforcement stiffness
        public Matrix<double> InitialReinforcementStiffness
        {
            get
            {
                // Steel matrix
                var Ds = Matrix<double>.Build.Dense(3, 3);
                Ds[0, 0] = psx * Esxi;
                Ds[1, 1] = psy * Esyi;

                return Ds;
            }
        }

        // Calculate initial stiffness
        public Matrix<double> InitialStiffness => InitialConcreteStiffness + InitialReinforcementStiffness;

        // Calculate stiffness
        public Matrix<double> Stiffness_(double theta)
        {
            return
                Concrete_Stiffness(theta) + Reinforcement_Stiffness();
        }

        // Calculate steel stiffness matrix
        public Matrix<double> Reinforcement_Stiffness()
        {
            // Calculate secant module
            var (Esx, Esy) = Reinforcement.SecantModule;

            // Steel matrix
            var Ds = Matrix<double>.Build.Dense(3, 3);
            Ds[0, 0] = psx * Esx;
            Ds[1, 1] = psy * Esy;

            return Ds;
        }

        // Calculate concrete stiffness matrix
        public abstract Matrix<double> Concrete_Stiffness(double theta);

        // Calculate stresses/strains transformation matrix
        // This matrix transforms from x-y to 1-2 coordinates
        public abstract Matrix<double> Transformation_Matrix(double theta);

        // Calculate concrete stresses
        public abstract Vector<double> Concrete_Stresses(double theta);

        // Get reinforcement stresses as a vector multiplied by reinforcement ratio
        public Vector<double> Reinforcement_Stresses()
        {
            var (fsx, fsy) = Reinforcement.Stresses;

            return
                CreateVector.DenseOfArray(new[] { psx * fsx, psy * fsy, 0 });
        }

        // Calculate stresses
        public Vector<double> Stresses_(double theta)
        {
            return
                Concrete_Stresses(theta) + Reinforcement_Stresses();
        }

        // Set results
        public abstract void Results();

        // Calculate slopes related to reinforcement
        private (double X, double Y) ReinforcementAngles(double theta1)
        {
            // Calculate angles
            double
                thetaNx = theta1,
                thetaNy = theta1 - Constants.PiOver2;

            return
                (thetaNx, thetaNy);
        }

        // Crack check
        // Crack check procedure
        public double CrackCheck(double theta2)
        {
            // Get the values
            double ec1 = Concrete.PrincipalStrains.ec1;
            var (fsx, fsy) = Reinforcement.Stresses;
            double fc = Concrete.fc;
            double f1a = Concrete.PrincipalStresses.fc1;
            double phiAg = Concrete.AggregateDiameter;

            // Calculate thetaC sine and cosine
            var (cosTheta, sinTheta) = GlobalAuxiliary.DirectionCosines(theta2);
            double tanTheta = GlobalAuxiliary.Tangent(theta2);

            // Average crack spacing and opening
            double
                smTheta = 1 / (sinTheta / smx + cosTheta / smy),
                w = smTheta * ec1;

            // Reinforcement capacity reserve
            double
                f1cx = psx * (fyx - fsx),
                f1cy = psy * (fyy - fsy);

            // Maximum possible shear on crack interface
            double vcimaxA = 0.18 * Math.Sqrt(fc) / (0.31 + 24 * w / (phiAg + 16));

            // Maximum possible shear for biaxial yielding
            double vcimaxB = Math.Abs(f1cx - f1cy) / (tanTheta + 1 / tanTheta);

            // Maximum shear on crack
            double vcimax = Math.Min(vcimaxA, vcimaxB);

            // Biaxial yielding condition
            double f1b = f1cx * sinTheta * sinTheta + f1cy * cosTheta * cosTheta;

            // Maximum tensile stress for equilibrium in X and Y
            double
                f1c = f1cx + vcimax / tanTheta,
                f1d = f1cy + vcimax * tanTheta;

            // Calculate the minimum tensile stress
            var f1List = new[] { f1a, f1b, f1c, f1d };
            var fc1 = f1List.Min();

            // Set to concrete
            if (fc1 < f1a)
                Concrete.SetTensileStress(fc1);

            // Calculate critical stresses on crack
            StressesOnCrack();
            void StressesOnCrack()
            {
                // Initiate vci = 0 (for most common cases)
                double vci = 0;

                if (f1cx > f1cy && f1cy < fc1) // Y dominant
                    vci = (fc1 - f1cy) / tanTheta;

                if (f1cx < f1cy && f1cx < fc1) // X dominant
                    vci = (f1cx - fc1) * tanTheta;

                // Reinforcement stresses
                double
                    fsxcr = (fc1 + vci / tanTheta) / psx + fsx,
                    fsycr = (fc1 + vci * tanTheta) / psy + fsy;

                // Check if reinforcement yielded at crack
                int
                    lsYieldX = 0,
                    lsYieldY = 0;

                if (LSYield.X == 0 && fsxcr >= fyx)
                    lsYieldX = LoadStep;

                if (LSYield.Y == 0 && fsycr >= fyy)
                    lsYieldY = LoadStep;

                LSYield = (lsYieldX, lsYieldY);
            }

            return fc1;
        }
    }
}