using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using andrefmello91.Extensions;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using MathNet.Numerics;
using static Autodesk.AutoCAD.ApplicationServices.Core.Application;

#nullable enable

namespace SPMTool
{
	public static partial class Extensions
	{

		#region Methods

		/// <summary>
		///     Add a <paramref name="dbObject" /> to the drawing and return it's <see cref="ObjectId" />.
		/// </summary>
		/// <param name="document">The document to add the object.</param>
		/// <param name="dbObject">The <see cref="Entity" />.</param>
		/// <param name="erasedEvent">The event to call if <paramref name="dbObject" /> is erased.</param>
		public static ObjectId AddObject(this Document document, DBObject? dbObject, ObjectErasedEventHandler? erasedEvent = null)
		{
			if (dbObject is null)
				return ObjectId.Null;

			// Start a transaction
			using var lck   = document.LockDocument();
			using var trans = document.TransactionManager.StartTransaction();

			// Open the Block table for read
			var blkTbl = (BlockTable) trans.GetObject(document.Database.BlockTableId, OpenMode.ForRead);

			// Open the Block table record Model space for write
			var blkTblRec = (BlockTableRecord) trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

			// Add the object to the drawing
			var entity = (Entity) dbObject;

			blkTblRec.AppendEntity(entity);
			trans.AddNewlyCreatedDBObject(entity, true);

			if (erasedEvent != null)
				dbObject.Erased += erasedEvent;

			// Commit changes
			trans.Commit();

			return
				dbObject.ObjectId;
		}

		/// <summary>
		///     Add the <paramref name="dbObjects" /> in this collection to the drawing and return the collection of
		///     <see cref="ObjectId" />'s.
		/// </summary>
		/// <param name="document">The document to add the object.</param>
		/// <param name="dbObjects">The collection of objects to add to drawing.</param>
		/// <param name="erasedEvent">The event to call if <paramref name="dbObjects" /> are erased.</param>
		public static IEnumerable<ObjectId> AddObjects(this Document document, [NotNull] IEnumerable<DBObject?> dbObjects, ObjectErasedEventHandler? erasedEvent = null)
		{
			// Start a transaction
			using var lck   = document.LockDocument();
			using var trans = document.Database.TransactionManager.StartTransaction();

			// Open the Block table for read
			var blkTbl = (BlockTable) trans.GetObject(document.Database.BlockTableId, OpenMode.ForRead);

			// Open the Block table record Model space for write
			var blkTblRec = (BlockTableRecord) trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

			// Add the objects to the drawing
			var list = new List<ObjectId>();
			foreach (var obj in dbObjects)
			{
				var ent = (Entity?) obj;

				if (ent is not null)
				{
					blkTblRec.AppendEntity(ent);
					trans.AddNewlyCreatedDBObject(ent, true);

					if (erasedEvent != null)
						ent.Erased += erasedEvent;
				}

				list.Add(ent?.ObjectId ?? ObjectId.Null);
			}

			// Commit changes
			trans.Commit();

			return list;
		}

		/// <summary>
		///     Create a group in the database.
		/// </summary>
		/// <param name="document">The document to add the objects.</param>
		/// <param name="groupEntities">The collection of <see cref="Entity" />'s that form the group.</param>
		/// <param name="groupName">The <see cref="Group" />.</param>
		/// <inheritdoc cref="AddObject" />
		public static ObjectId AddObjectsAsGroup(this Document document, IEnumerable<Entity> groupEntities, string groupName)
		{
			using var lck   = document.LockDocument();
			using var trans = document.Database.TransactionManager.StartTransaction();

			// Open the nod
			using var nod = (DBDictionary) trans.GetObject(document.Database.NamedObjectsDictionaryId, OpenMode.ForWrite);

			// Open the Block table for read
			using var blkTbl = (BlockTable) trans.GetObject(document.Database.BlockTableId, OpenMode.ForRead);

			// Open the Block table record Model space for write
			using var blkTblRec = (BlockTableRecord) trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

			// Create the group
			using var group = new Group(groupName, true);
			var       id    = nod.SetAt(groupName, group);

			// Add to the transaction
			trans.AddNewlyCreatedDBObject(group, true);

			// Add the elements to the block
			foreach (var ent in groupEntities)
			{
				blkTblRec.AppendEntity(ent);
				trans.AddNewlyCreatedDBObject(ent, true);
			}

			// Set to group
			using var ids = new ObjectIdCollection(groupEntities.Select(e => e.ObjectId).ToArray());
			group.InsertAt(0, ids);

			// Commit changes
			trans.Commit();

			return id;
		}

