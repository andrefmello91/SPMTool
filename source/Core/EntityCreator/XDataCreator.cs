using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Extensions;

namespace SPMTool.Core
{
	/// <summary>
	///     Base class for extended data creating.
	/// </summary>
	public abstract class XDataCreator
	{
		#region Fields

		private ObjectId _id = ObjectId.Null;

		#endregion

		#region Properties

		/// <summary>
		///     Get/set the <see cref="Autodesk.AutoCAD.DatabaseServices.ObjectId" /> of this object.
		/// </summary>
		public ObjectId ObjectId
		{
			get => _id;
			set => AttachObject(value);
		}

		#endregion

		#region  Methods

		/// <summary>
		///     Get properties from the extended data for this object.
		/// </summary>
		public abstract void GetProperties();

		/// <summary>
		///     Attach an <see cref="Autodesk.AutoCAD.DatabaseServices.ObjectId" /> to this object.
		/// </summary>
		/// <param name="objectId">The <see cref="Autodesk.AutoCAD.DatabaseServices.ObjectId" /> to attach.</param>
		public void AttachObject(ObjectId objectId)
		{
			if (objectId.IsNull)
				return;

			// Id changed
			if (!_id.IsNull)
				objectId.SetXData(CreateXData());

			// First set, read data
			else
				GetProperties();

			_id = objectId;

			// Set the extended data
			_id.SetXData(CreateXData());
		}

		/// <summary>
		///     Create the extended data for this object.
		/// </summary>
		protected abstract TypedValue[] CreateXData();

		/// <summary>
		///     Read the XData associated to this object.
		/// </summary>
		protected virtual TypedValue[]? ReadXData() => ObjectId.ReadXData();

		#endregion
	}
}