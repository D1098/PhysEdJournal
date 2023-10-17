using PhysEdJournal.Core.Entities.Types;
using PhysEdJournal.Core.PResult;

namespace PhysEdJournal.Api.Endpoints.Endpoint;

public abstract class CommandEndpoint<TRequest, TResponse> : BaseEndpoint<TRequest, TResponse>
    where TRequest : notnull
{
    protected sealed override EndpointType EndpointType { get; init; } = EndpointType.Command;

    public required PermissionValidator PermissionValidator { get; init; }

    private TeacherPermissions _teacherPermissions = TeacherPermissions.SuperUser;

    protected delegate Task<PResult<bool>> TeacherPermissionsValidator(
        TRequest req,
        string teacherGuid
    );
    private TeacherPermissionsValidator? _teacherPermissionsValidator;

    /// <summary>
    /// Устанавливает необходимые разрешения для преподавателя.
    /// </summary>
    /// <param name="permissions"><see cref="TeacherPermissions"/>.</param>
    protected void SetTeacherPermissions(TeacherPermissions permissions)
    {
        _teacherPermissions = permissions;
    }

    /// <summary>
    /// Устанавливает валидатор разрешений для преподавателя.
    /// </summary>
    /// <param name="validator"><see cref="TeacherPermissionsValidator"/>.</param>
    protected void SetTeacherPermissions(TeacherPermissionsValidator validator)
    {
        _teacherPermissionsValidator = validator;
    }

    protected override async Task<ProblemDetailsResponse?> BeforeCommandExecuteAsync(TRequest req)
    {
        var teacherGuidClaim = User.FindFirst("IndividualGuid");

        if (teacherGuidClaim is null)
        {
            return new ProblemDetailsResponse
            {
                Type = "empty-claim-guid",
                Title = "Empty claim guid",
                Detail = "User token does not have claim with user guid",
                StatusCode = 401,
            };
        }

        PResult<bool> validationResult;

        if (_teacherPermissionsValidator is not null)
        {
            validationResult = await _teacherPermissionsValidator(req, teacherGuidClaim.Value);
        }
        else
        {
            validationResult = await PermissionValidator.ValidateTeacherPermissions(
                teacherGuidClaim.Value,
                _teacherPermissions
            );
        }

        if (validationResult.IsError)
        {
            Logger.LogWarning(
                "Teacher with guid: {guid} was trying to access secure resource with less permissions that is required",
                teacherGuidClaim.Value
            );
            return new ProblemDetailsResponse
            {
                Type = "not-enough-permissions",
                Title = "Not enough permissions",
                Detail = "You does not have enough permissions to access this resource",
                StatusCode = 403,
            };
        }

        return null;
    }

    public override void Configure()
    {
        base.Configure();
        Roles("staff");
    }
}
