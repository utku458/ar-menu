using ArMenu.Domain.Common;

namespace ArMenu.Domain.ArModels;

public static class ArModelProcessingErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "ar_model_processing.not_found", "The menu item has no model processing.");

    public static readonly Error AlreadyFinished = Error.Conflict(
        "ar_model_processing.finished", "The processing has already finished.");

    public static readonly Error NotStarted = Error.Conflict(
        "ar_model_processing.not_started", "The processing has not been started by a worker.");

    public static readonly Error FailureCodeInvalid = Error.Validation(
        "ar_model_processing.failure_code_invalid",
        FormattableString.Invariant($"Failure codes are required and cannot exceed {ArModelProcessing.FailureCodeMaxLength} characters."));

    /// <summary>Every attempt failed for reasons unrelated to the file, such as the processor being down.</summary>
    public static readonly Error Unavailable = Error.Validation(
        "asset.processing_unavailable", "The model could not be processed right now. Upload it again later.");
}
