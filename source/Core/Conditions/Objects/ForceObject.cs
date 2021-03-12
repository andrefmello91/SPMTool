using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Extensions;
using MathNet.Numerics;
using OnPlaneComponents;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;
using static SPMTool.Core.DataBase;

#nullable enable

// ReSharper disable once CheckNamespace
namespace SPMTool.Core.Conditions
{
	/// <summary>
	///     Force object class.
	/// </summary>
	public class ForceObject : ConditionObject<ForceObject, PlaneForce>
	{
		#region Fields

		private List<AttributeReference?> _attributes;

		#endregion

		#region Properties

		public override Block Block => Direction is ComponentDirection.Both ? Block.ForceXY : Block.ForceY;

		public override ComponentDirection Direction => Value.Direction;

		public override Layer Layer => Layer.Force;

		/// <summary>
		///		Get rotation angle for X direction. Rotation around Y axis.
		/// </summary>
		protected override double RotationAngle => Value.X >= Force.Zero
			? 0
			: Constants.Pi;

		/// <summary>
		///		Get rotation angle for Y direction. Rotation around X axis.
		/// </summary>
		protected double RotationAngleY => Value.Y <= Force.Zero
			? 0
			: Constants.Pi;

		#endregion

		#region Constructors

		/// <summary>
		///     Plane Force object constructor.
		/// </summary>
		/// <inheritdoc />
		public ForceObject(Point position, PlaneForce value)
			: base(position, value)
		{
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Read a <see cref="ForceObject" /> from an <see cref="ObjectId" />.
		/// </summary>
		/// <param name="forceObjectId">The <see cref="ObjectId" /> of the force.</param>
		public static ForceObject? ReadFromObjectId(ObjectId forceObjectId) => ReadFromBlock((BlockReference?) forceObjectId.GetEntity());

		/// <summary>
		///     Read a <see cref="ForceObject" /> from a <see cref="BlockReference" />.
		/// </summary>
		/// <param name="reference">The <see cref="BlockReference" /> object of the force.</param>
		public static ForceObject? ReadFromBlock(BlockReference? reference) =>
			reference is null
				? null
				: new ForceObject(reference.Position.ToPoint(Settings.Units.Geometry), PlaneForce.Zero)
				{
					ObjectId = reference.ObjectId
				};

		public override BlockReference CreateEntity()
		{
			var insertionPoint = Position.ToPoint3d();

			var block = Block.GetReference(insertionPoint, Layer)!;

			// Rotate the block
			if (Direction is ComponentDirection.X)
				block.TransformBy(Matrix3d.Rotation(Constants.PiOver2, Ucs.Zaxis, insertionPoint));

			else if (!RotationAngleY.ApproxZero(1E-3))
				block.TransformBy(Matrix3d.Rotation(RotationAngleY, Ucs.Xaxis, insertionPoint));

			if (!RotationAngle.ApproxZero(1E-3))
				block.TransformBy(Matrix3d.Rotation(RotationAngle, Ucs.Yaxis, insertionPoint));

			return block;
		}

		public override void AddToDrawing()
		{
			ObjectId = CreateEntity().AddToDrawing();
			SetAttributes();
		}

		/// <summary>
		///		Set attributes to the force block.
		/// </summary>
		private void SetAttributes() => ObjectId.SetBlockAttributes(ForceAttributeReference()?.ToList());

		/// <summary>
		///		Get the attribute references for force block.
		/// </summary>
		private IEnumerable<AttributeReference?>? ForceAttributeReference()
		{
			if (!Value.IsXZero)
				yield return new AttributeReference(TextInsertionPoint(ComponentDirection.X).ToPoint3d(), $"{Value.X.Value.Abs():0.00}", "FX", DataBase.Database.Textstyle)
				{
					Height              = 30 * Settings.Units.ScaleFactor,
					Justify             = AttachmentPoint.MiddleLeft,
					LockPositionInBlock = true,
					Invisible           = false
				};

			if (!Value.IsYZero)
				yield return new AttributeReference(TextInsertionPoint(ComponentDirection.Y).ToPoint3d(), $"{Value.Y.Value.Abs():0.00}", "FY", DataBase.Database.Textstyle)
				{
					Height = 30 * Settings.Units.ScaleFactor,
					Justify = AttachmentPoint.MiddleLeft,
					LockPositionInBlock = true,
					Invisible = false
				};
		}

		protected override bool GetProperties()
		{
			var force = GetForce();

			if (!force.HasValue)
				return false;

			// Get values
			Value = force.Value;

			return true;
		}

		/// <summary>
		///     Get the insertion point of the associated text.
		/// </summary>
		private Point TextInsertionPoint(ComponentDirection direction)
		{
			var x = Position.X;
			var y = Position.Y;
			
			return direction switch
			{
				ComponentDirection.X when Value.X < Force.Zero => new Point(x + Length.FromMillimeters(75),  y + Length.FromMillimeters(25)),
				ComponentDirection.X when Value.X > Force.Zero => new Point(x - Length.FromMillimeters(200), y + Length.FromMillimeters(25)),
				ComponentDirection.Y when Value.Y < Force.Zero => new Point(x + Length.FromMillimeters(25),  y + Length.FromMillimeters(100)),
				ComponentDirection.Y when Value.Y > Force.Zero => new Point(x + Length.FromMillimeters(25),  y - Length.FromMillimeters(125)),
				_ => Position
			};
		}

		/// <summary>
		///     Get <see cref="Force" /> value from extended data.
		/// </summary>
		private PlaneForce? GetForce() => GetDictionary("Force").GetForce();

		protected override void SetProperties() => SetDictionary(Value.GetTypedValues(), "Force");

		#endregion

		#region Operators

		/// <summary>
		///     Returns true if objects are equal.
		/// </summary>
		public static bool operator == (ForceObject left, ForceObject right) => !(left is null) && left.Equals(right);

		/// <summary>
		///     Returns true if objects are different.
		/// </summary>
		public static bool operator != (ForceObject left, ForceObject right) => !(left is null) && !left.Equals(right);

		#endregion
	}

