using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using StoryGen.Domain;  // WorldDatabase, CountryDef など
using StoryGen.AI;      // ITextGenProvider, DummyTextGen
using StoryGen.Sim;     // BeliefNetworkSim など（別ファイルで定義している場合）

namespace StoryGen.Sim
{
    public class WorldRunner : MonoBehaviour
    {
        [Header("DBアセット割当")]
        [SerializeField] private WorldDatabase database;

        [Header("ネットワーク構成")]
        [SerializeField, Range(4,128)] private int population = 12;
        [SerializeField, Range(0f,1f)] private float hubPart = 0.7f;
        [SerializeField] private float initialThreshold = 1.0f;
        [SerializeField] private Vector2 initialWeights = Vector2.one;

        [Header("タイミング")]
        [SerializeField] private float stepIntervalSec = 0.5f;
        [SerializeField] private int maxStepsPerCountry = 5;

        // LLM プロバイダ（ダミーで初期化、後で実APIに差し替え可能）
        private ITextGenProvider llm;

        private BeliefNetworkSim net;
        private int step = 0;
        private StringBuilder newsFeed = new();

        private void Awake()
        {
            if (database == null)
            {
                Debug.LogError("[WorldRunner] WorldDatabase未割当");
                enabled = false; 
                return;
            }
            llm = new DummyTextGen(); // ★まずはダミーで回す
        }

        private void Start()
        {
            var firstCountry = database.countries.FirstOrDefault();
            if (firstCountry == null)
            {
                Debug.LogError("CountryがDBにありません");
                return;
            }

            int mainCount = database.characters.Count(c => c.homeland == firstCountry);
            net = new BeliefNetworkSim(population, initialThreshold, initialWeights, hubPart, mainCount);

            StartCoroutine(RunCountryCoroutine(firstCountry));
        }

        private IEnumerator RunCountryCoroutine(CountryDef country)
        {
            var events = database.events.Where(e => e.targetCountry == country || e.isGlobal).ToList();
            if (events.Count == 0)
            {
                Debug.LogWarning("イベントがありません");
                yield break;
            }

            for (int i = 0; i < Mathf.Min(maxStepsPerCountry, events.Count); i++)
            {
                var eDef = events[i];
                string eventText = !string.IsNullOrEmpty(eDef.summaryTemplate) ? eDef.summaryTemplate : eDef.eventName;

                // 1) 噂生成
                yield return GenerateRumorsStep(eventText);
                // 2) ニュース化
                yield return GenerateNewsStep(eventText);
                // 3) ネットワーク更新
                net.UpdateEdgesByActiveRumors(4);

                step++;
                yield return new WaitForSeconds(stepIntervalSec);
            }

            // 4) 歴史統合
            yield return GenerateHistoryStep();
            Debug.Log("==== News Feed ====\n" + newsFeed.ToString());
        }

        private IEnumerator GenerateRumorsStep(string eventText)
        {
            var tasks = net.nodes.Values
                .Where(n => n.role == NodeRole.Citizen || n.role == NodeRole.MainCharacter)
                .Select(async n =>
                {
                    if (Random.value < 0.85f)
                    {
                        var text = await llm.GenerateRumorAsync(eventText);
                        n.currentRumor = text;
                        n.score += 0.2f;
                    }
                    else
                    {
                        n.currentRumor = null;
                        n.score -= 0.1f;
                    }
                    n.UpdateThreshold();
                }).ToArray();

            while (tasks.Any(t => !t.IsCompleted)) yield return null;
        }

        private IEnumerator GenerateNewsStep(string eventText)
        {
            var collected = string.Join(" / ",
                net.nodes.Values.Where(n => !string.IsNullOrEmpty(n.currentRumor)).Select(n => n.currentRumor));

            string news = null;
            var task = llm.GenerateNewsAsync(eventText, collected, asJournalist: true)
                .ContinueWith(t => news = t.Result);

            while (!task.IsCompleted) yield return null;

            newsFeed.AppendLine(news);
        }

        private IEnumerator GenerateHistoryStep()
        {
            string consolidated = null;
            var task = llm.GenerateHistoryAsync(newsFeed.ToString())
                .ContinueWith(t => consolidated = t.Result);

            while (!task.IsCompleted) yield return null;

            newsFeed.AppendLine("\n--- Consolidated History ---\n" + consolidated);
        }
    }
}
