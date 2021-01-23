using Autodesk.AutoCAD.DatabaseServices;

namespace SPMTool.Database.Elements
{
	/// <summary>
	/// Interface for SPM objects
	/// </summary>
	public interface ISPMObject
	{
		/// <summary>
		/// Get/set the <see cref="ObjectId"/>
		/// </summary>
		ObjectId ObjectId { get; set; }

		/// <summary>
		/// Get/set the object number.
		/// </summary>
		int Number { get; set; }
	}
}