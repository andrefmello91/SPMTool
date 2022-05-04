using System;

namespace SPMTool.Global
{
	public enum ObjectModification
	{
		Unappended,
		Reappended
	}

	public abstract class ObjectModifiedEventArgs : EventArgs
	{

		#region Properties

		public abstract ObjectModification Modification { get; }

		#endregion

	}

	public class ObjectUnappendedEventArgs : ObjectModifiedEventArgs
	{

		#region Properties

		public override ObjectModification Modification => ObjectModification.Unappended;

		#endregion

	}

	public class ObjectReappendedEventArgs : ObjectModifiedEventArgs
	{

		#region Properties

		public override ObjectModification Modification => ObjectModification.Reappended;

		#endregion

	}

	public delegate void ObjectModifiedEventHandler(object sender, ObjectModifiedEventArgs e);


}