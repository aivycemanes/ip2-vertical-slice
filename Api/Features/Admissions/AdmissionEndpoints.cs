namespace Api.Features.Admissions;

using Application.Features.Admissions.GetAllAdmissions;
using Api.Common.Extensions;
using Application.Abstractions.Messaging;
using Application.Features.Admissions.CreateAdmission;
using Application.Features.Admissions.GetAdmissionById;
using Domain.Common;
using Api.Common.Authorization;

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
            .WithName("GetAdmissionById")
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
            ? TypedResults.CreatedAtRoute(result.Value, "GetAdmissionById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetAdmissionByIdQuery, Result<GetAdmissionByIdResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetAdmissionByIdQuery(id);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }
}
