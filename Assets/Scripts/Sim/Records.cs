using UnityEngine;
using System;
//シミュレーション用データ構造
namespace StoryGen.Sim
{
    [Serializable]
    public class RumorRecord
    {
        public int nodeId;
        public string text;
        public float time;//0-1伝播強度
        public float credibility; //０−１信頼度
    }
    [Serializable]
    public class NewsRecord
    {
        public string eventText;
        public string aggregatedRumors;//噂まとめ
        public string article;//記事
    }
    [Serializable]
    public class HistoryRecord
    {
        public string consolidated;//歴史
    }
 }

