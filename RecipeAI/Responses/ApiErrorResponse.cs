using System.Net;

namespace RecipeAI.Responses;

public class ApiErrorResponse
{

    // Default set to InternalServerError (500)

    public int StatusCode { get; } = (int)HttpStatusCode.InternalServerError;

    public string Message { get; }

    public string TraceId { get; set; }

    public object Data { get; set; }

    public ApiErrorResponse(string message)
    {
        Message = message;
    }

    public ApiErrorResponse(HttpStatusCode statusCode, string message = null)
    {
        StatusCode = (int)statusCode;
        Message = message ?? GetDefaultMessageForStatusCode(statusCode);
    }

    public ApiErrorResponse(HttpStatusCode statusCode, object data = null)
    {
        StatusCode = (int)statusCode;
        Message = GetDefaultMessageForStatusCode(statusCode);
        Data = data;
    }

    public ApiErrorResponse(HttpStatusCode statusCode, string message = null, object data = null)
    {
        StatusCode = (int)statusCode;
        Message = message ?? GetDefaultMessageForStatusCode(statusCode);
        Data = data;
    }

    public ApiErrorResponse(HttpStatusCode statusCode, string traceId, string message = null, object data = null)
    {
        StatusCode = (int)statusCode;
        TraceId = traceId;
        Message = message ?? GetDefaultMessageForStatusCode(statusCode);
        Data = data;
    }

    public ApiErrorResponse(Exception ex)
    {
        Message = ex.Message;
        Data = ex.InnerException is not null
            ? new
            {
                InnerException = new
                {
                    ex.InnerException.Message
                }
            }
            : null;
    }

    public ApiErrorResponse(HttpStatusCode statusCode, Exception ex)
    {
        StatusCode = (int)statusCode;
        Message = ex.Message;
        Data = ex.InnerException is not null
            ? new
            {
                InnerException = new
                {
                    ex.InnerException.Message
                }
            }
            : null;
    }

    public ApiErrorResponse(HttpStatusCode statusCode, string traceId, Exception ex)
    {
        StatusCode = (int)statusCode;
        TraceId = traceId;
        Message = ex.Message;
        Data = ex.InnerException is not null
            ? new
            {
                InnerException = new
                {
                    ex.InnerException.Message
                }
            }
            : null;
    }

    private static string GetDefaultMessageForStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "Server can't process the request.",
            HttpStatusCode.UnprocessableEntity => "Validation failed.",
            HttpStatusCode.NotFound => "Resource not found.",
            HttpStatusCode.Forbidden => "You don't have permission to perform this request.",
            HttpStatusCode.InternalServerError => "An unhandled error occurred.",
            _ => null,
        };
    }
}