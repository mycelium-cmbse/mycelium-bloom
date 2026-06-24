namespace Mycelium.Bloom.Model
{
    /// <summary>
    /// Defines the available chip visual variants.
    /// </summary>
    public enum ChipVariant
    {
        /// <summary>
        /// Represents the default neutral chip style.
        /// </summary>
        Default,

        /// <summary>
        /// Represents a success chip style.
        /// </summary>
        Success,

        /// <summary>
        /// Represents a warning chip style.
        /// </summary>
        Warning,

        /// <summary>
        /// Represents a danger chip style used for critical or negative states.
        /// </summary>
        Danger,

        /// <summary>
        /// Represents an informational chip style.
        /// </summary>
        Info,

        /// <summary>
        /// Represents a chip style used for ownership indicators.
        /// </summary>
        Ownership,

        /// <summary>
        /// Represents a chip style used for lifecycle state indicators.
        /// </summary>
        Lifecycle
    }
}
