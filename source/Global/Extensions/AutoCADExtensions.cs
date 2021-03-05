using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using MathNet.Numerics;
using SPMTool.Core;
using SPMTool.Enums;
using UnitsNet.Units;
using static SPMTool.Core.DataBase;

#nullable enable

namespace SPMTool.Extensions
{
	public static partial class Extensions
	{
		#region  Methods

		/// <summary>
		///     Return the horizontal distance from this <paramref name="point" /> to <paramref name="otherPoint" /> .
		/// </summary>
		/// <param name="otherPoint">The other <see cref="Point3d" />.</param>
		public static double DistanceInX(this Point3d point, Point3d otherPoint) => (otherPoint.X - point.X).Abs();

		/// <summary>
		///     Return the vertical distance from this <paramref name="point" /> to <paramref name="otherPoint" /> .
		/// </summary>
		/// <inheritdoc cref="DistanceInX" />
		public static double DistanceInY(this Point3d point, Point3d otherPoint) => (otherPoint.Y - point.Y).Abs();

		/// <summary>
		///     Return the angle (in radians), related to horizontal axis, of a line that connects this to
		///     <paramref name="otherPoint" /> .
		/// </summary>
		/// <inheritdoc cref="DistanceInX" />
		/// <param name="tolerance">The tolerance to consider being zero.</param>
		public static double AngleTo(this Point3d point, Point3d otherPoint, double tolerance = 1E-6)
		{
			double
				x = otherPoint.X - point.X,
				y = otherPoint.Y - point.Y;

			if (x.Abs() < tolerance && y.Abs() < tolerance)
				return 0;

			if (y.Abs() < tolerance)
				return x > 0 ? 0 : Constants.Pi;

			if (x.Abs() < tolerance)
				return y > 0 ? Constants.PiOver2 : Constants.Pi3Over2;

			return
				(y / x).Atan();
		}

		/// <summary>
		///     Return the mid <see cref="Point3d" /> between this and <paramref name="otherPoint" />.
		/// </summary>
		/// <inheritdoc cref="DistanceInX" />
		public static Point3d MidPoint(this Point3d point, Point3d otherPoint) => point == otherPoint ? point : new Point3d(0.5 * (point.X + otherPoint.X), 0.5 * (point.Y + otherPoint.Y), 0.5 * (point.Z + otherPoint.Z));

		/// <summary>
		///     Return a <see cref="Point3d" /> with coordinates converted.
		/// </summary>
		/// <param name="fromUnit">The <see cref="LengthUnit" /> of origin.</param>
		/// <param name="toUnit">The <seealso cref="LengthUnit" /> to convert.</param>
		/// <returns></returns>
		public static Point3d Convert(this Point3d point, LengthUnit fromUnit, LengthUnit toUnit = LengthUnit.Millimeter) => fromUnit == toUnit ? point : new Point3d(point.X.Convert(fromUnit, toUnit), point.Y.Convert(fromUnit, toUnit), point.Z.Convert(fromUnit, toUnit));

		/// <summary>
		///     Returns true if this <paramref name="point" /> is approximately equal to <paramref name="otherPoint" />.
		/// </summary>
		/// <param name="otherPoint">The other <see cref="Point3d" /> to compare.</param>
		/// <param name="tolerance">The tolerance to considering equivalent.</param>
		public static bool Approx(this Point3d point, Point3d otherPoint, double tolerance = 1E-3) => point.X.Approx(otherPoint.X, tolerance) && point.Y.Approx(otherPoint.Y, tolerance) && point.Z.Approx(otherPoint.Z, tolerance);

		/// <summary>
		///     Return this collection of <see cref="Point3d" />'s ordered in ascending Y then ascending X.
		/// </summary>
		public static IEnumerable<Point3d> Order(this IEnumerable<Point3d> points) => points.OrderBy(p => p.Y).ThenBy(p => p.X);

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
		///     Convert to a <see cref="IEnumerable{T}" /> of <see cref="Point3d" />.
		/// </summary>
		public static IEnumerable<Point3d> ToCollection(this Point3dCollection points) => points.Cast<Point3d>();

