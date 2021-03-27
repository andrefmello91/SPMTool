using System.Collections.Generic;
using System.Windows;
using andrefmello91.Extensions;

namespace SPMTool.Extensions
{
    /// <summary>
    /// Extensions for interface elements.
    /// </summary>
    public static class UIExtensions
    {
	    /// <summary>
	    /// Disable this <paramref name="element"/>.
	    /// </summary>
	    public static void Disable(this UIElement? element)
	    {
            if (element is null)
                return;

		    element.IsEnabled = false;
	    }

	    /// <summary>
        /// Enable this <paramref name="element"/>.
        /// </summary>
        public static void Enable(this UIElement? element)
	    {
		    if (element is null)
			    return;

		    element.IsEnabled = true;
	    }


        /// <summary>
        /// Disable these <paramref name="elements"/>.
        /// </summary>
        public static void Disable(this IEnumerable<UIElement>? elements)
        {
            if (elements.IsNullOrEmpty())
                return;

			foreach(var element in elements)
				element.Disable();
        }

        /// <summary>
        /// Enable these <paramref name="elements"/>.
        /// </summary>
        public static void Enable(this IEnumerable<UIElement>? elements)
        {
	        if (elements.IsNullOrEmpty())
		        return;

            foreach (var element in elements)
				element.Enable();
        }
    }
}
