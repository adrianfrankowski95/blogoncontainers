using Blog.Services.Identity.Domain.AggregatesModel.UserAggregate;
using MediatR;

namespace Blog.Services.Identity.API.Application.Commands;

public record ResetPasswordCommand(NonEmptyString EmailAddress, NonEmptyString NewPassword, NonEmptyString PasswordResetCode) : IRequest<Unit>;