		/// <summary>
		///     Convert to a <see cref="IEnumerable{T}" /> of <see cref="ObjectId" />.
		/// </summary>
		public static IEnumerable<ObjectId> ToCollection(this ObjectIdCollection objectIds) => objectIds.Cast<ObjectId>();

		/// <summary>
		///     Convert to a <see cref="IEnumerable{T}" /> of <see cref="DBObject" />.
		/// </summary>
		public static IEnumerable<DBObject> ToCollection(this DBObjectCollection objects) => objects.Cast<DBObject>();

		/// <summary>
		///     Convert this <paramref name="value" /> to a <see cref="double" />.
		/// </summary>
		public static double ToDouble(this TypedValue value) => System.Convert.ToDouble(value.Value);

		/// <summary>
		///     Convert this <paramref name="value" /> to an <see cref="int" />.
		/// </summary>
		public static int ToInt(this TypedValue value) => System.Convert.ToInt32(value.Value);

		/// <summary>
		///     Read a <see cref="DBObject" /> in the drawing from this <see cref="ObjectId" />.
		/// </summary>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static DBObject? GetDBObject(this ObjectId objectId, Transaction? ongoingTransaction = null)
		{
			if (!objectId.IsOk())
				return null;

			// Start a transaction
			var trans = ongoingTransaction ?? StartTransaction();

			// Read the object
			var obj = trans.GetObject(objectId, OpenMode.ForRead);

			if (ongoingTransaction is null)
				trans.Dispose();

			return obj;
		}

		/// <summary>
		///     Read a <see cref="Entity" /> in the drawing from this <see cref="ObjectId" />.
		/// </summary>
		/// <inheritdoc cref="GetDBObject" />
		public static Entity? GetEntity(this ObjectId objectId, Transaction? ongoingTransaction = null) => (Entity?) objectId.GetDBObject(ongoingTransaction);

		/// <summary>
		///     Get the <see cref="ObjectId" /> related to this <paramref name="handle" />.
		/// </summary>
		public static ObjectId GetObjectId(this Handle handle) => DataBase.Database.TryGetObjectId(handle, out var obj) ? obj : ObjectId.Null;

		/// <summary>
		///     Get the <see cref="Entity" /> related to this <paramref name="handle" />.
		/// </summary>
		public static Entity? GetEntity(this Handle handle) => handle.GetObjectId().GetEntity();

