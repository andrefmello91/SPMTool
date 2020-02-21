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
                ind[i] = 2 * grips[i] - 2;

            return ind;
        }

        // Simplify the stiffness matrix
        public static void SimplifyStiffnessMatrix(Matrix<double> Kg, Vector<double> f, Node[] nodes)
        {
            foreach (var nd in nodes)
            {
                // Get the index of the row
                int i = 2 * nd.Number - 2;

                // Simplify the matrices removing the rows that have constraints (external nodes)
                if (nd.Type == (int) Geometry.Node.NodeType.External)
                {
                    if (nd.SupportX)
                        // There is a support in this direction
                    {
                        // Clear the row and column [i] in the stiffness matrix (all elements will be zero)
                        Kg.ClearRow(i);
                        Kg.ClearColumn(i);

                        // Set the diagonal element to 1
                        Kg[i, i] = 1;

                        // Clear the row in the force vector
                        f[i] = 0;

                        // So ui = 0
                    }

                    if (nd.SupportY)
                        // There is a support in this direction
                    {
                        // Clear the row and column [i] in the stiffness matrix (all elements will be zero)
                        Kg.ClearRow(i + 1);
                        Kg.ClearColumn(i + 1);

                        // Set the diagonal element to 1
                        Kg[i + 1, i + 1] = 1;

                        // Clear the row in the force vector
                        f[i + 1] = 0;

                        // So ui = 0
                    }
                }
                
                // Simplification for internal nodes (There is only a displacement at the stringer direction, the perpendicular one will be zero)
                else
                {
                    // Verify rows i and i + 1
                    for (int j = i; j <= i + 1; j++)
                    {
                        // Verify what line of the matrix is composed of zeroes
                        if (!Kg.Row(j).Exists(Auxiliary.NotZero))
                        {
                            // The row is composed of only zeroes, so the displacement must be zero
                            // Set the diagonal element to 1
                            Kg[j, j] = 1;

                            // Clear the row in the force vector
                            f[j] = 0;
                        }
                    }
                }
            }
        }

        // Get the force vector
        public static Vector<double> ForceVector(Node[] nodes)
        {
            // Get the number of DoFs
            int numDofs = nodes.Length;

            // Initialize the force vector with size 2x number of DoFs (forces in x and y)
            var f = Vector<double>.Build.Dense(numDofs * 2);

            // Read the nodes data
            foreach (var nd in nodes)
            {
                // Check if it's a external node
                if (nd.Type == (int) Geometry.Node.NodeType.External && (nd.ForceX != 0 || nd.ForceY != 0))
                {
                    // Get the position in the vector
                    int i = 2 * nd.Number - 2;

                    // Read the forces in x and y (transform in N) and assign the values in the force vector at position (i) and (i + 1)
                    f[i]     = nd.ForceX * 1000;
                    f[i + 1] = nd.ForceY * 1000;
                }
            }

            return f;
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
                    var pnls   = Panel.Parameters(pnlObjs);
                    var nds     = Node.Parameters(ndObjs);

                    // Get the number of DoFs
                    int numDofs = 2 * nds.Length;

                    // Initialize the global stiffness matrix
                    var Kg = Matrix<double>.Build.Dense(numDofs, numDofs);

                    // Calculate the stiffness of each stringer and panel, add to the global stiffness and get the matrices of the stiffness of elements
                    Stringer.Linear.StringersStiffness(strs, Ec, Kg);
                    Panel.Linear.PanelsStiffness(pnls, Gc, Kg);

                    // Get the force vector and the constraints vector
                    var f = ForceVector(nds);

                    // Simplify the stiffness matrix
                    SimplifyStiffnessMatrix(Kg, f, nds);

                    // Solve the system
                    var u = Kg.Solve(f);

                    // Calculate the stringer, panel forces and nodal displacements
                    double fMax = Stringer.StringerForces(strs, u);
                    Panel.PanelForces(pnls, u);
                    Node.NodalDisplacements(nds, u);

                    // Draw results
                    Results.DrawStringerForces(strs, fMax);
                    Results.DrawPanelForces(pnls);
                    Results.DrawDisplacements(strs, nds);

                    // Write in a csv file (debug)
                    DelimitedWriter.Write("D:/SPMTooldataF.csv", f.ToColumnMatrix(), ";");
                    DelimitedWriter.Write("D:/SPMTooldataU.csv", u.ToColumnMatrix(), ";");
                    DelimitedWriter.Write("D:/SPMTooldataK.csv", Kg, ";");
                }
                else
                {
                    Application.ShowAlertDialog("Please set the material parameters.");
                }
            }
        }
    }
}