		/// <summary>
		///     Get the approximated center <see cref="Point3d" /> of a rectangular <paramref name="solid" />.
		/// </summary>
		public static Point3d CenterPoint(this Solid solid)
		{
			var verts = solid.GetVertices().ToArray();

			if (verts.Length != 4)
				throw new NotImplementedException();

			var mid1 = verts[0].MidPoint(verts[3]);
			var mid2 = verts[1].MidPoint(verts[2]);

			return mid1.MidPoint(mid2);
		}

		/// <summary>
		///     Create a block in the database.
		/// </summary>
		/// <param name="document">The document to add the object.</param>
		/// <param name="blockEntities">The collection of <see cref="Entity" />'s that form the block.</param>
		/// <param name="originPoint">The origin point of the block.</param>
		/// <param name="blockName">The name to save the block in database.</param>
		public static void CreateBlock(this Document document, IEnumerable<Entity> blockEntities, Point3d originPoint, string blockName)
		{
			using var lck   = document.LockDocument();
			using var trans = document.Database.TransactionManager.StartTransaction();

			// Open the Block table for read
			using var blkTbl = (BlockTable) trans.GetObject(document.Database.BlockTableId, OpenMode.ForRead);

			// Check if the support blocks already exist in the drawing
			if (blkTbl.Has(blockName))
				return;

			// Create the X block
			using var blkTblRec = new BlockTableRecord
			{
				Name = blockName
			};

			// Add the block table record to the block table and to the transaction
			blkTbl.UpgradeOpen();
			blkTbl.Add(blkTblRec);
			trans.AddNewlyCreatedDBObject(blkTblRec, true);

			// Set the insertion point for the block
			blkTblRec.Origin = originPoint;

			// Add the elements to the block
			foreach (var ent in blockEntities)
			{
				blkTblRec.AppendEntity(ent);
				trans.AddNewlyCreatedDBObject(ent, true);
			}

			// Commit changes
			trans.Commit();
		}

		/// <summary>
		///     Divide a <paramref name="line" /> in a <paramref name="number" /> of new ones.
		/// </summary>
		/// <param name="number">The number of lines to return.</param>
		public static IEnumerable<Line> Divide(this Line line, int number)
		{
			// Get the coordinates of the initial and end points
			Point3d
				start = line.StartPoint,
				end   = line.EndPoint;

			// Calculate the distance of the points in X and Y
			double
				distX = end.DistanceInX(start) / number,
				distY = end.DistanceInY(start) / number;

			// Create the new lines
			for (var i = 0; i < number; i++)
			{
				// Get the coordinates of the other points
				double
					xCrd = start.X + distX,
					yCrd = start.Y + distY;

				var endPt = new Point3d(xCrd, yCrd, 0);

				// Create the line
				yield return new Line(start, endPt) { XData = line.XData };

				// Set the start point of the next line
				start = endPt;
			}
		}

		/// <summary>
		///     Divide a rectangular <paramref name="solid" /> into new ones.
		/// </summary>
		/// <param name="numberOfRows">The number of rows of new solids to return.</param>
		/// <param name="numberOfColumns">The number of columns of new solids to return.</param>
		public static IEnumerable<Solid>? Divide(this Solid solid, int numberOfRows, int numberOfColumns)
		{
			// Verify if the solid is rectangular
			return
				solid.IsRectangular()
					? SolidDivide()
					: null;

			IEnumerable<Solid> SolidDivide()
			{
				// Get vertices
				var verts = solid.GetVertices().ToArray();

				// Get the distances between vertices of new solids
				double
					x = verts[0].DistanceTo(verts[1]) / numberOfColumns,
					y = verts[0].DistanceTo(verts[2]) / numberOfRows;

				for (var r = 0; r < numberOfRows; r++)
				{
					// Get starting y coordinate
					var yStart = verts[0].Y + r * y;

					for (var c = 0; c < numberOfColumns; c++)
					{
						// Get starting x coordinate
						var xStart = verts[0].X + c * x;

						// Get vertices of the solid
						Point3d[] newVerts =
						{
							new(xStart, yStart, 0),
							new(xStart + x, yStart, 0),
							new(xStart, yStart + y, 0),
							new(xStart + x, yStart + y, 0)
						};

						// Return the new solid
						yield return new Solid(newVerts[0], newVerts[1], newVerts[2], newVerts[3]) { XData = solid.XData };
					}
				}
			}
		}

