using System.Security.Claims;
using FitnessCenter.Modules.Members.Application;
using FitnessCenter.Modules.Members.Contracts;
using FitnessCenter.Modules.Members.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace FitnessCenter.Modules.Members.Endpoints;

internal static class PaymentEndpoints
{
    /// <summary>Turns the simulators on (D-041). Only Development sets it; everywhere else it is off.</summary>
    public const string SimulationEnabledSetting = "Simulation:Enabled";

    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/members").WithTags("Members")
            .MapGet("/me/payments", GetMyPaymentsAsync).WithName("GetMyPayments").RequireAuthorization()
            .Produces(StatusCodes.Status401Unauthorized);

        // Mapped always, so the OpenAPI document and the generated client know it. D-041 checks
        // the setting before the login: 404 when the simulation is off, even without a login,
        // then 401 without one. RequireAuthorization would answer 401 first, so the filter does both.
        endpoints.MapGroup("/api/simulation").WithTags("Simulation")
            .MapPost("/payments", SimulatePaymentAsync).WithName("SimulatePayment")
            .AddEndpointFilter(async (context, next) =>
            {
                var http = context.HttpContext;
                if (!http.RequestServices.GetRequiredService<IConfiguration>().GetValue<bool>(SimulationEnabledSetting))
                {
                    return TypedResults.NotFound();
                }

                return http.User.Identity?.IsAuthenticated == true ? await next(context) : TypedResults.Unauthorized();
            })
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    /// <param name="PeriodFrom">First day of the month the payment covers (Copenhagen).</param>
    /// <param name="PeriodTo">Last day it covers, inclusive.</param>
    internal sealed record Payment(
        long AmountOre, DateTimeOffset PaidAt, string CardLast4, DateOnly PeriodFrom, DateOnly PeriodTo);

    /// <param name="Status">"AwaitingFirstPayment" until the first payment, then "Active".</param>
    /// <param name="PaidUntil">Last day covered by a payment; null before the first one (D-020).</param>
    /// <param name="Payments">Newest period first.</param>
    internal sealed record MyPayments(string Status, DateOnly? PaidUntil, long MonthlyPriceOre, Payment[] Payments);

    /// <param name="PeriodFrom">
    /// Where the simulated month starts. Leave it out for the next month: the day after
    /// paid-until, or today before the first payment. A past date lets a missing payment be tried.
    /// </param>
    internal sealed record SimulatedPaymentRequest(DateOnly? PeriodFrom);

    private static async Task<Results<Ok<MyPayments>, NotFound>> GetMyPaymentsAsync(
        ClaimsPrincipal user, PaymentsHandler handler, CancellationToken cancellationToken)
    {
        var history = await handler.GetHistoryAsync(AccountIdOf(user), cancellationToken);
        return history is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new MyPayments(
                history.Status.ToString(),
                history.PaidUntil?.ToDateOnly(),
                history.MonthlyPrice.Ore,
                history.Payments.Select(ToResponse).ToArray()));
    }

    private static async Task<Results<Created<Payment>, NotFound, ProblemHttpResult>> SimulatePaymentAsync(
        SimulatedPaymentRequest request, ClaimsPrincipal user, PaymentsHandler handler, CancellationToken cancellationToken)
    {
        var periodFrom = request.PeriodFrom is { } from ? LocalDate.FromDateOnly(from) : (LocalDate?)null;
        var outcome = await handler.SimulateAsync(AccountIdOf(user), periodFrom, cancellationToken);
        return outcome switch
        {
            null => TypedResults.NotFound(),
            { Payment: { } payment } => TypedResults.Created((string?)null, ToResponse(payment)),
            { Result: PaymentConfirmationResult.PeriodNotContiguous } => TypedResults.Problem(
                title: "A payment must start the day after the paid-until date.",
                statusCode: StatusCodes.Status409Conflict,
                type: "period-not-contiguous"),
            _ => throw new InvalidOperationException($"Unexpected result {outcome.Result}."),
        };
    }

    private static Payment ToResponse(PaymentRecord payment) => new(
        payment.Amount.Ore,
        payment.PaidAt.ToDateTimeOffset(),
        payment.CardLast4,
        payment.PeriodFrom.ToDateOnly(),
        payment.PeriodTo.ToDateOnly());

    /// <summary>The logged-in account (the Identity cookie's user id). The endpoints require a login.</summary>
    private static Guid AccountIdOf(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