	//   /// <summary>
	//   ///     Force direction class.
	//   /// </summary>
	//   public class ForceObject : ConditionObject<ForceObject, Force>
	//{
	//	#region Properties

	//	public override Block Block => Block.ForceY;

	//	public override Layer Layer => Layer.Force;

	//	/// <summary>
	//	///     Get the rotation angle of the <see cref="BlockReference" />.
	//	/// </summary>
	//	protected override double RotationAngle =>
	//		Direction switch
	//		{
	//			ComponentDirection.Y when Value <= Force.Zero => 0,
	//			ComponentDirection.Y when Value >  Force.Zero => Constants.Pi,
	//			ComponentDirection.X when Value <= Force.Zero => Constants.PiOver2,
	//			_                                             => Constants.Pi3Over2
	//		};

	//	/// <summary>
	//	///     Get/set the <see cref="TextCreator" /> for this object.
	//	/// </summary>
	//	public TextCreator Text { get; private set; }


	//	/// <summary>
	//	///     Get the insertion point of the associated text.
	//	/// </summary>
	//	private Point TextInsertionPoint(ComponentDirection direction)
	//	{
	//			var x = Position.X;
	//			var y = Position.Y;

	//			return direction switch
	//			{
	//				ComponentDirection.X when Value > Force.Zero => new Point(x - Length.FromMillimeters(200), y + Length.FromMillimeters(25)),
	//				ComponentDirection.X when Value < Force.Zero => new Point(x + Length.FromMillimeters(75),  y + Length.FromMillimeters(25)),
	//				ComponentDirection.Y when Value > Force.Zero => new Point(x + Length.FromMillimeters(25),  y - Length.FromMillimeters(125)),
	//				ComponentDirection.Y when Value < Force.Zero => new Point(x + Length.FromMillimeters(25),  y + Length.FromMillimeters(100)),
	//				_ => Position
	//			};
	//	}

	//	#endregion

	//	#region Constructors

	//	/// <summary>
	//	///     Create the force object.
	//	/// </summary>
	//	/// <param name="position">The <see cref="Point" /> position.</param>
	//	/// <param name="force">The <see cref="Force" /> applied in <paramref name="position" />.</param>
	//	/// <param name="direction">The <seealso cref="ComponentDirection" /> of <paramref name="force"/>. Only <seealso cref="ComponentDirection.X"/> of <seealso cref="ComponentDirection.Y"/></param>
	//	public ForceObject(Point position, Force force, ComponentDirection direction)
	//		: base(position, force, direction)
	//	{
	//		Text = GetText();
	//	}

	//	#endregion

	//	#region  Methods

	//	/// <summary>
	//	///     Read a <see cref="ForceObject" /> from an <see cref="ObjectId" />.
	//	/// </summary>
	//	/// <param name="forceObjectId">The <see cref="ObjectId" /> of the force.</param>
	//	public static ForceObject? ReadFromObjectId(ObjectId forceObjectId) => ReadFromBlock((BlockReference) forceObjectId.GetEntity());