		/// <summary>
		///     Remove this object from drawing.
		/// </summary>
		/// <param name="erasedEvent">The event to remove from object.</param>
		public static void EraseObject(this Document document, ObjectId objectId, ObjectErasedEventHandler? erasedEvent = null)
		{
			if (!objectId.IsOk())
				return;

			var database = document.Database;

			using var lck = document.LockDocument();

			using var trans = database.TransactionManager.StartTransaction();

			using var dbObject = trans.GetObject(objectId, OpenMode.ForWrite);

			if (dbObject is null)
				return;

			if (erasedEvent != null)
				dbObject.Erased -= erasedEvent;

			// Verify if there is attributes
			// if (ent is BlockReference blkRef && blkRef.AttributeCollection is not null && blkRef.AttributeCollection.Count > 0)
			// 	foreach (ObjectId attId in blkRef.AttributeCollection)
			// 	{
			// 		var attEnt = trans.GetObject(attId, OpenMode.ForWrite);
			// 		attEnt.Erase();
			// 	}

			dbObject.Erase();

			trans.Commit();
		}

		/// <summary>
		///     Remove all the objects in this collection from drawing.
		/// </summary>
		/// <param name="objects">The collection containing the <see cref="ObjectId" />'s to erase.</param>
		public static void EraseObjects(this Document document, IEnumerable<ObjectId>? objects, ObjectErasedEventHandler? erasedEvent = null)
		{
			if (objects.IsNullOrEmpty())
				return;

			var database = document.Database;

			using var lck = document.LockDocument();

			using var trans = database.TransactionManager.StartTransaction();

			foreach (var obj in objects)
			{
				using var dbObject = trans.GetObject(obj, OpenMode.ForWrite);

				if (dbObject is null)
					continue;

				if (erasedEvent != null)
					dbObject.Erased -= erasedEvent;

				dbObject.Erase();
			}

			trans.Commit();
		}

		/// <summary>
		///     Erase all the objects in this <paramref name="layerName" />.
		/// </summary>
		public static void EraseObjects(this Document document, string layerName, ObjectErasedEventHandler? erasedEvent = null)
		{
			var objs = document.GetObjectIds(layerName);

			document.EraseObjects(objs, erasedEvent);
		}

		/// <summary>
		///     Erase all the objects in these <paramref name="layerNames" />.
		/// </summary>
		public static void EraseObjects(this Document document, IEnumerable<string> layerNames, ObjectErasedEventHandler? erasedEvent = null)
		{
			var objs = document.GetObjectIds(layerNames.ToArray());

			document.EraseObjects(objs, erasedEvent);
		}

		/// <summary>
		///     Read a collection of <see cref="TypedValue" /> from a <see cref="DBDictionary" />.
		/// </summary>
		/// <param name="dbDictionary">The <see cref="DBDictionary" />.</param>
		/// <inheritdoc cref="GetExtendedDictionary(ObjectId, string)" />
		public static TypedValue[]? GetData(this DBDictionary? dbDictionary, string dataName)
		{
			if (dbDictionary is null)
				return null;

			// Start a transaction
			using var trans = dbDictionary.ObjectId.Database.TransactionManager.StartTransaction();

			var data = dbDictionary.Contains(dataName)
				? ((Xrecord) trans.GetObject(dbDictionary.GetAt(dataName), OpenMode.ForRead)).Data?.AsArray()
				: null;

			return data;
		}

		/// <summary>
		///     Read a collection of <see cref="TypedValue" /> from a <see cref="DBDictionary" />'s <see cref="ObjectId" />.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId" /> of a <see cref="DBDictionary" />.</param>
		/// <inheritdoc cref="GetExtendedDictionary(ObjectId, string)" />
		public static TypedValue[]? GetDataFromDictionary(this ObjectId objectId, string dataName)
		{
			if (!objectId.IsOk())
				return null;

			// Start a transaction
			using var trans = objectId.Database.TransactionManager.StartTransaction();

			// Get record
			using var obj = trans.GetObject(objectId, OpenMode.ForRead);
			
			return 
				obj is DBDictionary dict
					? dict.GetData(dataName)
					: null;
		}

