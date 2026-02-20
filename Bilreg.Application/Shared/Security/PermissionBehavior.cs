//using MediatR;

//namespace Bilreg.Application.Shared.Security;

//public class PermissionBehavior<TRequest, TResponse>
//    : IPipelineBehavior<TRequest, TResponse>
//{
//    private readonly IPermissionChecker _checker;
//    private readonly ICurrentUser _currentUser;

//    public PermissionBehavior(
//        IPermissionChecker checker,
//        ICurrentUser currentUser)
//    {
//        _checker = checker;
//        _currentUser = currentUser;
//    }

//    public async Task<TResponse> Handle(
//        TRequest request,
//        RequestHandlerDelegate<TResponse> next,
//        CancellationToken cancellationToken)
//    {
//        if (request is IRequirePermission permissionRequest)
//        {
//            var hasPermission = await _checker
//                .HasPermissionAsync(_currentUser.UserId, permissionRequest.PermissionCode);

//            if (!hasPermission)
//                throw new UnauthorizedAccessException("No permission.");
//        }

//        return await next();
//    }
//}
