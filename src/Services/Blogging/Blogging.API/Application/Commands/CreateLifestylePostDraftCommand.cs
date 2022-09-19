using Blog.Services.Blogging.API.Application.Models;
using MediatR;

namespace Blog.Services.Blogging.API.Application.Commands;

public record CreateLifestylePostDraftCommand(
    string HeaderImgUrl,
    IList<LifestylePostTranslationDTO> Translations) : IRequest<Unit>;