		/// <summary>
		///     Return a <see cref="DBObjectCollection" /> from an <see cref="ObjectIdCollection" />.
		/// </summary>
		/// <inheritdoc cref="GetDBObject" />
		public static DBObjectCollection? ToDBObjectCollection(this ObjectIdCollection? collection, Transaction? ongoingTransaction = null)
		{
			if (collection is null)
				return null;

			var dbCollection = new DBObjectCollection();

			// Start a transaction
			if (collection.Count > 0)
			{
				var trans = ongoingTransaction ?? StartTransaction();

				foreach (ObjectId objectId in collection)
					if (objectId.IsValid && !objectId.IsNull)
						dbCollection.Add(trans.GetObject(objectId, OpenMode.ForRead));

				if (ongoingTransaction is null)
					trans.Dispose();
			}

			return dbCollection;
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
		///     Get the collection of <see cref="ObjectId" />'s of <paramref name="objects" />.
		/// </summary>
		public static IEnumerable<ObjectId>? GetObjectIds(this IEnumerable<DBObject>? objects) => objects?.Select(obj => obj.ObjectId);

		/// <summary>
		///     Get the collection of <see cref="DBObject" />'s of <paramref name="objectIds" />.
		/// </summary>
		/// <inheritdoc cref="GetDBObject" />
		public static IEnumerable<TDBObject?>? GetDBObjects<TDBObject>(this IEnumerable<ObjectId>? objectIds, Transaction? ongoingTransaction = null)
			where TDBObject : DBObject
		{
			if (objectIds.IsNullOrEmpty())
				return null;

			// Start a transaction
			var trans = ongoingTransaction ?? StartTransaction();

			var objs = objectIds.Select(obj => trans.GetObject(obj, OpenMode.ForRead)).Cast<TDBObject?>().ToArray();

			if (ongoingTransaction is null)
				trans.Dispose();

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
		///     Get the mid <see cref="Point3d" /> of a <paramref name="line" />.
		/// </summary>
		public static Point3d MidPoint(this Line line) => line.StartPoint.MidPoint(line.EndPoint);

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
		///     Read this <see cref="DBObject" />'s extended dictionary as an <see cref="Array" /> of <see cref="TypedValue" />.
		/// </summary>
		/// <inheritdoc cref="GetExtendedDictionary(ObjectId, string, Transaction)" />
		public static TypedValue[]? GetExtendedDictionary(this DBObject? dbObject, string dataName, Transaction? ongoingTransaction = null) => dbObject?.ObjectId.GetExtendedDictionary(dataName, ongoingTransaction);

		/// <summary>
		///     Read this <see cref="Entity" />'s extended dictionary as an <see cref="Array" /> of <see cref="TypedValue" />.
		/// </summary>
		/// <inheritdoc cref="GetExtendedDictionary(ObjectId, string, Transaction)" />
		public static TypedValue[]? GetExtendedDictionary(this Entity? entity, string dataName, Transaction? ongoingTransaction = null) => entity?.ObjectId.GetExtendedDictionary(dataName, ongoingTransaction);

		/// <summary>
		///     Read the extended dictionary associated to this <see cref="ObjectId" />'s as an <see cref="Array" /> of
		///     <see cref="TypedValue" />.
		/// </summary>
		/// <param name="dataName">The name of the required record.</param>
		/// <inheritdoc cref="GetDBObject" />
		public static TypedValue[]? GetExtendedDictionary(this ObjectId objectId, string dataName, Transaction? ongoingTransaction = null)
		{
			if (!objectId.IsOk())
				return null;

			// Start a transaction
			var trans = ongoingTransaction ?? StartTransaction();

			// Get dictionary
			var obj = trans.GetObject(objectId, OpenMode.ForRead);

			// Get data from record
			var data = obj.ExtensionDictionary.GetDataFromDictionary(dataName, trans);

			if (ongoingTransaction is null)
				trans.Dispose();

			return
				data;
		}

		/// <summary>
		///     Verify the state of this <see cref="ObjectId" />.
		/// </summary>
		/// <returns>
		///     True if <paramref name="objectId" /> is valid, not null and not erased.
		/// </returns>
		public static bool IsOk(this ObjectId objectId) => objectId.IsValid && !objectId.IsNull && !objectId.IsErased;

		/// <summary>
		///     Set a collection of <see cref="TypedValue" /> from a <see cref="DBDictionary" />'s <see cref="ObjectId" />.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId" /> of a <see cref="DBDictionary" />.</param>
		/// <param name="dataName">The name to set to the record.</param>
		/// <inheritdoc cref="SetExtendedDictionary(ObjectId, IEnumerable{TypedValue}, string, bool, Transaction)" />
		public static bool SetDataOnDictionary(this ObjectId objectId, IEnumerable<TypedValue>? data,  string dataName, bool overwrite = true, Transaction? ongoingTransaction = null)
		{
			if (!objectId.IsOk())
				return false;

			// Start a transaction
			var trans = ongoingTransaction ?? StartTransaction();

			// Get record
			using var obj  = trans.GetObject(objectId, OpenMode.ForRead);

			if (!(obj is DBDictionary dbExt) || !overwrite && dbExt.Contains(dataName))
				return false;

			dbExt.UpgradeOpen();

			var xRec = new Xrecord
			{
				Data = data is null
					? null
					: new ResultBuffer(data.ToArray())
			};

			// Set the data
			dbExt.SetAt(dataName, xRec);
			trans.AddNewlyCreatedDBObject(xRec, true);

			if (ongoingTransaction is null)
			{
				trans.Commit();
				trans.Dispose();
			}

			return true;
		}

		/// <summary>
		///     Read a collection of <see cref="TypedValue" /> from a <see cref="DBDictionary" />'s <see cref="ObjectId" />.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId" /> of a <see cref="DBDictionary" />.</param>
		/// <inheritdoc cref="GetExtendedDictionary(ObjectId, string, Transaction)" />
		public static TypedValue[]? GetDataFromDictionary(this ObjectId objectId, string dataName, Transaction? ongoingTransaction = null)
		{
			if (!objectId.IsOk())
				return null;

			// Start a transaction
			var trans = ongoingTransaction ?? StartTransaction();

			// Get record
			using var obj  = trans.GetObject(objectId, OpenMode.ForRead);
			var data = obj is DBDictionary dict
				? dict.GetData(dataName, trans)
				: null;

			if (ongoingTransaction is null)
				trans.Dispose();

			return data;
		}

		/// <summary>
		///     Read a collection of <see cref="TypedValue" /> from a <see cref="DBDictionary" />.
		/// </summary>
		/// <param name="dbDictionary">The <see cref="DBDictionary"/>.</param>
		/// <inheritdoc cref="GetExtendedDictionary(ObjectId, string, Transaction)" />
		public static TypedValue[]? GetData(this DBDictionary? dbDictionary, string dataName, Transaction? ongoingTransaction = null)
		{
			if (dbDictionary is null)
				return null;

			// Start a transaction
			var trans = ongoingTransaction ?? StartTransaction();

			var data = dbDictionary.Contains(dataName)
				? ((Xrecord) trans.GetObject(dbDictionary.GetAt(dataName), OpenMode.ForRead)).Data?.AsArray()
				: null;

			if (ongoingTransaction is null)
				trans.Dispose();

			return data;
		}

		/// <summary>
		///     Set extended dictionary to this <paramref name="objectId" /> and return its <see cref="ObjectId"/>.
		/// </summary>
		/// <param name="objectId">The <see cref="ObjectId" /> to set the extended dictionary.</param>
		/// <param name="data">The collection of <see cref="TypedValue" /> to set at <paramref name="dataName" />.</param>
		/// <param name="dataName">The name to set to the record.</param>
		/// <param name="overwrite">Overwrite record if it already exists?</param>
		/// <inheritdoc cref="GetDBObject" />
		public static ObjectId SetExtendedDictionary(this ObjectId objectId, IEnumerable<TypedValue>? data, string dataName, bool overwrite = true, Transaction? ongoingTransaction = null)
		{
			if (!objectId.IsOk())
				return ObjectId.Null;

			// Start a transaction
			using var lck = Document.LockDocument();
			using var trans = ongoingTransaction ?? StartTransaction();

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

			SetDataOnDictionary(extId, data, dataName, overwrite, trans);

			if (ongoingTransaction is null)
			{
				trans.Commit();
				trans.Dispose();
			}

			return extId;
		}

		/// <summary>
		///     Set extended data to this <paramref name="dbObject" /> and return its <see cref="ObjectId"/>.
		/// </summary>
		/// <param name="dbObject">The <see cref="DBObject" /> to set the extended dictionary.</param>
		/// <inheritdoc cref="SetExtendedDictionary(ObjectId, IEnumerable{TypedValue}, string, bool, Transaction)" />
		public static ObjectId SetExtendedDictionary(this DBObject dbObject, IEnumerable<TypedValue>? data, string dataName, bool overwrite = true, Transaction? ongoingTransaction = null) =>
			dbObject.ObjectId.SetExtendedDictionary(data, dataName, overwrite, ongoingTransaction);

		/// <summary>
		///     Set extended data to this <paramref name="entity" /> and return its <see cref="ObjectId"/>.
		/// </summary>
		/// <param name="entity">The <see cref="Entity" /> to set the extended dictionary.</param>
		/// <inheritdoc cref="SetExtendedDictionary(ObjectId, IEnumerable{TypedValue}, string, bool, Transaction)" />
		public static ObjectId SetExtendedDictionary(this Entity entity, IEnumerable<TypedValue>? data, string dataName, bool overwrite = true, Transaction? ongoingTransaction = null) =>
			entity.ObjectId.SetExtendedDictionary(data, dataName, overwrite, ongoingTransaction);

		/// <summary>
		///     Add this <paramref name="dbObject" /> to the drawing and return it's <see cref="ObjectId" />.
		/// </summary>
		/// <param name="dbObject">The <see cref="DBObject" />.</param>
		/// <param name="erasedEvent">The event to call if <paramref name="dbObject" /> is erased.</param>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static ObjectId AddToDrawing(this DBObject dbObject, ObjectErasedEventHandler? erasedEvent = null, Transaction? ongoingTransaction = null) => ((Entity) dbObject).AddToDrawing(erasedEvent, ongoingTransaction);

		/// <summary>
		///     Add this <paramref name="entity" /> to the drawing and return it's <see cref="ObjectId" />.
		/// </summary>
		/// <param name="entity">The <see cref="Entity" />.</param>
		/// <param name="erasedEvent">The event to call if <paramref name="entity" /> is erased.</param>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static ObjectId AddToDrawing(this Entity entity, ObjectErasedEventHandler? erasedEvent = null, Transaction? ongoingTransaction = null)
		{
			if (entity is null)
				return ObjectId.Null;

			// Start a transaction
			using var lck = Document.LockDocument();
			var trans = ongoingTransaction ?? StartTransaction();

			// Open the Block table for read
			var blkTbl = (BlockTable) trans.GetObject(BlockTableId, OpenMode.ForRead);

			// Open the Block table record Model space for write
			var blkTblRec = (BlockTableRecord) trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

			// Add the object to the drawing
			blkTblRec.AppendEntity(entity);
			trans.AddNewlyCreatedDBObject(entity, true);

			if (erasedEvent != null)
				entity.Erased += erasedEvent;

			// Commit changes
			if (ongoingTransaction is null)
			{
				trans.Commit();
				trans.Dispose();
			}

			return
				entity.ObjectId;
		}

		/// <summary>
		///     Add the <paramref name="objects" /> in this collection to the drawing and return the collection of
		///     <see cref="ObjectId" />'s.
		/// </summary>
		/// <param name="erasedEvent">The event to call if <paramref name="objects" /> are erased.</param>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static IEnumerable<ObjectId>? AddToDrawing(this IEnumerable<DBObject>? objects, ObjectErasedEventHandler? erasedEvent = null, Transaction? ongoingTransaction = null) => objects?.Cast<Entity>()?.AddToDrawing(erasedEvent, ongoingTransaction);

		/// <summary>
		///     Add the <paramref name="entities" /> in this collection to the drawing and return the collection of
		///     <see cref="ObjectId" />'s.
		/// </summary>
		/// <param name="erasedEvent">The event to call if <paramref name="entities" /> are erased.</param>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static IEnumerable<ObjectId>? AddToDrawing(this IEnumerable<Entity>? entities, ObjectErasedEventHandler? erasedEvent = null, Transaction? ongoingTransaction = null)
		{
			if (entities.IsNullOrEmpty())
				return null;

			// Start a transaction
			using var lck = Document.LockDocument();
			var trans = ongoingTransaction ?? StartTransaction();

			// Open the Block table for read
			var blkTbl = (BlockTable) trans.GetObject(BlockTableId, OpenMode.ForRead);

			// Open the Block table record Model space for write
			var blkTblRec = (BlockTableRecord) trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

			// Get a list to return
			var list = new List<ObjectId>();

			// Add the objects to the drawing
			foreach (var ent in entities)
			{
				blkTblRec.AppendEntity(ent);
				trans.AddNewlyCreatedDBObject(ent, true);

				if (erasedEvent != null)
					ent.Erased += erasedEvent;

				list.Add(ent.ObjectId);
			}

			// Commit changes
			if (ongoingTransaction is null)
			{
				trans.Commit();
				trans.Dispose();
			}

			return list;
		}

		/// <summary>
		///     Register a <see cref="ObjectErasedEventHandler" /> to this <paramref name="objectId" />
		/// </summary>
		/// <param name="handler"> The <see cref="ObjectErasedEventHandler" /> to add.</param>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void RegisterErasedEvent(this ObjectId objectId, ObjectErasedEventHandler handler, Transaction? ongoingTransaction = null)
		{
			if (handler is null || !objectId.IsOk())
				return;

			// Start a transaction
			using var lck = Document.LockDocument();
			var trans = ongoingTransaction ?? StartTransaction();

			using (var ent = (Entity) trans.GetObject(objectId, OpenMode.ForWrite))
			{
				ent.Erased += handler;
			}

			// Commit changes
			if (ongoingTransaction != null)
				return;

			trans.Commit();
			trans.Dispose();
		}

		/// <summary>
		///     Register a <see cref="ObjectErasedEventHandler" /> to these <paramref name="objectIds" />
		/// </summary>
		/// <param name="handler"> The <see cref="ObjectErasedEventHandler" /> to add.</param>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void RegisterErasedEvent(this IEnumerable<ObjectId>? objectIds, ObjectErasedEventHandler handler, Transaction? ongoingTransaction = null)
		{
			if (handler is null || objectIds.IsNullOrEmpty())
				return;

			// Start a transaction
			using var lck = Document.LockDocument();
			var trans = ongoingTransaction ?? StartTransaction();

			foreach (var obj in objectIds)
				obj.RegisterErasedEvent(handler, trans);

			// Commit changes
			if (ongoingTransaction != null)
				return;

			trans.Commit();
			trans.Dispose();
		}

		/// <summary>
		///     Unregister a <see cref="ObjectErasedEventHandler" /> from this <paramref name="objectId" />
		/// </summary>
		/// <param name="handler"> The <see cref="ObjectErasedEventHandler" /> to remove.</param>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void UnregisterErasedEvent(this ObjectId objectId, ObjectErasedEventHandler handler, Transaction? ongoingTransaction = null)
		{
			if (handler is null || !objectId.IsOk())
				return;

			// Start a transaction
			using var lck = Document.LockDocument();
			var trans = ongoingTransaction ?? StartTransaction();

			using (var ent = (Entity) trans.GetObject(objectId, OpenMode.ForWrite))
			{
				ent.Erased -= handler;
			}

			// Commit changes
			if (ongoingTransaction != null)
				return;

			trans.Commit();
			trans.Dispose();
		}

		/// <summary>
		///     Unregister a <see cref="ObjectErasedEventHandler" /> from these <paramref name="objectIds" />
		/// </summary>
		/// <param name="handler"> The <see cref="ObjectErasedEventHandler" /> to add.</param>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void UnregisterErasedEvent(this IEnumerable<ObjectId>? objectIds, ObjectErasedEventHandler handler, Transaction? ongoingTransaction = null)
		{
			if (handler is null || objectIds.IsNullOrEmpty())
				return;

			// Start a transaction
			using var lck = Document.LockDocument();
			var trans = ongoingTransaction ?? StartTransaction();

			foreach (var obj in objectIds)
				obj.UnregisterErasedEvent(handler, trans);

			// Commit changes
			if (ongoingTransaction != null)
				return;

			trans.Commit();
			trans.Dispose();
		}

		/// <summary>
		///     Remove this object from drawing.
		/// </summary>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void RemoveFromDrawing(this ObjectId obj, Transaction? ongoingTransaction = null)
		{
			if (!obj.IsOk())
				return;

			// Start a transaction
			using var lck = Document.LockDocument();
			var trans = ongoingTransaction ?? StartTransaction();

			using (var ent = (Entity) trans.GetObject(obj, OpenMode.ForWrite))
			{
				ent.Erase();
			}

			// Commit changes
			if (ongoingTransaction != null)
				return;

			trans.Commit();
			trans.Dispose();
		}

		/// <summary>
		///     Remove this object from drawing.
		/// </summary>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void RemoveFromDrawing(this DBObject? obj, Transaction? ongoingTransaction = null) => obj?.ObjectId.RemoveFromDrawing(ongoingTransaction);

		/// <summary>
		///     Remove this <paramref name="entity" /> from drawing.
		/// </summary>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void RemoveFromDrawing(this Entity? entity, Transaction? ongoingTransaction = null) => entity?.ObjectId.RemoveFromDrawing(ongoingTransaction);

		/// <summary>
		///     Remove all the objects in this collection from drawing.
		/// </summary>
		/// <param name="objects">The collection containing the <see cref="ObjectId" />'s to erase.</param>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void RemoveFromDrawing(this IEnumerable<ObjectId>? objects, Transaction? ongoingTransaction = null)
		{
			if (objects.IsNullOrEmpty())
				return;

			// Start a transaction
			using var lck = Document.LockDocument();
			var trans = ongoingTransaction ?? StartTransaction();

			foreach (var obj in objects)
				obj.RemoveFromDrawing(trans);

			// Commit changes
			if (ongoingTransaction != null)
				return;

			trans.Commit();
			trans.Dispose();
		}

		/// <summary>
		///     Remove all the objects in this collection from drawing.
		/// </summary>
		/// <param name="objects">The collection containing the <see cref="DBObject" />'s to erase.</param>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void RemoveFromDrawing(this IEnumerable<DBObject>? objects, Transaction? ongoingTransaction = null) => objects?.GetObjectIds()?.RemoveFromDrawing(ongoingTransaction);

		/// <summary>
		///     Remove all the objects in this collection from drawing.
		/// </summary>
		/// <param name="objects">The <see cref="ObjectIdCollection" /> containing the objects to erase.</param>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void RemoveFromDrawing(this ObjectIdCollection? objects, Transaction? ongoingTransaction = null) => objects?.Cast<ObjectId>().RemoveFromDrawing(ongoingTransaction);

		/// <summary>
		///     Erase all the objects in this <see cref="DBObjectCollection" />.
		/// </summary>
		/// <param name="objects">The <see cref="DBObjectCollection" /> containing the objects to erase.</param>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void RemoveFromDrawing(this DBObjectCollection? objects, Transaction? ongoingTransaction = null) => objects?.ToObjectIdCollection()?.RemoveFromDrawing(ongoingTransaction);

		/// <summary>
		///     Erase all the objects in this <paramref name="layerName" />.
		/// </summary>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void RemoveFromDrawing(this string layerName, Transaction? ongoingTransaction = null) => layerName.GetObjectIds()?.RemoveFromDrawing(ongoingTransaction);

		/// <summary>
		///     Erase all the objects in these <paramref name="layerNames" />.
		/// </summary>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void RemoveFromDrawing(this IEnumerable<string> layerNames, Transaction? ongoingTransaction = null) => layerNames.GetObjectIds()?.RemoveFromDrawing(ongoingTransaction);

		/// <summary>
		///     Move the objects in this collection to drawing bottom.
		/// </summary>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void MoveToBottom(this IEnumerable<ObjectId>? objectIds, Transaction? ongoingTransaction = null)
		{
			if (objectIds.IsNullOrEmpty())
				return;

			using var lck = Document.LockDocument();
			var trans = ongoingTransaction ?? StartTransaction();

			var blkTbl = (BlockTable) trans.GetObject(BlockTableId, OpenMode.ForRead);

			var blkTblRec = (BlockTableRecord) trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead);

			var drawOrder = (DrawOrderTable) trans.GetObject(blkTblRec.DrawOrderTableId, OpenMode.ForWrite);

			// Move the panels to bottom
			using (var objs = new ObjectIdCollection(objectIds.ToArray()))
			{
				drawOrder.MoveToBottom(objs);
			}

			// Commit changes
			if (ongoingTransaction != null)
				return;

			trans.Commit();
			trans.Dispose();
		}

		/// <summary>
		///     Move the objects in this collection to drawing bottom.
		/// </summary>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void MoveToBottom(this IEnumerable<DBObject>? objects, Transaction? ongoingTransaction = null) => objects?.GetObjectIds()?.MoveToBottom(ongoingTransaction);

		/// <summary>
		///     Move the objects in this collection to drawing top.
		/// </summary>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void MoveToTop(this IEnumerable<ObjectId>? objectIds, Transaction? ongoingTransaction = null)
		{
			if (objectIds.IsNullOrEmpty())
				return;

			using var lck = Document.LockDocument();
			var trans = ongoingTransaction ?? StartTransaction();

			var blkTbl = (BlockTable) trans.GetObject(BlockTableId, OpenMode.ForRead);

			var blkTblRec = (BlockTableRecord) trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead);

			var drawOrder = (DrawOrderTable) trans.GetObject(blkTblRec.DrawOrderTableId, OpenMode.ForWrite);

			// Move the panels to bottom
			using (var objs = new ObjectIdCollection(objectIds.ToArray()))
			{
				drawOrder.MoveToTop(objs);
			}

			// Commit changes
			if (ongoingTransaction != null)
				return;

			trans.Commit();
			trans.Dispose();
		}

