namespace Apcloudpms.Application.DTOs;

public enum ChangePasswordOutcome
{
    Success,
    UserNotFound,
    CurrentPasswordIncorrect,
    ExternallyManagedAccount,
    InvalidPassword
}
