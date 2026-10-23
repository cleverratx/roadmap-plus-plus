namespace RoadmapPlusPlus.Modules.Identity.Contracts
{
    /// <summary>
    /// Пользователь, от имени которого идёт текущий запрос. Годится только в эндпоинтах, которые требуют входа,
    /// а это все эндпоинты под <c>/api</c>, кроме явно открытых.
    /// </summary>
    public interface ICurrentUser
    {
        Guid Id { get; }
    }
}