		/// <summary>
		///     Get the AutoCAD document associated to a database.
		/// </summary>
		public static Document GetDocument(this Database database) => DocumentManager.GetDocument(database);

		/// <summary>
		///     Get the surrounding <see cref="Line" />'s of this quadrilateral <paramref name="solid" />.
		/// </summary>
		public static IEnumerable<Line>? GetEdges(this Solid solid)
		{
			// Get the vertices ordered
			var verts = solid.GetVertices().Order().ToArray();

			// Verify if it is quadrilateral
			return
				verts.Length is 4
					? Lines()
					: null;

			// Return the lines
			IEnumerable<Line> Lines()
			{
				yield return new Line(verts[0], verts[1]);
				yield return new Line(verts[2], verts[3]);
				yield return new Line(verts[2], verts[0]);
				yield return new Line(verts[3], verts[1]);
			}
		}

		/// <summary>
		///     Read this <see cref="DBObject" />'s extended dictionary as an <see cref="Array" /> of <see cref="TypedValue" />.
		/// </summary>
		/// <inheritdoc cref="GetExtendedDictionary(ObjectId, string)" />
		public static TypedValue[]? GetExtendedDictionary(this DBObject? dbObject, string dataName) =>
			dbObject?.ObjectId.GetExtendedDictionary(dataName);

		/// <summary>
		///     Read the extended dictionary associated to this <see cref="ObjectId" />'s as an <see cref="Array" /> of
		///     <see cref="TypedValue" />.
		/// </summary>
		/// <param name="dataName">The name of the required record.</param>
		/// <inheritdoc cref="GetObject" />
		public static TypedValue[]? GetExtendedDictionary(this ObjectId objectId, string dataName)
		{
			if (!objectId.IsOk())
				return null;

			// Start a transaction
			using var trans = objectId.Database.TransactionManager.StartTransaction();

			// Get dictionary
			var obj = trans.GetObject(objectId, OpenMode.ForRead);

			// Get data from record
			var data = obj.ExtensionDictionary.GetDataFromDictionary(dataName);

			return data;
		}

		/// <summary>
		///     Get the <see cref="ObjectId" /> of the extended dictionary associated to an object's <see cref="ObjectId" />.
		/// </summary>
		/// <inheritdoc cref="GetObject" />
		public static ObjectId GetExtendedDictionaryId(this ObjectId objectId)
		{
			if (!objectId.IsOk())
				return ObjectId.Null;

			// Start a transaction
			using var trans = objectId.Database.TransactionManager.StartTransaction();

			// Get dictionary
			var obj = trans.GetObject(objectId, OpenMode.ForRead);

			// Get data from record
			return obj.ExtensionDictionary;
		}

		/// <summary>
		///     Read a <see cref="DBObject" /> in the drawing from this <see cref="ObjectId" />.
		/// </summary>
		public static DBObject? GetObject(this Database database, ObjectId objectId)
		{
			if (!objectId.IsOk())
				return null;

			// Start a transaction
			using var trans = database.TransactionManager.StartTransaction();

			// Read the object
			var obj = trans.GetObject(objectId, OpenMode.ForRead);

			return obj;
		}

		/// <summary>
		///     Get the collection of <see cref="ObjectId" />'s of <paramref name="objects" />.
		/// </summary>
		public static IEnumerable<ObjectId>? GetObjectIds(this IEnumerable<DBObject>? objects) => objects?.Select(obj => obj.ObjectId);

		/// <summary>
		///     Get a collection containing all the <see cref="ObjectId" />'s in those <paramref name="layerNames" />.
		/// </summary>
		/// <param name="document">The AutoCAD document.</param>
		/// <param name="layerNames">The layer names.</param>
		public static IEnumerable<ObjectId>? GetObjectIds(this Document document, params string[] layerNames)
		{
			// Get the entities on the layername
			var selRes = document.Editor.SelectAll(layerNames.LayerFilter());

			return
				selRes.Status is PromptStatus.OK && selRes.Value is not null
					? selRes.Value.GetObjectIds()
					: null;
		}
		
