using System.Reflection;
using Bilreg.Api.Controllers.ApotekContext;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.ApotekContext.WorklistFeature;
using Bilreg.Domain.ApotekContext.Shared;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bilreg.Test.ApotekContext.Api;

public class ApotekApiContractTest
{
  private static readonly string[] LegacyQueueOrDepositTokens =
  [
    "DepositStatus", "Open", "Taken", "Assigned", "Delivered"
  ];

  [Fact]
  public void ApotekController_IsVersionedAuthorizedAndUsesExceptionFilter()
  {
    var type = typeof(ApotekController);
    type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Should().NotBeEmpty();
    type.GetCustomAttributes(typeof(ServiceFilterAttribute), true).Cast<ServiceFilterAttribute>()
      .Should().Contain(x => x.ServiceType == typeof(ApotekExceptionFilter));
    type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template
      .Should().Be("api/v1/apotek");
  }

  [Fact]
  public void ApotekController_PublishesAllMutationRoutes()
  {
    var postMethods = typeof(ApotekController)
      .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
      .Select(m => m.GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>().SingleOrDefault()?.Template)
      .Where(t => t is not null)
      .OrderBy(t => t)
      .ToArray();

    postMethods.Should().HaveCount(31);
    postMethods.Should().Contain([
      "resep-kerja/intake-electronic",
      "resep-kerja/intake-physical",
      "jual-bebas/accept",
      "telaah/start",
      "sales-order/establish",
      "queue/map",
      "invoice/establish",
      "dispensing/establish",
      "dispensing/handover",
      "integration/retry"
    ]);
  }

  [Fact]
  public void MutationHandlers_InvokeAuthorizationPolicyPerCommand()
  {
    var applicationAssembly = typeof(IAptAuthorizationPolicy).Assembly;
    var mutationHandlers = applicationAssembly.GetTypes()
      .Where(t => t.IsClass && !t.IsAbstract)
      .Where(t => t.Namespace?.Contains("ApotekContext", StringComparison.Ordinal) == true)
      .Where(t => t.GetInterfaces().Any(IsMutationHandlerInterface))
      .ToList();

    mutationHandlers.Should().NotBeEmpty();
    foreach (var handler in mutationHandlers)
    {
      handler.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
        .Concat(handler.GetFields(BindingFlags.Instance | BindingFlags.Public))
        .Any(f => f.FieldType == typeof(IAptAuthorizationPolicy))
        .Should().BeTrue($"handler {handler.Name} must depend on {nameof(IAptAuthorizationPolicy)}");
    }
  }

  private static bool IsMutationHandlerInterface(Type iface)
  {
    if (!iface.IsGenericType)
      return false;

    var def = iface.GetGenericTypeDefinition();
    if (def == typeof(IRequestHandler<>))
      return iface.GetGenericArguments()[0].Name.EndsWith("Cmd", StringComparison.Ordinal);

    if (def == typeof(IRequestHandler<,>))
      return iface.GetGenericArguments()[0].Name.EndsWith("Cmd", StringComparison.Ordinal);

    return false;
  }

  [Fact]
  public void AuthorizationPolicy_ReferencesBc12ReleaseGate()
  {
    var act = () => new AuthenticatedActorAuthorizationPolicy().AssertCommandAllowed("TestCmd", " ");
    act.Should().Throw<UnauthorizedAccessException>()
      .Which.Message.Should().Contain(ApotekReleaseGates.Bc12CommandRoleMatrix)
      .And.Contain("RELEASE-BLOCKED");
  }

  [Theory]
  [InlineData(typeof(ApotekConcurrencyException), StatusCodes.Status409Conflict, "CONCURRENCY")]
  [InlineData(typeof(ApotekDomainException), StatusCodes.Status400BadRequest, "DOMAIN")]
  [InlineData(typeof(UnauthorizedAccessException), StatusCodes.Status401Unauthorized, "AUTH")]
  public void ApotekExceptionFilter_MapsKnownFailuresToDocumentedShape(
    Type exceptionType, int statusCode, string code)
  {
    var filter = new ApotekExceptionFilter();
    var exception = exceptionType == typeof(ApotekConcurrencyException)
      ? (Exception)new ApotekConcurrencyException("agg-1", 2)
      : exceptionType == typeof(ApotekDomainException)
        ? new ApotekDomainException("domain failure")
        : new UnauthorizedAccessException("not authenticated");

    var context = new ExceptionContext(
      new ActionContext
      {
        HttpContext = new DefaultHttpContext(),
        RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
        ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
      },
      []);

    context.Exception = exception;
    filter.OnException(context);

    context.ExceptionHandled.Should().BeTrue();
    var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
    result.StatusCode.Should().Be(statusCode);
    var payload = result.Value!;
    payload.GetType().GetProperty("status")!.GetValue(payload).Should().Be("fail");
    payload.GetType().GetProperty("code")!.GetValue(payload).Should().Be(code);
  }

  [Fact]
  public void WorklistAndJourneyDtos_DoNotExposeLegacyQueueOrDepositStates()
  {
    var dtoTypes = typeof(TelaahWorklistItem).Assembly.GetTypes()
      .Where(t => t.Namespace == typeof(TelaahWorklistItem).Namespace)
      .Where(t => t.IsClass || t.IsValueType)
      .ToList();

    foreach (var dto in dtoTypes)
    {
      foreach (var member in dto.GetMembers(BindingFlags.Public | BindingFlags.Instance))
      {
        LegacyQueueOrDepositTokens.Should().NotContain(member.Name,
          $"DTO {dto.Name} must not expose legacy queue/deposit member {member.Name}");
      }
    }
  }
}
