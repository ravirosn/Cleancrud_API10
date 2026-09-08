namespace Apcloudpms.Application.Common;

public sealed class ForbiddenAccessException(string message) : Exception(message);