		/// <summary>
		///     Get a collection containing all the <see cref="ObjectId" />'s in those <paramref name="layerNames" />.
		/// </summary>
		/// <param name="document">The AutoCAD document.</param>
		/// <param name="layerNames">The layer names.</param>
		public static IEnumerable<DBObject?>? GetObjects(this Document document, params string[] layerNames)
		{
			// Get the ids
			var ids = document.GetObjectIds(layerNames);

			return 
				ids is not null
					? document.Database.GetObjects(ids)
					: null;
		}

		/// <summary>
		///     Get the collection of <see cref="DBObject" />'s of <paramref name="objectIds" />.
		/// </summary>
		/// <inheritdoc cref="GetObject" />
		public static IEnumerable<DBObject?> GetObjects(this Database database, IEnumerable<ObjectId> objectIds)
		{
			// Start a transaction
			using var trans = database.TransactionManager.StartTransaction();

			var objs = objectIds
				.Select(obj => obj.IsOk() ? trans.GetObject(obj, OpenMode.ForRead) : null)
				.ToArray();	

			return objs;
		}

		/// <summary>
		///     Get the <see cref="Point3d" /> vertices of this <paramref name="solid" />.
		/// </summary>
		public static IEnumerable<Point3d> GetVertices(this Solid solid)
		{
			var points = new Point3dCollection();
			solid.GetGripPoints(points, new IntegerCollection(), new IntegerCollection());
			return points.Cast<Point3d>();
		}

		/// <summary>
		///     Compare two <see cref="ObjectId" />'s.
		/// </summary>
		/// <returns>
		///     True if <paramref name="other" /> is not <see cref="ObjectId.Null" /> and is equal to this.
		/// </returns>
		public static bool IsNotNullAndEqualTo(this ObjectId objectId, ObjectId other) => other != ObjectId.Null && other == objectId;

		/// <summary>
		///     Verify the state of this <see cref="ObjectId" />.
		/// </summary>
		/// <returns>
		///     True if <paramref name="objectId" /> is valid, not null and not erased.
		/// </returns>
		public static bool IsOk(this ObjectId objectId) => objectId != ObjectId.Null || !objectId.IsValid || !objectId.IsErased;

		/// <summary>
		///     Returns true if this <paramref name="solid" /> is rectangular.
		/// </summary>
		public static bool IsRectangular(this Solid solid)
		{
			// Get the vertices
			var verts = solid.GetVertices().ToArray();

			// Get the angles
			double[] angles =
			{
				(verts[0].AngleTo(verts[1]) - verts[0].AngleTo(verts[2])).Abs(),
				(verts[3].AngleTo(verts[1]) - verts[3].AngleTo(verts[2])).Abs()
			};

			return
				angles.All(angle => angle.Approx(Constants.PiOver2, 1E-3) || angle.Approx(Constants.Pi3Over2, 1E-3));
		}

		/// <summary>
		///     Returns a <see cref="SelectionFilter" /> for objects in this <paramref name="layerName" />.
		/// </summary>
		public static SelectionFilter LayerFilter(this string layerName) => new(new[] { new TypedValue((int) DxfCode.LayerName, layerName) });

		/// <summary>
		///     Returns a <see cref="SelectionFilter" /> for objects in these <paramref name="layerNames" />.
		/// </summary>
		public static SelectionFilter LayerFilter(this IEnumerable<string> layerNames) => new(new[] { new TypedValue((int) DxfCode.LayerName, layerNames.Aggregate(string.Empty, (current, layer) => current + $"{layer},")) });

		/// <summary>
		///     Move the objects in this collection to drawing bottom.
		/// </summary>
		public static void MoveToBottom(this Document document, IEnumerable<ObjectId>? objectIds)
		{
			if (objectIds.IsNullOrEmpty())
				return;

			var database = document.Database;

			using var lck   = document.LockDocument();
			using var trans = database.TransactionManager.StartTransaction();

			var blkTbl = (BlockTable) trans.GetObject(database.BlockTableId, OpenMode.ForRead);

			var blkTblRec = (BlockTableRecord) trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead);

			var drawOrder = (DrawOrderTable) trans.GetObject(blkTblRec.DrawOrderTableId, OpenMode.ForWrite);

