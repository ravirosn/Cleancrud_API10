using System.Net;

namespace Apcloud.Web.Services.Authentication;

public sealed class AuthApiException(HttpStatusCode statusCode, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
