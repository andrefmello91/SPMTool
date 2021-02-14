using System;
using Autodesk.AutoCAD.DatabaseServices;
using MathNet.Numerics;
using OnPlaneComponents;
using SPMTool.Enums;
using SPMTool.Extensions;
using static SPMTool.Database.DataBase;

namespace SPMTool.Database.Conditions
{
	/// <summary>
	///     Constraint object class.
	/// </summary>
	public class ConstraintObject : ConditionObject<ConstraintObject, Constraint>
	{
		#region Properties

		public override Block Block =>
			Value.Direction switch
			{
				ConstraintDirection.Both => Block.SupportXY,
				_                        => Block.SupportY
			};

		public override Layer Layer => Layer.Support;

		/// <summary>
		///     Get the rotation angle of the block.
		/// </summary>
		protected override double RotationAngle =>
			Value.Direction switch
			{
				ConstraintDirection.X => Constants.PiOver2,
				_                     => 0
			};

		#endregion

		#region Constructors

		/// <summary>
		///     Constraint object constructor.
		/// </summary>
		/// <inheritdoc />
		public ConstraintObject(Point position, Constraint value) : base(position, value)
		{
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Read a <see cref="ConstraintObject" /> from an <see cref="ObjectId" />.
		/// </summary>
		/// <param name="forceObjectId">The <see cref="ObjectId" /> of the force.</param>
		public static ConstraintObject? ReadFromObjectId(ObjectId forceObjectId) => ReadFromBlock((BlockReference) forceObjectId.GetEntity());

		/// <summary>
		///     Read a <see cref="ConstraintObject" /> from a <see cref="BlockReference" />.
		/// </summary>
		/// <param name="reference">The <see cref="BlockReference" /> object of the force.</param>
		public static ConstraintObject? ReadFromBlock(BlockReference? reference)
		{
			if (reference is null)
				return null;

			// Read the XData and get the necessary data
			var data = reference.ReadXData();

			// Get value and direction
			var direction  = (ConstraintDirection) data[(int) ForceIndex.Direction].ToInt();
			var constraint = Constraint.FromDirection(direction);

			return
				new ConstraintObject(reference.Position.ToPoint(Settings.Units.Geometry), constraint)
				{
					ObjectId = reference.ObjectId
				};
		}

		/// <summary>
		///     Create XData for supports.
		/// </summary>
		/// <param name="direction">The <see cref="Constraint" /> type.</param>
		public static TypedValue[] CreateXData(ConstraintDirection direction)
		{
			// Definition for the Extended Data
			string xdataStr = "SupportDirection Data";

			// Get the Xdata size
			var size = Enum.GetNames(typeof(SupportIndex)).Length;
			var data = new TypedValue[size];

			// Set values
			data[(int) SupportIndex.AppName]   = new TypedValue((int) DxfCode.ExtendedDataRegAppName, AppName);
			data[(int) SupportIndex.XDataStr]  = new TypedValue((int) DxfCode.ExtendedDataAsciiString, xdataStr);
			data[(int) SupportIndex.Direction] = new TypedValue((int) DxfCode.ExtendedDataInteger32, (int) direction);

			// Add XData to force block
			return data;
		}

		protected override TypedValue[] ConditionXData() => CreateXData(Value.Direction);

		#endregion

		#region Operators

		/// <summary>
		///     Returns true if objects are equal.
		/// </summary>
		public static bool operator == (ConstraintObject left, ConstraintObject right) => !(left is null) && left.Equals(right);

		/// <summary>
		///     Returns true if objects are different.
		/// </summary>
		public static bool operator != (ConstraintObject left, ConstraintObject right) => !(left is null) && !left.Equals(right);

		#endregion
	}
}