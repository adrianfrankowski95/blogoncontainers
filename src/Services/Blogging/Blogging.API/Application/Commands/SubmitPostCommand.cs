using Blog.Services.Blogging.API.Application.Models;
using MediatR;

namespace Blog.Services.Blogging.API.Application.Commands;

public record SubmitPostCommand(Guid PostId) : IRequest<ICommandResult>;