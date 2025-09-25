using System.Threading.Tasks;

namespace StoryGen.AI
{
    // とりあえず回すためのダミー生成器
    public class DummyTextGen : ITextGenProvider
    {
        public Task<string> GenerateRumorAsync(string eventText)
            => Task.FromResult($"[噂] {eventText} に関する囁き。\n");

        public Task<string> GenerateNewsAsync(string eventText, string collectedRumors, bool asJournalist)
        {
            var role = asJournalist ? "記者" : "歴史家";
            return Task.FromResult($"[{role}記事] 出来事: {eventText}\n要約: {collectedRumors}");
        }

        public Task<string> GenerateHistoryAsync(string allNews)
            => Task.FromResult($"[歴史] 記事群を編纂：\n{allNews}");
    }
}
