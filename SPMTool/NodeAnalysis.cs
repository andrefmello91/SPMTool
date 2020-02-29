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
using MathNet.Numerics.Statistics;

namespace SPMTool
{
    partial class Analysis
    {
        public class Node
        {
            // Properties
            public ObjectId             ObjectId     { get; set; }
            public int                  Number       { get; set; }
            public int                  Type         { get; set; }
            public Point3d              Position     { get; set; }
            public (bool X, bool Y)     Support      { get; set; }
            public (double X, double Y) Force        { get; set; }
            public (double X, double Y) Displacement { get; set; }

            // Constructor
            public Node()
            {
                ObjectId = ObjectId;
                Number = Number;
                Type = Type;
                Position = Position;
                Support = Support;
                Force = Force;
                Displacement = Displacement;
            }

            // Read the parameters of nodes
            public static Node[] Parameters(ObjectIdCollection nodeObjects)
            {
                Node[] nodes = new Node[nodeObjects.Count];

                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId ndObj in nodeObjects)
                    {
                        // Read the object as a point
                        DBPoint ndPt = trans.GetObject(ndObj, OpenMode.ForRead) as DBPoint;

                        // Read the XData and get the necessary data
                        ResultBuffer rb = ndPt.GetXDataForApplication(AutoCAD.appName);
                        TypedValue[] data = rb.AsArray();

                        // Get the node number
                        int num = Convert.ToInt32(data[(int) XData.Node.Number].Value);

                        // Get type
                        int type;
                        if (ndPt.Layer == Layers.extNode)
                            type = (int)Geometry.Node.NodeType.External;
                        else
                            type = (int)Geometry.Node.NodeType.Internal;

                        // Get support conditions
                        bool 
                            supX = false, 
                            supY = false;

                        string support = data[(int) XData.Node.Support].Value.ToString();

                        if (support.Contains("X"))
                            supX = true;

                        if (support.Contains("Y"))
                            supY = true;

                        // Get forces
                        double
                            Fx = Convert.ToDouble(data[(int) XData.Node.Fx].Value),
                            Fy = Convert.ToDouble(data[(int) XData.Node.Fy].Value);

                        // Set the values
                        int i = num - 1;
                        nodes[i] = new Node
                        {
                            ObjectId = ndObj,
                            Number   = num,
                            Type     = type,
                            Position = ndPt.Position,
                            Support = (supX, supY),
                            Force   = (Fx, Fy)
                        };
                    }

                    // Return the parameters
                    return nodes;
                }
            }

            // Get the nodal displacements and save to XData
            public static void NodalDisplacements(Node[] nodes, Vector<double> u)
            {
                // Start a transaction
                using (Transaction trans = AutoCAD.curDb.TransactionManager.StartTransaction())
                {
                    // Get the stringers stifness matrix and add to the global stifness matrix
                    foreach (var nd in nodes)
                    {
                        // Get the index of the node on the list
                        int i = 2 * nd.Number - 2;

                        // Get the displacements
                        double 
                            ux = Math.Round(u[i], 6),
                            uy = Math.Round(u[i + 1], 6);

                        // Save to the node
                        nd.Displacement = (ux, uy);

                        // Read the object of the node as a point
                        DBPoint ndPt = trans.GetObject(nd.ObjectId, OpenMode.ForWrite) as DBPoint;

                        // Get the result buffer as an array
                        ResultBuffer rb   = ndPt.GetXDataForApplication(AutoCAD.appName);
                        TypedValue[] data = rb.AsArray();

                        // Save the displacements on the XData
                        data[(int)XData.Node.Ux] = new TypedValue((int)DxfCode.ExtendedDataReal, ux);
                        data[(int)XData.Node.Uy] = new TypedValue((int)DxfCode.ExtendedDataReal, uy);

                        // Add the new XData
                        ndPt.XData = new ResultBuffer(data);
                    }

                    // Commit changes
                    trans.Commit();
                }
            }
        }
    }
}
