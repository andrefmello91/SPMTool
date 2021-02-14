using System;
using Autodesk.AutoCAD.DatabaseServices;
using Extensions;
using MathNet.Numerics;
using OnPlaneComponents;
using SPMTool.Database.Elements;
using SPMTool.Enums;
using SPMTool.Extensions;
using UnitsNet;

using static SPMTool.Database.DataBase;
using static SPMTool.Units;

#nullable enable

// ReSharper disable once CheckNamespace
namespace SPMTool.Database.Conditions
{
	/// <summary>
	///		Force object class.
	/// </summary>
	public class ForceObject : ConditionObject<ForceObject, PlaneForce>
	{
		#region Fields

		/// <summary>
		///     The horizontal <see cref="ForceDirection" />.
		/// </summary>
		private readonly ForceDirection? _x;

		/// <summary>
		///     The vertical <see cref="ForceDirection" />.
		/// </summary>
		private readonly ForceDirection? _y;

		#endregion

		#region Properties

		public override Block Block => Block.Force;

		public override PlaneForce Value => new PlaneForce(_x?.Value ?? Force.Zero, _y?.Value ?? Force.Zero);

		public override Layer Layer => Layer.Force;

		/// <summary>
		///		Get the <see cref="ObjectId"/>'s associated to this object.
		/// </summary>
		public ObjectId[] ObjectIds => new []
		{
			_x?.ObjectId     ?? ObjectId.Null,
			_y?.ObjectId     ?? ObjectId.Null,
			_x?.TextObjectId ?? ObjectId.Null,
			_y?.TextObjectId ?? ObjectId.Null
		};

		protected override double RotationAngle { get; }

		#endregion

		#region Constructors

		/// <summary>
		///     Force object constructor.
		/// </summary>
		/// <inheritdoc />
		public ForceObject(Point position, PlaneForce value)
			: base(position, value)
		{
			_x = value.IsXZero
				? null
				: new ForceDirection(position, value.X, Direction.X);

			_y = value.IsYZero
				? null
				: new ForceDirection(position, value.Y, Direction.Y);
		}

		#endregion

		#region  Methods

		public override void AddToDrawing()
		{
			_x?.AddToDrawing();
			_y?.AddToDrawing();
		}

		#endregion

		/// <summary>
		///     Returns true if objects are equal.
		/// </summary>
		public static bool operator ==(ForceObject left, ForceObject right) => !(left is null) && left.Equals(right);

		/// <summary>
		///     Returns true if objects are different.
		/// </summary>
		public static bool operator !=(ForceObject left, ForceObject right) => !(left is null) && !left.Equals(right);

		/// <summary>
		///     Force direction class.
		/// </summary>
		private class ForceDirection : ConditionObject<ForceDirection, Force>
		{
			#region Fields

			private readonly TextCreator _text;

			#endregion

			#region Properties

			/// <summary>
			///     The force <seealso cref="Enums.Direction"/>.
			/// </summary>
			public Direction Direction { get; }

			public override Block Block => Block.Force;

			public override Layer Layer => Layer.Force;

			/// <summary>
			///		Get the <see cref="ObjectId"/> of the text associated to this object.
			/// </summary>
			public ObjectId TextObjectId => _text.ObjectId;

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
			///		Get the insertion point of the associated text.
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
						Direction.X when Value < Force.Zero => new Point(x + Length.FromMillimeters(75), y + Length.FromMillimeters(25)),
						Direction.Y when Value > Force.Zero => new Point(x + Length.FromMillimeters(25), y - Length.FromMillimeters(125)),
						Direction.Y when Value < Force.Zero => new Point(x + Length.FromMillimeters(25), y + Length.FromMillimeters(100)),
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
			/// <param name="direction">The <seealso cref="Enums.Direction" /> of <paramref name="value" /></param>
			public ForceDirection(Point position, Force force, Direction direction)
				: base(position, force)
			{
				Direction = direction;
				_text     = new TextCreator(TextInsertionPoint, Layer.ForceText, $"{force.ToUnit(Settings.Units.AppliedForces).Value:0.00}");
			}

			#endregion

			#region  Methods

			/// <summary>
			///     Read a <see cref="ForceDirection" /> from an <see cref="ObjectId" />.
			/// </summary>
			/// <param name="forceObjectId">The <see cref="ObjectId" /> of the force.</param>
			public static ForceDirection ReadFromObjectId(ObjectId forceObjectId) => ReadFromBlock((BlockReference) forceObjectId.GetEntity());

			/// <summary>
			///     Read a <see cref="ForceDirection" /> from a <see cref="BlockReference" />.
			/// </summary>
			/// <param name="reference">The <see cref="BlockReference" /> object of the force.</param>
			public static ForceDirection ReadFromBlock(BlockReference reference)
			{
				// Read the XData and get the necessary data
				var data = reference.ReadXData();

				// Get value and direction
				var force     = Force.FromNewtons(data[(int) ForceIndex.Value].ToDouble()).ToUnit(Settings.Units.AppliedForces);
				var direction = (Direction) data[(int) ForceIndex.Direction].ToInt();

				return
					new ForceDirection(reference.Position.ToPoint(Settings.Units.Geometry), force, direction)
					{
						ObjectId = reference.ObjectId
					};
			}

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

			public override void AddToDrawing()
			{
				base.AddToDrawing();
				_text.AddToDrawing();
			}

			protected override TypedValue[] ConditionXData() => CreateXData(Value, Direction);

			#endregion

			#region Operators

			/// <summary>
			///     Returns true if objects are equal.
			/// </summary>
			public static bool operator == (ForceDirection left, ForceDirection right) => !(left is null) && left.Equals(right);

			/// <summary>
			///     Returns true if objects are different.
			/// </summary>
			public static bool operator != (ForceDirection left, ForceDirection right) => !(left is null) && !left.Equals(right);

			#endregion
		}
	}
}