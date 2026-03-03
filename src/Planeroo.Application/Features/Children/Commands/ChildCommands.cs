using MediatR;
using Planeroo.Application.DTOs.Children;
using Planeroo.Domain.Common;

namespace Planeroo.Application.Features.Children.Commands;

// ── Create Child ──
public record CreateChildCommand(
    Guid ParentId,
    string FirstName,
    string? LastName,
    DateTime DateOfBirth,
    int GradeLevel,
    string? SchoolName,
    string? Pin,
    string? FavoriteColor,
    string? MascotName
) : IRequest<Result<ChildDetailDto>>;

// ── Update Child ──
public record UpdateChildCommand(
    Guid ChildId,
    Guid ParentId,
    string FirstName,
    string? LastName,
    int GradeLevel,
    string? SchoolName,
    string? Pin,
    string? FavoriteColor,
    string? MascotName
) : IRequest<Result<ChildDetailDto>>;

// ── Delete Child ──
public record DeleteChildCommand(Guid ChildId, Guid ParentId) : IRequest<Result>;
