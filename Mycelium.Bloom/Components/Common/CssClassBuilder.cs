namespace Mycelium.Bloom.Components.Common
{
    /// <summary>
    /// Builds a CSS class string by conditionally adding class names.
    /// </summary>
    public class CssClassBuilder
    {
        private readonly List<string> classes = [];

        /// <summary>
        /// Adds a CSS class when the provided condition is true and the class is not empty.
        /// </summary>
        /// <param name="cssClass">The CSS class to add.</param>
        /// <param name="condition">A value indicating whether the CSS class should be added.</param>
        /// <returns>The current CSS class builder instance.</returns>
        public CssClassBuilder Add(string cssClass, bool condition = true)
        {
            if (condition && !string.IsNullOrWhiteSpace(cssClass))
            {
                this.classes.Add(cssClass);
            }

            return this;
        }

        /// <summary>
        /// Builds the final CSS class string.
        /// </summary>
        /// <returns>The CSS classes separated by spaces.</returns>
        public override string ToString()
        {
            var cssClass = string.Join(" ", this.classes);

            return cssClass;
        }
    }
}
