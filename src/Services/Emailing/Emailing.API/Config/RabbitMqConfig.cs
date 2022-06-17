using System.ComponentModel.DataAnnotations;

namespace Blog.Services.Emailing.API.Config;

public class RabbitMqConfig
{
    public const string Section = "RabbitMq";

    [Required]
    public string Host { get; set; }

    [Required]
    public string VirtualHost { get; set; }

    [Required]
    public string Username { get; set; }

    [Required]
    public string Password { get; set; }

    public const string QueueName = "emailing-api";
}