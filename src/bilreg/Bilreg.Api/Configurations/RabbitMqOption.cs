namespace Bilreg.Api.Configurations;

public sealed class RabbitMqOption
{
    public const string SectionName = "RabbitMqOption";
    public string Server { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
