namespace Api.Features.Admissions;

using System.Security.Claims;
using Api.Common.Authorization;
using Api.Common.Extensions;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.Admissions.CreateAdmission;
using Application.Features.Admissions.GetAdmissionById;
using Application.Features.Admissions.GetAllAdmissions;
using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

public static class AdmissionEndpoints
{
    private const string GetByIdRouteName = "GetAdmissionById";

    public static void MapAdmissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admissions")
            .WithTags("Admissions")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllAdmissions")
            .WithSummary("Get all admissions")
            .RequireAuthorization(Policies.CanManageAdmissions);

        group.MapPost("/", Create)
            .WithName("CreateAdmission")
            .WithSummary("Create a new admission with courses")
            .RequireAuthorization(Policies.CanManageAdmissions);

        group.MapGet("/{id:guid}", GetById)
            .WithName(GetByIdRouteName)
            .WithSummary("Get admission by ID with courses")
            .RequireAuthorization();
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllAdmissionsQuery, Result<GetAllAdmissionsResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetAllAdmissionsQuery();
        var result = await handler.HandleAsync(query, cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateAdmissionCommand command,
        ICommandHandler<CreateAdmissionCommand, Result<CreateAdmissionResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(
                result.Value,
                GetByIdRouteName,
                new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetAdmissionByIdQuery, Result<GetAdmissionByIdResponse>> handler,
        IAuthorizationService authorizationService,
        IAppDbContext dbContext,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var admission = await dbContext.Admissions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (admission is null)
        {
            return TypedResults.NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(
            user,
            admission,
            new AdmissionOwnerRequirement());

        if (!authResult.Succeeded)
        {
            return TypedResults.Forbid();
        }

        var result = await handler.HandleAsync(
            new GetAdmissionByIdQuery(id),
            cancellationToken);

        return result.Match(
            onSuccess: value => TypedResults.Ok(value),
            onFailure: _ => result.ToProblemDetails());
    }
}