		/// <summary>
		///     Move the objects in this collection to drawing top.
		/// </summary>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void MoveToTop(this IEnumerable<DBObject>? objects, Transaction? ongoingTransaction = null) => objects?.GetObjectIds()?.MoveToTop(ongoingTransaction);

		/// <summary>
		///     Returns a <see cref="SelectionFilter" /> for objects in this <paramref name="layerName" />.
		/// </summary>
		public static SelectionFilter LayerFilter(this string layerName) => new SelectionFilter(new [] {new TypedValue((int) DxfCode.LayerName, layerName)});

		/// <summary>
		///     Returns a <see cref="SelectionFilter" /> for objects in these <paramref name="layerNames" />.
		/// </summary>
		public static SelectionFilter LayerFilter(this IEnumerable<string> layerNames) => new SelectionFilter(new [] {new TypedValue((int) DxfCode.LayerName, layerNames.Aggregate(string.Empty, (current, layer) => current + $"{layer},"))});

		/// <summary>
		///     Get a collection containing all the <see cref="ObjectId" />'s in this <see cref="layerName" />.
		/// </summary>
		public static IEnumerable<ObjectId> GetObjectIds(this string layerName)
		{
			// Get the entities on the layername
			var selRes = Model.Editor.SelectAll(layerName.LayerFilter());

			return
				selRes.Status == PromptStatus.OK && selRes.Value.Count > 0
					? selRes.Value.GetObjectIds()
					: new ObjectId[0];
		}

