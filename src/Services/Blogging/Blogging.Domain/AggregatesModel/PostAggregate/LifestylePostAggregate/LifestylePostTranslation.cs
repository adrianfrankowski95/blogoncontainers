using Blog.Services.Blogging.Domain.AggregatesModel.Shared;
using Blog.Services.Blogging.Domain.AggregatesModel.TagAggregate;

namespace Blog.Services.Blogging.Domain.AggregatesModel.PostAggregate.LifestylePostAggregate;

public class LifestylePostTranslation : PostTranslationBase
{
    public LifestylePostTranslation(
        Language language,
        string title,
        string content,
        string description,
        IEnumerable<Tag> tags)
        : base(language, title, content, description, tags) { }

    protected override IEnumerable<object?> GetEqualityCheckAttributes()
    {
        yield return base.GetEqualityCheckAttributes();
    }

    public override void Validate()
    {
        base.Validate();
    }
}
