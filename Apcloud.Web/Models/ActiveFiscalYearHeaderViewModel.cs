namespace Apcloud.Web.Models;

public sealed record ActiveFiscalYearHeaderViewModel(
    int Id,
    string DisplayName,
    DateOnly StartDate,
    DateOnly EndDate);
