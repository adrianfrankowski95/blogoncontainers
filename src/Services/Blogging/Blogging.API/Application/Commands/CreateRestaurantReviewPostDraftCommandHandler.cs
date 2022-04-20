using Blog.Services.Blogging.API.Application.Commands.Models;
using Blog.Services.Blogging.API.Application.Models;
using Blog.Services.Blogging.API.Infrastructure.Services;
using Blog.Services.Blogging.Domain.AggregatesModel.PostAggregate;
using Blog.Services.Blogging.Domain.AggregatesModel.PostAggregate.ReviewPostAggregate;
using Blog.Services.Blogging.Domain.AggregatesModel.Shared;
using Blog.Services.Blogging.Domain.AggregatesModel.TagAggregate;
using MediatR;

namespace Blog.Services.Blogging.API.Application.Commands;

public class CreateRestaurantReviewPostDraftCommandHandler : IRequestHandler<CreateRestaurantReviewPostDraftCommand, ICommandResult>
{
    private readonly IPostRepository _postRepository;
    private readonly IIdentityService _identityService;
    private readonly ITagRepository _tagRepository;

    public CreateRestaurantReviewPostDraftCommandHandler(
        IPostRepository postRepository,
        ITagRepository tagRepository,
        IIdentityService identityService)
    {
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
    }
    public async Task<ICommandResult> Handle(CreateRestaurantReviewPostDraftCommand request, CancellationToken cancellationToken)
    {
        if (!_identityService.TryGetAuthenticatedUser(out User user))
            return CommandResult.IdentityError();

        var tagIds = request.Translations.SelectMany(x => x.TagIds).Select(x => new TagId(x));
        IEnumerable<Tag> tags = tagIds is null ?
            Enumerable.Empty<Tag>() :
            await _tagRepository.FindTagsByIdsAsync(tagIds).ConfigureAwait(false);

        RestaurantReviewPost post;
        try
        {
            var translations = MapTranslations(request.Translations, tags);

            post = new RestaurantReviewPost(
                user,
                translations,
                new Restaurant(request.RestaurantName, request.RestaurantWebsiteUrl),
                new Rating(request.Rating),
                request.HeaderImgUrl);
        }
        catch (Exception ex)
        {
            return CommandResult.DomainError(ex.Message);
        }

        _postRepository.AddPost(post);

        if (!await _postRepository.UnitOfWork.CommitChangesAsync(cancellationToken).ConfigureAwait(false))
            return CommandResult.SavingError();

        return CommandResult.Success();
    }

    private static IEnumerable<RestaurantReviewPostTranslation> MapTranslations(
        IEnumerable<RestaurantReviewPostTranslationDTO> requestTranslations,
        IEnumerable<Tag> tags)
    {
        List<RestaurantReviewPostTranslation> translations = new();


        foreach (var requestTranslation in requestTranslations ?? Enumerable.Empty<RestaurantReviewPostTranslationDTO>())
        {
            translations.Add(
                new RestaurantReviewPostTranslation(
                    Language.FromName(requestTranslation.Language),
                    requestTranslation.Title,
                    requestTranslation.Content,
                    requestTranslation.Description,
                    tags.Where(x => requestTranslation.TagIds.Contains(x.Id.Value)),
                    new Address(
                        requestTranslation.RestaurantAddressCountry,
                        requestTranslation.RestaurantAddressZipCode,
                        requestTranslation.RestaurantAddressCity,
                        requestTranslation.RestaurantAddressStreet),
                    requestTranslation.RestaurantCuisines));
        }

        return translations;
    }
}