using System;
using Autodesk.AutoCAD.DatabaseServices;
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
	///// <summary>
	/////		Force object class.
	///// </summary>
	//public class ForceObject : ConditionObject<ForceObject, PlaneForce>
	//{
	//	//#region Fields

	//	///// <summary>
	//	/////     The horizontal <see cref="ForceDirection" />.
	//	///// </summary>
	//	//private readonly ForceDirection? _x;

	//	///// <summary>
	//	/////     The vertical <see cref="ForceDirection" />.
	//	///// </summary>
	//	//private readonly ForceDirection? _y;

	//	//#endregion

	//	//#region Properties

	//	//public override Block Block => Block.Force;

	//	//public override PlaneForce Value => new PlaneForce(_x?.Value ?? Force.Zero, _y?.Value ?? Force.Zero);

	//	//public override Layer Layer => Layer.Force;

	//	///// <summary>
	//	/////		Get the <see cref="ObjectId"/>'s associated to this object.
	//	///// </summary>
	//	//public ObjectId[] ObjectIds => new []
	//	//{
	//	//	_x?.ObjectId     ?? ObjectId.Null,
	//	//	_y?.ObjectId     ?? ObjectId.Null,
	//	//	_x?.TextObjectId ?? ObjectId.Null,
	//	//	_y?.TextObjectId ?? ObjectId.Null
	//	//};

	//	//protected override double RotationAngle { get; }

	//	//#endregion

	//	//#region Constructors

	//	///// <summary>
	//	/////     Force object constructor.
	//	///// </summary>
	//	///// <inheritdoc />
	//	//public ForceObject(Point position, PlaneForce value)
	//	//	: base(position, value)
	//	//{
	//	//	_x = value.IsXZero
	//	//		? null
	//	//		: new ForceDirection(position, value.X, Direction.X);

	//	//	_y = value.IsYZero
	//	//		? null
	//	//		: new ForceDirection(position, value.Y, Direction.Y);
	//	//}

	//	//private ForceObject(ForceDirection? x, ForceDirection? y)
	//	//{
	//	//	_x = x;
	//	//	_y = y;
	//	//}

	//	//#endregion

	//	//#region  Methods

	//	//public static ForceObject? ReadFromBlocks(IEnumerable<BlockReference>? blocks)
	//	//{

	//	//}

	//	//public override void AddToDrawing()
	//	//{
	//	//	_x?.AddToDrawing();
	//	//	_y?.AddToDrawing();
	//	//}

	//	//public override void RemoveFromDrawing() => ObjectIds.RemoveFromDrawing();

	//	//protected override TypedValue[] CreateXData() => ForceDirection.CreateXData(Force.Zero, Direction.X);

	//	//public override void GetProperties()
	//	//{
	//	//	_x?.GetProperties();
	//	//	_y?.GetProperties();
	//	//}

	//	//#endregion

	//	///// <summary>
	//	/////     Returns true if objects are equal.
	//	///// </summary>
	//	//public static bool operator ==(ForceObject left, ForceObject right) => !(left is null) && left.Equals(right);

	//	///// <summary>
	//	/////     Returns true if objects are different.
	//	///// </summary>
	//	//public static bool operator !=(ForceObject left, ForceObject right) => !(left is null) && !left.Equals(right);
	//}

	/// <summary>
	///     Force direction class.
	/// </summary>
	public class ForceObject : ConditionObject<ForceObject, Force, Direction>
	{
		#region Properties

		public override Block Block => Block.Force;

		public override Layer Layer => Layer.Force;

		/// <summary>
		///     Get the rotation angle of the <see cref="BlockReference" />.
		/// </summary>
		protected override double RotationAngle =>
			Direction switch
			{
				Direction.Y when Value <= Force.Zero => 0,
				Direction.Y when Value >  Force.Zero => Constants.Pi,
				Direction.X when Value <= Force.Zero => Constants.PiOver2,
				_                                    => Constants.Pi3Over2
			};

		/// <summary>
		///     Get/set the <see cref="TextCreator" /> for this object.
		/// </summary>
		public TextCreator Text { get; private set; }

		/// <summary>
		///     Get the insertion point of the associated text.
		/// </summary>
		private Point TextInsertionPoint
		{
			get
			{
				var x = Position.X;
				var y = Position.Y;

				return Direction switch
				{
					Direction.X when Value > Force.Zero => new Point(x - Length.FromMillimeters(200), y + Length.FromMillimeters(25)),
					Direction.X when Value < Force.Zero => new Point(x + Length.FromMillimeters(75),  y + Length.FromMillimeters(25)),
					Direction.Y when Value > Force.Zero => new Point(x + Length.FromMillimeters(25),  y - Length.FromMillimeters(125)),
					Direction.Y when Value < Force.Zero => new Point(x + Length.FromMillimeters(25),  y + Length.FromMillimeters(100)),
					_ => Position
				};
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		///     Create the force object.
		/// </summary>
		/// <param name="position">The <see cref="Point" /> position.</param>
		/// <param name="force">The <see cref="Force" /> applied in <paramref name="position" />.</param>
		/// <param name="direction">The <seealso cref="Enums.Direction" /> of <paramref name="force" /></param>
		public ForceObject(Point position, Force force, Direction direction)
			: base(position, force, direction)
		{
			Text      = GetText();
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Read a <see cref="ForceObject" /> from an <see cref="ObjectId" />.
		/// </summary>
		/// <param name="forceObjectId">The <see cref="ObjectId" /> of the force.</param>
		public static ForceObject? ReadFromObjectId(ObjectId forceObjectId) => ReadFromBlock((BlockReference) forceObjectId.GetEntity());

		/// <summary>
		///     Read a <see cref="ForceObject" /> from a <see cref="BlockReference" />.
		/// </summary>
		/// <param name="reference">The <see cref="BlockReference" /> object of the force.</param>
		public static ForceObject? ReadFromBlock(BlockReference? reference) =>
			reference is null
				? null
				: new ForceObject(reference.Position.ToPoint(Settings.Units.Geometry), Force.Zero, GetDirectionFromAngle(reference))
				{
					ObjectId = reference.ObjectId
				};

		/// <summary>
		///     Get the <see cref="Direction" /> from a <seealso cref="BlockReference" />'s rotation angle.
		/// </summary>
		/// <param name="reference">The <see cref="BlockReference" /> object.</param>
		public static Direction GetDirectionFromAngle(BlockReference? reference) =>
			reference?.Rotation switch
			{
				{ } x when x.ApproxZero(1E-3) || x.Approx(Constants.Pi, 1E-3) => Direction.X,
				_                                                             => Direction.Y
			};

		/// <summary>
		///     Create XData for forces
		/// </summary>
		/// <param name="force">The force value.</param>
		/// <param name="forceDirection">The force direction.</param>
		public static TypedValue[] CreateXData(Force force, Direction forceDirection)
		{
			// Definition for the Extended Data
			var xdataStr = "Force Data";

			// Get the Xdata size
			var size  = Enum.GetNames(typeof(ForceIndex)).Length;
			var data = new TypedValue[size];

			// Set values
			data[(int) ForceIndex.AppName]    = new TypedValue((int) DxfCode.ExtendedDataRegAppName,  AppName);
			data[(int) ForceIndex.XDataStr]   = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
			data[(int) ForceIndex.Value]      = new TypedValue((int) DxfCode.ExtendedDataReal,        force.Newtons);
			data[(int) ForceIndex.Direction]  = new TypedValue((int) DxfCode.ExtendedDataInteger32,  (int) forceDirection);

			// Add XData to force block
			return data;
		}

		/// <summary>
		///     Get this object as a <see cref="PlaneForce" />.
		/// </summary>
		public PlaneForce AsPlaneForce() => Direction is Direction.X
			? PlaneForce.InX(Value)
			: PlaneForce.InY(Value);

		public override void AddToDrawing()
		{
			base.AddToDrawing();
			Text.AddToDrawing();
		}

		public override void GetProperties()
		{
			var data = ReadXData();

			Value = GetForce(data);

			Direction = GetDirection(data);

			Text = GetText();
		}

		public override void RemoveFromDrawing() => new[] {ObjectId, Text.ObjectId}.RemoveFromDrawing();

		/// <summary>
		///		Get a <see cref="TextCreator"/> based in this object's properties.
		/// </summary>
		/// <returns></returns>
		private TextCreator GetText() => new TextCreator(TextInsertionPoint, Layer.ForceText, $"{Value.ToUnit(Settings.Units.AppliedForces).Value:0.00}");

		/// <summary>
		///     Get <see cref="Force" /> value from extended data.
		/// </summary>
		/// <param name="data">The extended data of this object. Leave null to read.</param>
		private Force GetForce(TypedValue[]? data = null) =>
			Force.FromNewtons((data ?? ReadXData())?[(int) ForceIndex.Value].ToDouble() ?? 0).ToUnit(Settings.Units.AppliedForces);

		/// <summary>
		///     Get the force <see cref="Enums.Direction" /> from extended data.
		/// </summary>
		/// <inheritdoc cref="GetForce" />
		private Direction GetDirection(TypedValue[]? data = null) =>
			(Direction) ((data ?? ReadXData())?[(int) ForceIndex.Direction].ToInt() ?? 0);

		protected override TypedValue[] CreateXData() => CreateXData(Value, Direction);

		public override bool Equals(ForceObject other) => base.Equals(other) && Direction == other.Direction;

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
}