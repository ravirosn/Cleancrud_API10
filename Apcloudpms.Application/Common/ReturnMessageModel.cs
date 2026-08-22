namespace Apcloudpms.Application.Common;

public sealed class ReturnMessageModel
{
    public bool IsSuccess { get; set; }

    public string ReturnMessage { get; set; } = string.Empty;

    public int HttpStatusCode { get; set; }
}
