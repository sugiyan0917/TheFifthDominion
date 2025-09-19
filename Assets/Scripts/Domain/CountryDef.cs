using UnityEngine;

namespace StoryGen.Domain
{
    [CreateAssetMenu(fileName = "CountryDef", menuName = "StoryGen/Country")]
    public class CountryDef : ScriptableObject
    {
        [Header("基本情報")]
        public string countryName;
        [TextArea] public string description;

        [Header("世界観パラメータ（0-100）")]
        [Range(0,100)] public int technology = 50;   // 工業/技術
        [Range(0,100)] public int magichalogy = 50;  // 魔法（資料の用語に合わせています）
        [Range(0,100)] public int culture = 50;      // 文化
        [Range(0,100)] public int stability = 50;    // 安定度（治安）

        [Header("初期フラグ")]
        public bool isPlayerHomeland;
    }
}
