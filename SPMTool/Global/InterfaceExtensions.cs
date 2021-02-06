using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Extensions.Interface
{
    /// <summary>
    /// Extensions for interface elements.
    /// </summary>
    public static class Extensions
    {
	    /// <summary>
	    /// Disable this <paramref name="element"/>.
	    /// </summary>
	    public static void Disable(this UIElement element) => element.IsEnabled = false;

        /// <summary>
        /// Enable this <paramref name="element"/>.
        /// </summary>
        public static void Enable(this UIElement element) => element.IsEnabled = true;

        /// <summary>
        /// Disable these <paramref name="elements"/>.
        /// </summary>
        public static void Disable(this IEnumerable<UIElement> elements)
        {
			foreach(var element in elements)
				element.Disable();
        }

        /// <summary>
        /// Enable these <paramref name="elements"/>.
        /// </summary>
        public static void Enable(this IEnumerable<UIElement> elements)
        {
			foreach(var element in elements)
				element.Enable();
        }
    }
}
