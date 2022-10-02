namespace Blog.Services.Blogging.API.Application.Queries.TagQueries.Models;

public record AvatarViewModel
{
    public byte[] ImageData { get; init; }
    public string Format { get; init; }
}
