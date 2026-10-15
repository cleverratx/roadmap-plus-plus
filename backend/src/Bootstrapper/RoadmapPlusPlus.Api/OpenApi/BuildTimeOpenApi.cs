using System.Reflection;

namespace RoadmapPlusPlus.Api.OpenApi
{
    /// <summary>
    /// Генератор OpenAPI-документа при сборке (<c>Microsoft.Extensions.ApiDescription.Server</c>) выполняет <c>Program.cs</c>
    /// до старта сервера, включая код после <c>builder.Build()</c>. Базы при сборке нет, поэтому в этом режиме
    /// миграции пропускаем, а логи приложения отключаем, чтобы фоновые службы вроде Data Protection не сыпали ошибками в вывод сборки.
    /// </summary>
    internal static class BuildTimeOpenApi
    {
        public static bool IsGeneratingDocument => Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
    }
}