			// Move the panels to bottom
			using var objs = new ObjectIdCollection(objectIds.ToArray());

			drawOrder.MoveToBottom(objs);

			trans.Commit();
		}

		/// <summary>
		///     Move the objects in this collection to drawing top.
		/// </summary>
		public static void MoveToTop(this Document document, IEnumerable<ObjectId>? objectIds)
		{
			if (objectIds.IsNullOrEmpty())
				return;

			var database = document.Database;

			using var lck   = document.LockDocument();
			using var trans = database.TransactionManager.StartTransaction();

			var blkTbl = (BlockTable) trans.GetObject(database.BlockTableId, OpenMode.ForRead);

			var blkTblRec = (BlockTableRecord) trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead);

			var drawOrder = (DrawOrderTable) trans.GetObject(blkTblRec.DrawOrderTableId, OpenMode.ForWrite);

			// Move the panels to bottom
			using (var objs = new ObjectIdCollection(objectIds.ToArray()))
			{
				drawOrder.MoveToTop(objs);
			}

			trans.Commit();
		}

		/// <summary>
		///     Return this collection of <see cref="DBPoint" />'s ordered in ascending Y then ascending X coordinates.
		/// </summary>
		public static IEnumerable<DBPoint> Order(this IEnumerable<DBPoint> points) => points.OrderBy(p => p.Position.Y).ThenBy(p => p.Position.X);

		/// <summary>
		///     Return this collection of <see cref="Line" />'s ordered in ascending Y then ascending X midpoint coordinates.
		/// </summary>
		public static IEnumerable<Line> Order(this IEnumerable<Line> lines) => lines.OrderBy(l => l.MidPoint().Y).ThenBy(l => l.MidPoint().X);

		/// <summary>
		///     Return this collection of <see cref="Solid" />'s ordered in ascending Y then ascending X center point coordinates.
		/// </summary>
		public static IEnumerable<Solid> Order(this IEnumerable<Solid> solids) => solids.OrderBy(s => s.CenterPoint().Y).ThenBy(s => s.CenterPoint().X);

		/// <summary>
		///     Register a <see cref="ObjectErasedEventHandler" /> to these <paramref name="objectIds" />
		/// </summary>
		/// <param name="handler"> The <see cref="ObjectErasedEventHandler" /> to add.</param>
		public static void RegisterErasedEvent(this Document document, IEnumerable<ObjectId> objectIds, ObjectErasedEventHandler handler)
		{
			if (objectIds.IsNullOrEmpty())
				return;

			var database = document.Database;

			using var lck = document.LockDocument();

			using var trans = database.TransactionManager.StartTransaction();

			foreach (var obj in objectIds.Where(o => o.IsOk()))
			{
				using var ent = (Entity?) trans.GetObject(obj, OpenMode.ForWrite);

				if (ent is null)
					continue;

				ent.Erased += handler;
			}

			trans.Commit();
		}

		/// <summary>
		///     Set a collection of <see cref="TypedValue" /> from a <see cref="DBDictionary" />'s <see cref="ObjectId" />.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId" /> of a <see cref="DBDictionary" />.</param>
		/// <param name="dataName">The name to set to the record.</param>
		/// <inheritdoc cref="SetExtendedDictionary(ObjectId, IEnumerable{TypedValue}, string, bool)" />
		public static void SetDataOnDictionary(this ObjectId objectId, IEnumerable<TypedValue>? data, string dataName, bool overwrite = true)
		{
			if (!objectId.IsOk())
				return;

			// Start a transaction
			using var trans = objectId.Database.TransactionManager.StartTransaction();

			// Get record
			using var obj = trans.GetObject(objectId, OpenMode.ForRead);

			if (obj is not DBDictionary dbExt || !overwrite && dbExt.Contains(dataName))
				return;

			dbExt.UpgradeOpen();

			var xRec = new Xrecord();

			if (data is not null)
				xRec.Data = new ResultBuffer(data.ToArray());

			// Set the data
			dbExt.SetAt(dataName, xRec);
			trans.AddNewlyCreatedDBObject(xRec, true);

			trans.Commit();
		}

		/// <summary>
		///     Set extended dictionary to this <paramref name="objectId" /> and return its <see cref="ObjectId" />.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId" /> to set the extended dictionary.</param>
		/// <param name="data">The collection of <see cref="TypedValue" /> to set at <paramref name="dataName" />.</param>
		/// <param name="dataName">The name to set to the record.</param>
		/// <param name="overwrite">Overwrite record if it already exists?</param>
		/// <inheritdoc cref="GetObject" />
		public static ObjectId SetExtendedDictionary(this ObjectId objectId, IEnumerable<TypedValue>? data, string dataName, bool overwrite = true)
		{
			if (!objectId.IsOk())
				return ObjectId.Null;

			// Start a transaction
			var database = objectId.Database;

			var doc = database.GetDocument();

			using var lck = doc.LockDocument();

			using var trans = database.TransactionManager.StartTransaction();

			using var obj = trans.GetObject(objectId, OpenMode.ForRead);

			if (obj is null)
				return ObjectId.Null;

			obj.UpgradeOpen();

			var extId = obj.ExtensionDictionary;

			if (extId.IsNull)
			{
				obj.CreateExtensionDictionary();
				extId = obj.ExtensionDictionary;
			}

			SetDataOnDictionary(extId, data, dataName, overwrite);

			trans.Commit();

			return extId;
		}

		/// <summary>
		///     Return an <see cref="ObjectIdCollection" /> from an<see cref="DBObjectCollection" />.
		/// </summary>
		public static ObjectIdCollection? ToObjectIdCollection(this DBObjectCollection? collection)
		{
			if (collection is null)
				return null;

			return
				collection.Count > 0
					? new ObjectIdCollection((from DBObject obj in collection select obj.ObjectId).ToArray())
					: new ObjectIdCollection();
		}

		/// <summary>
		///     Update scale of a collection of blocks.
		/// </summary>
		/// <param name="blockIds">The blocks' <see cref="ObjectId" />'s.</param>
		/// <param name="oldScale">The old scale factor.</param>
		/// <param name="newScale">The new scale factor.</param>
		/// <param name="setToAttributes">Set scale to attribute text heights?</param>
		public static void UpdateScale(this Document document, IEnumerable<ObjectId> blockIds, double oldScale, double newScale, bool setToAttributes = false)
		{
			var ratio = newScale / oldScale;

			var objIds = blockIds.ToList();

			if (objIds.IsNullOrEmpty() || !ratio.IsFinite() || ratio.Approx(1, 1E-3))
				return;

			using var lck   = document.LockDocument();
			using var trans = document.Database.TransactionManager.StartTransaction();

			foreach (var id in objIds)
			{
				if (id == ObjectId.Null || trans.GetObject(id, OpenMode.ForRead) is not BlockReference block)
					continue;

				// Get attributes
				var atts = block.AttributeCollection?.Cast<ObjectId>()
					.Select(o => (AttributeReference) trans.GetObject(o, OpenMode.ForRead))
					.ToList();

				// Get old attribute heights
				var attHeights = atts?.Select(a => a.Height).ToList();

				// Set scale
				block.UpgradeOpen();
				block.TransformBy(Matrix3d.Scaling(ratio, block.Position));

				if (setToAttributes || atts is null || atts.Count == 0)
					continue;

				// Set old attribute heights
				for (var i = 0; i < atts.Count; i++)
				{
					atts[i].UpgradeOpen();
					atts[i].Height = attHeights![i];
				}
			}

			trans.Commit();
		}

		/// <summary>
		///     Update text heights.
		/// </summary>
		/// <param name="objectIds">The collection of objects.</param>
		/// <param name="height">The height to set to texts.</param>
		public static void UpdateTextHeight(this Document document, IEnumerable<ObjectId> objectIds, double height)
		{
			using var lck   = document.LockDocument();
			using var trans = document.Database.TransactionManager.StartTransaction();

			foreach (var id in objectIds)
				switch (trans.GetObject(id, OpenMode.ForRead))
				{
					case DBText text:
						text.UpgradeOpen();
						text.Height = height;
						continue;

					case BlockReference blockReference:
						var atts = blockReference.AttributeCollection;
						foreach (ObjectId attId in atts)
						{
							if (trans.GetObject(attId, OpenMode.ForRead) is not DBText txt)
								continue;

							txt.UpgradeOpen();
							txt.Height = height;
						}

						continue;

					default:
						continue;
				}

			trans.Commit();
		}

		#endregion

	}
}