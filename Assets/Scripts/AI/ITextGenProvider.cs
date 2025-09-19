using System.Threading.Tasks;

namespace StoryGen.AI
{
    // LLM呼び出しの抽象インターフェイス
    public interface ITextGenProvider
    {
        Task<string> GenerateRumorAsync(string eventText);
        Task<string> GenerateNewsAsync(string eventText, string collectedRumors, bool asJournalist);
        Task<string> GenerateHistoryAsync(string allNews);
    }
}
