using Blog.Services.Blogging.API.Application.Models;
using Blog.Services.Blogging.API.Infrastructure.Services;
using Blog.Services.Blogging.Domain.AggregatesModel.PostAggregate;
using MediatR;

namespace Blog.Services.Blogging.API.Application.Commands;

public class RejectPostCommandHandler : IRequestHandler<RejectPostCommand, ICommandResult>
{
    private readonly IPostRepository _postRepository;
    private readonly IIdentityService _identityService;

    public RejectPostCommandHandler(IPostRepository postRepository, IIdentityService identityService)
    {
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
    }
    public async Task<ICommandResult> Handle(RejectPostCommand request, CancellationToken cancellationToken)
    {
        if (!_identityService.TryGetAuthenticatedUser(out User user))
            return CommandResult.IdentityError();

        var post = await _postRepository.FindPostAsync(new PostId(request.PostId)).ConfigureAwait(false);

        if (post is null)
            return CommandResult.NotFoundError(request.PostId);

        try
        {
            post.RejectBy(user);
        }
        catch (Exception ex)
        {
            return CommandResult.DomainError(ex.Message);
        }

        if (!await _postRepository.UnitOfWork.CommitChangesAsync(cancellationToken).ConfigureAwait(false))
            return CommandResult.SavingError();

        return CommandResult.Success();
    }
}