namespace ArMenu.Domain.ArModels;

public enum ArModelProcessingStatus
{
    /// <summary>Waiting for a worker, for the first time or before a retry.</summary>
    Queued = 1,

    /// <summary>A worker is turning the upload into the model's files.</summary>
    Processing = 2,

    /// <summary>The files were published and attached to the menu item.</summary>
    Succeeded = 3,

    /// <summary>The upload cannot become a model, or processing kept failing.</summary>
    Failed = 4,

    /// <summary>A newer upload for the same item replaced this one before it finished.</summary>
    Superseded = 5,
}
