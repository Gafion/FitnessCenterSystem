using FitnessCenter.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace FitnessCenter.Modules.Members.Endpoints;

internal static class RequestLanguage
{
    /// <summary>The language the request's <c>Accept-Language</c> asks for; Danish by default (D-065).</summary>
    public static Language Language(this HttpRequest request) =>
        Languages.FromAcceptLanguage(request.Headers.AcceptLanguage);
}