		/// <summary>
		///     Get a collection containing all the <see cref="ObjectId" />'s in those <paramref name="layerNames" />.
		/// </summary>
		public static IEnumerable<ObjectId>? GetObjectIds(this IEnumerable<string>? layerNames)
		{
			if (layerNames.IsNullOrEmpty())
				return null;

			// Get the entities on the layername
			var selRes = Model.Editor.SelectAll(layerNames.LayerFilter());

			return
				selRes.Status == PromptStatus.OK && selRes.Value.Count > 0
					? selRes.Value.GetObjectIds()
					: new ObjectId[0];
		}

		/// <summary>
		///     Create a block in the database.
		/// </summary>
		/// <param name="blockEntities">The collection of <see cref="Entity" />'s that form the block.</param>
		/// <param name="originPoint">The origin point of the block.</param>
		/// <param name="blockName">The name to save the block in database.</param>
		/// <param name="ongoingTransaction">The ongoing <see cref="Transaction" />. Commit latter if not null.</param>
		public static void CreateBlock(this IEnumerable<Entity>? blockEntities, Point3d originPoint, string blockName, Transaction? ongoingTransaction = null)
		{
			if (blockEntities.IsNullOrEmpty())
				return;

			using var lck = Document.LockDocument();
			var trans = ongoingTransaction ?? StartTransaction();

			// Open the Block table for read
			using var blkTbl = (BlockTable) trans.GetObject(BlockTableId, OpenMode.ForRead);

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
			if (ongoingTransaction != null)
				return;

			trans.Commit();
			trans.Dispose();
		}

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
					? Divide()
					: null;

			IEnumerable<Solid> Divide()
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
							new Point3d(xStart,     yStart,     0),
							new Point3d(xStart + x, yStart,     0),
							new Point3d(xStart,     yStart + y, 0),
							new Point3d(xStart + x, yStart + y, 0)
						};

						// Return the new solid
						yield return new Solid(newVerts[0], newVerts[1], newVerts[2], newVerts[3]) { XData = solid.XData};
					}
				}
			}
		}

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

		#endregion
	}
}