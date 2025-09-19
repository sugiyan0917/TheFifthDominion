using UnityEngine;

namespace StoryGen.Domain
{
    public enum Profession { Poet, Scholar, Merchant, Knight, Priest, Spy, Journalist }
    public enum Faction { Neutral, Royal, Guild, Temple, Rebel }

    [CreateAssetMenu(fileName = "CharacterDef", menuName = "StoryGen/Character")]
    public class CharacterDef : ScriptableObject
    {
        [Header("基本情報")]
        public string characterName;
        public Sprite portrait;
        public CountryDef homeland;
        public Profession profession = Profession.Scholar;
        public Faction faction = Faction.Neutral;

        [Header("性格・傾向（0-100）")]
        [Range(0,100)] public int curiosity = 50;   // 噂に近づく傾向
        [Range(0,100)] public int skepticism = 50;  // 懐疑度（高いほど騙されにくい）
        [Range(0,100)] public int influence = 50;   // 影響力（伝播力）
        [Range(0,100)] public int loyalty = 50;     // 陣営忠誠

        [Header("関係・信頼 初期値")]
        [Range(0,100)] public int trustToPlayer = 50;
    }
}
