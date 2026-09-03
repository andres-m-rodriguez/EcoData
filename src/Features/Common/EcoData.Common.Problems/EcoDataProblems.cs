using System.Net;

namespace EcoData.Common.Problems;

/// <summary>
/// Factory extension members creating common <see cref="EcoDataProblemDetails"/> shapes.
/// </summary>
public static class EcoDataProblems
{
    extension(EcoDataProblemDetails)
    {
        /// <summary>Creates a 400 problem carrying per-field validation errors.</summary>
        public static EcoDataProblemDetails Validation(
            IReadOnlyDictionary<string, string[]> errors,
            string? detail = null,
            string? instance = null)
        {
            ArgumentNullException.ThrowIfNull(errors);
            return new EcoDataProblemDetails
            {
                Type = ProblemTypes.Validation,
                Title = "One or more validation errors occurred.",
                Status = HttpStatusCode.BadRequest,
                Detail = detail,
                Instance = instance,
                Errors = errors,
            };
        }

        /// <summary>Creates a 404 problem.</summary>
        public static EcoDataProblemDetails NotFound(string? detail = null, string? instance = null) => new()
        {
            Type = ProblemTypes.NotFound,
            Title = "The requested resource was not found.",
            Status = HttpStatusCode.NotFound,
            Detail = detail,
            Instance = instance,
        };

        /// <summary>Creates a 401 problem.</summary>
        public static EcoDataProblemDetails Unauthorized(string? detail = null, string? instance = null) => new()
        {
            Type = ProblemTypes.Unauthorized,
            Title = "Authentication is required.",
            Status = HttpStatusCode.Unauthorized,
            Detail = detail,
            Instance = instance,
        };

        /// <summary>Creates a 403 problem.</summary>
        public static EcoDataProblemDetails Forbidden(string? detail = null, string? instance = null) => new()
        {
            Type = ProblemTypes.Forbidden,
            Title = "You are not allowed to perform this action.",
            Status = HttpStatusCode.Forbidden,
            Detail = detail,
            Instance = instance,
        };

        /// <summary>Creates a 409 problem.</summary>
        public static EcoDataProblemDetails Conflict(string? detail = null, string? instance = null) => new()
        {
            Type = ProblemTypes.Conflict,
            Title = "The request conflicts with the current state of the resource.",
            Status = HttpStatusCode.Conflict,
            Detail = detail,
            Instance = instance,
        };

        /// <summary>Creates a status-zero problem for a request that never reached the server.</summary>
        public static EcoDataProblemDetails Unreachable(string? detail = null) => new()
        {
            Type = ProblemTypes.Unreachable,
            Title = "The server could not be reached.",
            Status = 0,
            Detail = detail,
        };

        /// <summary>Creates a status-zero problem for a request the client stopped waiting for.</summary>
        public static EcoDataProblemDetails Timeout(string? detail = null) => new()
        {
            Type = ProblemTypes.Timeout,
            Title = "The server took too long to answer.",
            Status = 0,
            Detail = detail,
        };

        /// <summary>Creates a 500 problem carrying an optional correlation identifier.</summary>
        public static EcoDataProblemDetails Internal(string? traceId = null, string? instance = null) => new()
        {
            Type = ProblemTypes.Internal,
            Title = "An unexpected error occurred.",
            Status = HttpStatusCode.InternalServerError,
            TraceId = traceId,
            Instance = instance,
        };
    }
}
