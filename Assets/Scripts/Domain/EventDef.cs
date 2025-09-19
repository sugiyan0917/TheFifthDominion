using UnityEngine;

namespace StoryGen.Domain
{
    public enum EventTag { Politics, War, Economy, Culture, Magic, Crime, Disaster, Festival }

    [CreateAssetMenu(fileName = "EventDef", menuName = "StoryGen/Event")]
    public class EventDef : ScriptableObject
    {
        [Header("出来事テンプレ")]
        public string eventName;
        [TextArea] public string summaryTemplate; // ここに1〜2文の出来事説明を入れてOK
        public EventTag[] tags;

        [Header("影響範囲")]
        public CountryDef targetCountry;  // nullなら全体（isGlobalで出し分け）
        public bool isGlobal;

        [Header("重み・頻度")]
        [Range(0,100)] public int weight = 50;   // 発生確率の重み（簡易）
        [Range(0,100)] public int severity = 50; // 事象の大きさ（噂強度の初期値などに利用）
    }
}
