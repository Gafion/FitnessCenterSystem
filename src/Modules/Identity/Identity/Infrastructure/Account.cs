using Microsoft.AspNetCore.Identity;

namespace FitnessCenter.Modules.Identity.Infrastructure;

/// <summary>A login account. Only credentials live here; the person's details belong to Members.</summary>
internal sealed class Account : IdentityUser<Guid>;
