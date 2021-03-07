using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using SPMTool.Extensions;

#nullable enable

namespace SPMTool.Core
{
	/// <summary>
	///     Base class for extended dictionary creating.
	/// </summary>
	public abstract class DictionaryCreator
	{
		protected ObjectId _objectId = ObjectId.Null;

		#region Properties

		/// <summary>
		///     Get/set the <see cref="Autodesk.AutoCAD.DatabaseServices.ObjectId" /> of this object.
		/// </summary>
		public ObjectId ObjectId 
		{
			get => _objectId;
			set => AttachObject(value);
		}

		/// <summary>
		///     Get/set the <see cref="Autodesk.AutoCAD.DatabaseServices.ObjectId" /> of this object's extended dictionary.
		/// </summary>
		public ObjectId DictionaryId { get; protected set; } = ObjectId.Null;

		#endregion

		#region  Methods

		/// <summary>
		///     Get properties from the extended dictionary for this object.
		/// </summary>
		/// <returns>
		///		True if properties were successfully read from object dictionary.
		/// </returns>
		protected abstract bool GetProperties();

		/// <summary>
		///     Set properties for the extended dictionary for this object.
		/// </summary>
		/// <returns>
		///		True if properties were successfully set to object dictionary.
		/// </returns>
		protected abstract void SetProperties();

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
		///     Read the extended dictionary associated to this object.
		/// </summary>
		/// <param name="dataName">The name of the required record.</param>
		protected TypedValue[]? GetDictionary(string dataName) => DictionaryId.GetDataFromDictionary(dataName);

        #endregion

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

			if (!GetProperties())
	            SetProperties();
        }
    }
}