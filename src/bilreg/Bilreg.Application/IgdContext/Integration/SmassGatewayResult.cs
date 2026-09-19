namespace Bilreg.Application.IgdContext.Integration;

/// <summary>
/// Outcome of one outbound SMASS operation (architecture §6.2). The gateway never
/// throws into the caller; every failure is represented here (BR-10).
/// <see cref="AssessmentId"/> is only meaningful for a successful generate call.
/// </summary>
public record SmassGatewayResult(bool Success, string? AssessmentId, string? ErrorMessage);
