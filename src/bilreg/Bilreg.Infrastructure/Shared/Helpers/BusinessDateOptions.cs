namespace Bilreg.Infrastructure.Shared.Helpers;

public class BusinessDateOptions
{
    public const string SECTION_NAME = "BusinessDate";

    public BusinessDateMode Mode { get; set; } = BusinessDateMode.System;
    public DateOnly? FixedDate { get; set; }
}
