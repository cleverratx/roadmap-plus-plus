namespace RoadmapPlusPlus.Modules.Planning.Domain.Items
{
    /// <summary>
    /// Вид узла. Всё в Roadmap++ это узлы одного дерева (docs/domain-model.md), вид определяет только, что во что можно вложить.
    /// </summary>
    internal enum ItemKind
    {
        Macro,
        Event,
        Group,
        Task,
    }
}
