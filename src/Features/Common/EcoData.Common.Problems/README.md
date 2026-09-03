# EcoData.Common.Problems

RFC 9457 problem details for EcoData, ported from Pace. Nothing references it yet; it is the
target for retiring `EcoData.Common.Problems.Contracts` and the per-feature `ValidationFailed`
copies.

## Types

| Type | Purpose |
|------|---------|
| `EcoDataProblemDetails` | The wire payload: `type`, `title`, `status`, `detail`, `instance`, `traceId`, `errors`, plus extension data that round-trips. |
| `ProblemTypes` | Stable `urn:ecodata:problem:*` URIs. Clients discriminate on these, never on `errors` being non-empty. |
| `EcoDataProblems` | Factory extension members on `EcoDataProblemDetails`: `Validation`, `NotFound`, `Unauthorized`, `Forbidden`, `Conflict`, `Internal`. |
| `EcoDataProblemJsonContext` | Source-generated serializer context, so the payload is AOT-safe. |
| `EcoDataProblemException` | Thrown by `EnsureSuccessOrProblemAsync` when a response carries a problem. |
| `ProblemDetailsHttpExtensions` | `HttpResponseMessage` members: `ReadProblemAsync`, `ReadOneOfAsync<T>`, `EnsureSuccessOrProblemAsync`. A failed response always yields a problem, synthesised from the status code when the body has none. |
| `ValidationFailed` | Per-field errors with `AllMessages` for flat display. |
| `RequestFailed` | A failure with no usable problem payload. A `StatusCode` of zero (no `HttpStatusCode` member) means the request never reached the server. |

`EcoData.Common.Problems.AspNetCore` adds `ProblemResults`, the single place a typed error becomes
an `IResult` with the `application/problem+json` media type. Server-side only.

## Adopting it

1. Servers switch to `ProblemResults` so every failure carries a `type` URI.
2. Clients replace the try, parse, branch block with `ReadOneOfAsync` and discriminate on `ProblemTypes`.
3. Feature contracts drop their local `ValidationFailed` and use this one.
4. `EcoData.Common.Problems.Contracts` goes away.
