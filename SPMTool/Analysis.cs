using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using MathNet.Numerics.LinearAlgebra;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics.Data.Text;

[assembly: CommandClass(typeof(SPMTool.Analysis))]
[assembly: CommandClass(typeof(SPMTool.Analysis.Linear))]

namespace SPMTool
{
    public partial class Analysis
    {
        // Get the indexes of an element grips in the global matrix
        public static int[] GlobalIndexes(int[] grips)
        {
            // Initialize the array
            int[] ind = new int[grips.Length];

            // Get the indexes
            for (int i = 0; i < grips.Length; i++)
                ind[i] = 2 * (grips[i] - 1);

            return ind;
        }

        // Simplify the stiffness matrix
        public static void SimplifyStiffnessMatrix(Matrix<double> Kg, Vector<double> f, List<Point3d> allNds, IEnumerable<Tuple<int, double>> constraints)
        {
            // Get the list of internal nodes
            List<Point3d> intNds = Geometry.Node.ListOfNodes((int)Geometry.Node.NodeType.Internal);

            // Simplify the matrices removing the rows that have constraints
            foreach (Tuple<int, double> con in constraints)
            {
                // Simplification by the constraints
                if (con.Item2 == 0) // There is a support in this direction
                {
                    // Get the index of the row
                    int i = con.Item1;

                    // Clear the row and column [i] in the stiffness matrix (all elements will be zero)
                    Kg.ClearRow(i);
                    Kg.ClearColumn(i);

                    // Set the diagonal element to 1
                    Kg[i, i] = 1;

                    // Clear the row in the force vector
                    f[i] = 0;

                    // So ui = 0
                }
            }

            // Simplification for internal nodes (There is only a displacement at the stringer direction, the perpendicular one will be zero)
            foreach (Point3d intNd in intNds)
            {
                // Get the index of the global matrix
                int i = 2 * allNds.IndexOf(intNd);

                // Verify what line of the matrix is composed of zeroes
                if (!Kg.Row(i).Exists(Auxiliary.NotZero))
                {
                    // The row is composed of only zeroes, so the displacement must be zero
                    // Set the diagonal element to 1
                    Kg[i, i] = 1;

                    // Clear the row in the force vector
                    f[i] = 0;
                }

                if (!Kg.Row(i + 1).Exists(Auxiliary.NotZero))
                {
                    // The row is composed of only zeroes, so the displacement must be zero
                    // Set the diagonal element to 1
                    Kg[i + 1, i + 1] = 1;

                    // Clear the row in the force vector
                    f[i + 1] = 0;
                }
                // Else nothing is done
            }
        }

        // Linear analysis methods
        public class Linear
        {
            [CommandMethod("DoLinearAnalysis")]
            public static void DoLinearAnalysis()
            {
                // Get the concrete parameters
                var concrete = Material.Concrete.ConcreteParams();

                // Verify if concrete parameters were set
                if (concrete != null)
                {
                    // Get the elastic modulus
                    double Ec = concrete.Eci;

                    // Calculate the approximated shear modulus (elastic material)
                    double Gc = Ec / 2.4;

                    // Update and get the elements collection
                    ObjectIdCollection
                        ndObjs  = Geometry.Node.UpdateNodes(),
                        strObjs = Geometry.Stringer.UpdateStringers(),
                        pnlObjs = Geometry.Panel.UpdatePanels();

                    // Get the initial element parameters
                    var strs = Stringer.Parameters(strObjs);
                    var pnls = Panel.Parameters(pnlObjs);

                    // Get the list of node positions
                    List<Point3d> ndList = Geometry.Node.ListOfNodes((int)Geometry.Node.NodeType.All);

                    // Initialize the global stiffness matrix
                    var Kg = Matrix<double>.Build.Dense(2 * ndObjs.Count, 2 * ndObjs.Count);

                    // Calculate the stiffness of each stringer and panel, add to the global stiffness and get the matrices of the stiffness of elements
                    Stringer.Linear.StringersStiffness(strs, Ec, Kg);
                    Panel.Linear.PanelsStiffness(pnls, Gc, Kg);

                    // Get the force vector and the constraints vector
                    var f = Forces.ForceVector();
                    var cons = Constraints.ConstraintList();

                    // Simplify the stifness matrix
                    SimplifyStiffnessMatrix(Kg, f, ndList, cons);

                    // Solve the sistem
                    var u = Kg.Solve(f);

                    // Calculate the stringer, panel forces and nodal displacements
                    Stringer.StringerForces(strs, u);
                    Panel.PanelForces(pnls, u);
                    Results.NodalDisplacements(ndObjs, strObjs, ndList, u);

                    // Write in a csv file (debug)
                    //DelimitedWriter.Write("D:/SPMTooldataF.csv", f.ToColumnMatrix(), ";");
                    //DelimitedWriter.Write("D:/SPMTooldataU.csv", u.ToColumnMatrix(), ";");
                    //DelimitedWriter.Write("D:/SPMTooldataK.csv", Kg, ";");
                }
                else
                {
                    Application.ShowAlertDialog("Please set the material parameters.");
                }
            }
        }
    }
}
