using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using StoryGen.Domain;  // WorldDatabase, CountryDef, CharacterDef, EventDef
using StoryGen.AI;      // ITextGenProvider, DummyTextGen

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
        [SerializeField] private int maxStepsPerCountry = 10; // 各国10ターン固定

        private ITextGenProvider llm;
        private BeliefNetworkSim net;

        private void Awake()
        {
            if (database == null)
            {
                Debug.LogError("[WorldRunner] WorldDatabase未割当");
                enabled = false;
                return;
            }
            llm = new DummyTextGen(); // まずはダミー
        }

        private void Start()
        {
            // ★ 各国を順番に処理（国ごとに10ターン）
            StartCoroutine(RunAllCountriesCoroutine());
        }

        private IEnumerator RunAllCountriesCoroutine()
        {
            if (database.countries == null || database.countries.Count == 0)
            {
                Debug.LogError("CountryがDBにありません");
                yield break;
            }

            for (int i = 0; i < database.countries.Count; i++)
            {
                var country = database.countries[i];
                // 修正1: 必要な引数（country）を渡す
                yield return RunCountryCoroutine(country);
            }

            Debug.Log("==== 全国家の処理が完了しました ====");
        }

        // 国ごとに 10 ターン分の「噂→記事」を生成し、国別の履歴もまとめる
        private IEnumerator RunCountryCoroutine(CountryDef country)
        {
            // この国の出来事だけを“ちょうど10件”抽出
            var events = database.events
                .Where(e => e != null && e.targetCountry == country) // 国別イベントのみ
                .Take(maxStepsPerCountry)
                .ToList();

            if (events.Count < maxStepsPerCountry)
                Debug.LogWarning($"[WorldRunner] {country.countryName} の出来事が {events.Count} 件（推奨 {maxStepsPerCountry} 件）");

            if (events.Count == 0)
            {
                Debug.LogWarning($"[WorldRunner] {country.countryName} にイベントがありません");
                yield break;
            }

            // この国の主要人物数（今後の拡張用）
            int mainCount = database.characters.Count(c => c != null && c.homeland == country);

            // 国ごとにネットワークを作り直す（0:記者/1..mainCount:主要人物/最後:歴史家）
            net = new BeliefNetworkSim(population, initialThreshold, initialWeights, hubPart, mainCount);

            // 修正2: 国別フィード（スコープ内で宣言）
            var countryFeed = new StringBuilder();
            countryFeed.AppendLine($"=== {country.countryName} ===");

            for (int i = 0; i < events.Count; i++)
            {
                var eDef = events[i];
                string baseText  = !string.IsNullOrEmpty(eDef.summaryTemplate) ? eDef.summaryTemplate : eDef.eventName;
                string eventText = $"【国:{country.countryName}】{baseText}";

                // 1) 噂（市民/主要人物）
                yield return GenerateRumorsStep(eventText);

                // 2) 記事（記者が噂を集約）
                // 修正3: news をループ内で宣言し、コールバックでセット
                string news = null;
                yield return GenerateNewsStep(eventText, n => news = n);

                // 3) ネットワークを少し成長
                net.UpdateEdgesByActiveRumors(4);

                // 4) ログ追記
                if (!string.IsNullOrEmpty(news))
                    countryFeed.AppendLine(news);

                yield return new WaitForSeconds(stepIntervalSec);
            }

            // 5) 国別の履歴（まとめ）を生成
            string history = null;
            yield return GenerateHistoryStep(countryFeed.ToString(), h => history = h);

            Debug.Log(countryFeed.ToString());
            Debug.Log($"--- {country.countryName} Consolidated History ---\n{history}");
        }

        // ===== ここから下：各ステップ =====

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

        // 記事生成（噂を集約して1本の記事に）
        private IEnumerator GenerateNewsStep(string eventText, System.Action<string> onNewsReady)
        {
            var collected = string.Join(" / ",
                net.nodes.Values.Where(n => !string.IsNullOrEmpty(n.currentRumor)).Select(n => n.currentRumor));

            string news = null;
            var task = llm.GenerateNewsAsync(eventText, collected, asJournalist: true)
                .ContinueWith(t => news = t.Result);

            while (!task.IsCompleted) yield return null;

            onNewsReady?.Invoke(news);
        }

        // 履歴生成（国別テキストを渡してまとめる）
        private IEnumerator GenerateHistoryStep(string textToConsolidate, System.Action<string> onHistoryReady)
        {
            string consolidated = null;
            var task = llm.GenerateHistoryAsync(textToConsolidate)
                .ContinueWith(t => consolidated = t.Result);

            while (!task.IsCompleted) yield return null;

            onHistoryReady?.Invoke(consolidated);
        }
    }
}
