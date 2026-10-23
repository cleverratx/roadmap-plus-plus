using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RoadmapPlusPlus.Modules.Identity.Contracts;

namespace RoadmapPlusPlus.Modules.Identity.Infrastructure
{
    internal sealed class HttpCurrentUser( IHttpContextAccessor accessor ) : ICurrentUser
    {
        public Guid Id => Guid.TryParse( accessor.HttpContext?.User.FindFirstValue( ClaimTypes.NameIdentifier ), out Guid id )
            ? id
            : throw new InvalidOperationException( "В запросе нет вошедшего пользователя: эндпоинт должен требовать входа." );
    }
}
