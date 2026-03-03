using MediatR;
using Planeroo.Application.DTOs.Children;
using Planeroo.Domain.Common;

namespace Planeroo.Application.Features.Children.Queries;

public record GetChildByIdQuery(Guid ChildId, Guid ParentId) : IRequest<Result<ChildDetailDto>>;
public record GetChildrenByParentQuery(Guid ParentId) : IRequest<Result<List<ChildDetailDto>>>;
