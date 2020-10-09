using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Extensions.AutoCAD;
using SPM.Elements;
using SPM.Elements.StringerProperties;
using SPMTool.Database;
using SPMTool.Database.Elements;
using SPMTool.Editor.Commands;
using SPMTool.Enums;

[assembly: CommandClass(typeof(ElementEditor))]

namespace SPMTool.Editor.Commands
{
    /// <summary>
    /// Element editor command class.
    /// </summary>
    public static class ElementEditor
    {
		/// <summary>
        /// Divide a stringer into new ones.
        /// </summary>
	    [CommandMethod("DivideStringer")]
	    public static void DivideStringer()
	    {
		    // Get units
		    var units = DataBase.Units;

		    // Prompt for select stringers
		    var strs = UserInput.SelectStringers("Select stringers to divide");

		    if (strs is null)
			    return;

		    // Prompt for the number of segments
		    var numn = UserInput.GetInteger("Enter the number of new stringers:", 2);

		    if (!numn.HasValue)
			    return;

		    int num = numn.Value;

		    // Get the list of start and endpoints
		    var stringerCollection = Stringers.StringerGeometries();

		    // Create lists of points for adding the nodes later
		    List<Point3d> newIntNds = new List<Point3d>(),
			    newExtNds = new List<Point3d>();

		    // Access the internal nodes in the model
		    using (var intNds = Model.GetObjectsOnLayer(Layer.IntNode).ToDBObjectCollection())
			    foreach (Line str in strs)
			    {
				    // Access the XData as an array
				    var data = str.ReadXData();

				    // Get the coordinates of the initial and end points
				    Point3d
					    strSt  = str.StartPoint,
					    strEnd = str.EndPoint;

				    // Calculate the distance of the points in X and Y
				    double
					    distX = (strEnd.X - strSt.X) / num,
					    distY = (strEnd.Y - strSt.Y) / num;

				    // Initialize the start point
				    var stPt = strSt;

				    // Get the midpoint
				    var midPt = strSt.MidPoint(strEnd);

				    // Read the internal nodes to erase
				    using (var ndsToErase = new ObjectIdCollection((from DBPoint intNd in intNds
					    where intNd.Position.Approx(midPt)
					    select intNd.ObjectId).ToArray()))
					    Model.EraseObjects(ndsToErase);

				    // Create the new stringers
				    for (int i = 1; i <= num; i++)
				    {
					    // Get the coordinates of the other points
					    double
						    xCrd = str.StartPoint.X + i * distX,
						    yCrd = str.StartPoint.Y + i * distY;

					    var endPt = new Point3d(xCrd, yCrd, 0);

					    // Create the Stringer
					    using (var strLine = new Line(stPt, endPt))
					    {
						    Stringers.Add(strLine, ref stringerCollection);

						    // Append the XData of the original Stringer
						    strLine.XData = new ResultBuffer(data);
					    }

					    // Get the mid point
					    midPt = stPt.MidPoint(endPt);

					    // Add the position of the nodes to the list
					    if (!newExtNds.Contains(stPt))
						    newExtNds.Add(stPt);

					    if (!newExtNds.Contains(endPt))
						    newExtNds.Add(endPt);

					    if (!newIntNds.Contains(midPt))
						    newIntNds.Add(midPt);

					    // Set the start point of the next Stringer
					    stPt = endPt;
				    }

				    // Erase the original Stringer
				    str.UpgradeOpen();
				    str.Erase();

				    // Remove from the list
				    var strList = stringerCollection.ToList();
				    strList.Remove(new StringerGeometry(strSt, strEnd, 0, 0));
				    stringerCollection = strList;
			    }

		    // Erase original stringers
		    Model.EraseObjects(strs);

		    // Create the nodes
		    Nodes.Add(newExtNds, NodeType.External);
		    Nodes.Add(newIntNds, NodeType.Internal);

		    // Update nodes and stringers
		    Nodes.Update(units.Geometry);
		    Stringers.UpdateStringers();
	    }

		/// <summary>
        /// Set geometry to a selection of stringers.
        /// </summary>
	    [CommandMethod("SetStringerGeometry")]
	    public static void SetStringerGeometry()
	    {
		    // Read units
		    var units = DataBase.Units;

		    // Request objects to be selected in the drawing area
		    var strs = UserInput.SelectStringers("Select the stringers to assign properties (you can select other elements, the properties will be only applied to stringers)");

		    if (strs is null)
			    return;

		    // Get geometry
		    var geometryn = UserInput.GetStringerGeometry(units.Geometry);

		    if (!geometryn.HasValue)
			    return;

		    var geometry = geometryn.Value;

		    // Start a transaction
		    foreach (DBObject obj in strs)
		    {
			    // Access the XData as an array
			    var data = obj.ReadXData();

			    // Set the new geometry and reinforcement (line 7 to 9 of the array)
			    data[(int) StringerIndex.Width]  = new TypedValue((int) DxfCode.ExtendedDataReal, geometry.Width);
			    data[(int) StringerIndex.Height] = new TypedValue((int) DxfCode.ExtendedDataReal, geometry.Height);

			    // Add the new XData
			    obj.SetXData(data);
		    }
	    }
    }
}
