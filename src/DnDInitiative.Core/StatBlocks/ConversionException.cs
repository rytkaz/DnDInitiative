namespace DnDInitiative.Core.StatBlocks;

/// <summary>A conversion failed for a reason worth showing to the user as it is.</summary>
public sealed class ConversionException : Exception
{
    public ConversionException()
    {
    }

    public ConversionException(string message)
        : base(message)
    {
    }

    public ConversionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}