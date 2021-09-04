using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Enums;
#nullable enable

namespace SPMTool.Core
{
	/// <summary>
	///     Base class for extended objects.
	/// </summary>
	public abstract class ExtendedObject : IDBObjectCreator
	{

		#region Fields

		protected ObjectId _objectId = ObjectId.Null;

		#endregion

		#region Properties

		/// <summary>
		///     Get/set the <see cref="Autodesk.AutoCAD.DatabaseServices.ObjectId" /> of this object's extended dictionary.
		/// </summary>
		public ObjectId DictionaryId { get; protected set; } = ObjectId.Null;

		#endregion

		#region Constructors

		/// <summary>
		///     Extended object base constructor.
		/// </summary>
		/// <param name="blockTableId">The <see cref="ObjectId" /> of the block table that contains this object.</param>
		protected ExtendedObject(ObjectId blockTableId) => BlockTableId = blockTableId;

		#endregion

		#region Methods

		/// <summary>
		///     Attach an <see cref="Autodesk.AutoCAD.DatabaseServices.ObjectId" /> to this object.
		/// </summary>
		/// <param name="objectId">The <see cref="Autodesk.AutoCAD.DatabaseServices.ObjectId" /> to attach.</param>
		public void AttachObject(ObjectId objectId)
		{
			if (objectId.IsNull)
				return;

			// Set value
			_objectId = objectId;

			// Set dictionary id
			DictionaryId = objectId.GetExtendedDictionaryId();

			if (DictionaryId.IsNull)
				SetProperties();
			else
				GetProperties();
		}

		/// <summary>
		///     Read the extended dictionary associated to this object.
		/// </summary>
		/// <param name="dataName">The name of the required record.</param>
		protected TypedValue[]? GetDictionary(string dataName) => DictionaryId.GetDataFromDictionary(dataName);

		/// <summary>
		///     Get properties from the extended dictionary for this object.
		/// </summary>
		/// <returns>
		///     True if properties were successfully read from object dictionary.
		/// </returns>
		protected abstract void GetProperties();

		/// <summary>
		///     Create the extended dictionary for this object.
		/// </summary>
		/// <param name="data">The collection of <see cref="TypedValue" /> to set at <paramref name="dataName" />.</param>
		/// <param name="dataName">The name to set to the record.</param>
		/// <param name="overwrite">Overwrite record if it already exists?</param>
		protected void SetDictionary(IEnumerable<TypedValue>? data, string dataName, bool overwrite = true)
		{
			if (DictionaryId.IsNull)
				DictionaryId = ObjectId.SetExtendedDictionary(data, dataName, overwrite);

			else
				DictionaryId.SetDataOnDictionary(data, dataName, overwrite);
		}

		/// <summary>
		///     Set properties for the extended dictionary for this object.
		/// </summary>
		/// <returns>
		///     True if properties were successfully set to object dictionary.
		/// </returns>
		protected abstract void SetProperties();

		#endregion

		#region Interface Implementations

		/// <inheritdoc />
		public ObjectId BlockTableId { get; set; }

		/// <inheritdoc />
		public abstract Layer Layer { get; }

		/// <inheritdoc />
		public abstract string Name { get; }

		/// <inheritdoc />
		public ObjectId ObjectId
		{
			get => _objectId;
			set => AttachObject(value);
		}

		#endregion

		#region Interface Implementations

		/// <inheritdoc />
		public abstract DBObject CreateObject();

		/// <inheritdoc />
		public virtual DBObject? GetObject() => BlockTableId.Database.GetObject(ObjectId);

		#endregion

	}
}