	//	/// <summary>
	//	///     Read a <see cref="ForceObject" /> from a <see cref="BlockReference" />.
	//	/// </summary>
	//	/// <param name="reference">The <see cref="BlockReference" /> object of the force.</param>
	//	public static ForceObject? ReadFromBlock(BlockReference? reference) =>
	//		reference is null
	//			? null
	//			: new ForceObject(reference.Position.ToPoint(Settings.Units.Geometry), Force.Zero, GetDirectionFromAngle(reference))
	//			{
	//				ObjectId = reference.ObjectId
	//			};

	//	/// <summary>
	//	///     Get the <see cref="Direction" /> from a <seealso cref="BlockReference" />'s rotation angle.
	//	/// </summary>
	//	/// <param name="reference">The <see cref="BlockReference" /> object.</param>
	//	public static ComponentDirection GetDirectionFromAngle(BlockReference? reference) =>
	//		reference?.Rotation switch
	//		{
	//			{ } x when x.ApproxZero(1E-3) || x.Approx(Constants.Pi, 1E-3) => ComponentDirection.X,
	//			_                                                             => ComponentDirection.Y
	//		};

	//	/// <summary>
	//	///     Create XData for forces
	//	/// </summary>
	//	/// <param name="force">The force value.</param>
	//	/// <param name="forceDirection">The force direction.</param>
	//	public static TypedValue[] CreateXData(Force force, ComponentDirection forceDirection)
	//	{
	//		// Definition for the Extended Data
	//		var xdataStr = "Force Data";

	//		// Get the Xdata size
	//		var size  = Enum.GetNames(typeof(ForceIndex)).Length;
	//		var data = new TypedValue[size];

	//		// Set values
	//		data[(int) ForceIndex.AppName]    = new TypedValue((int) DxfCode.ExtendedDataRegAppName,  AppName);
	//		data[(int) ForceIndex.XDataStr]   = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
	//		data[(int) ForceIndex.ValueX]      = new TypedValue((int) DxfCode.ExtendedDataReal,        force.Newtons);
	//		data[(int) ForceIndex.ValueY]  = new TypedValue((int) DxfCode.ExtendedDataInteger32,  (int) forceDirection);

	//		// Add XData to force block
	//		return data;
	//	}

	//	/// <summary>
	//	///     Get this object as a <see cref="PlaneForce" />.
	//	/// </summary>
	//	public PlaneForce AsPlaneForce() => Direction is ComponentDirection.X
	//		? PlaneForce.InX(Value)
	//		: PlaneForce.InY(Value);

	//	public override void AddToDrawing()
	//	{
	//		base.AddToDrawing();
	//		Text.AddToDrawing();
	//	}

	//	public override void GetProperties()
	//	{
	//		var data = ReadXData();

	//		Value = GetForce(data);

	//		Direction = GetDirection(data);

	//		Text = GetText();
	//	}

	//	public override void RemoveFromDrawing() => new[] {ObjectId, Text.ObjectId}.RemoveFromDrawing();

	//	/// <summary>
	//	///		Get a <see cref="TextCreator"/> based in this object's properties.
	//	/// </summary>
	//	/// <returns></returns>
	//	private TextCreator GetText() => new TextCreator(TextInsertionPoint, Layer.ForceText, $"{Value.ToUnit(Settings.Units.AppliedForces).Value:0.00}");

	//	/// <summary>
	//	///     Get <see cref="Force" /> value from extended data.
	//	/// </summary>
	//	/// <param name="data">The extended data of this object. Leave null to read.</param>
	//	private Force GetForce(TypedValue[]? data = null) =>
	//		Force.FromNewtons((data ?? ReadXData())?[(int) ForceIndex.ValueX].ToDouble() ?? 0).ToUnit(Settings.Units.AppliedForces);

	//	/// <summary>
	//	///     Get the force <see cref="ComponentDirection" /> from extended data.
	//	/// </summary>
	//	/// <inheritdoc cref="GetForce" />
	//	private ComponentDirection GetDirection(TypedValue[]? data = null) =>
	//		(ComponentDirection) ((data ?? ReadXData())?[(int) ForceIndex.ValueY].ToInt() ?? 0);

	//	protected override TypedValue[] CreateXData() => CreateXData(Value, Direction);

	//	public override bool Equals(ForceObject other) => base.Equals(other) && Direction == other.Direction;

	//	#endregion

	//	#region Operators

	//	/// <summary>
	//	///     Returns true if objects are equal.
	//	/// </summary>
	//	public static bool operator == (ForceObject left, ForceObject right) => !(left is null) && left.Equals(right);

	//	/// <summary>
	//	///     Returns true if objects are different.
	//	/// </summary>
	//	public static bool operator != (ForceObject left, ForceObject right) => !(left is null) && !left.Equals(right);

	//	#endregion
	//}
}