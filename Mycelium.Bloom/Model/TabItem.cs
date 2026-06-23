namespace Mycelium.Bloom.Model
{
    public sealed class TabItem
    {
        /// <summary>
        /// Gets or sets the unique tab value.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the visible tab label.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the tab item is disabled.
        /// </summary>
        public bool Disabled { get; set; }
    }
}
