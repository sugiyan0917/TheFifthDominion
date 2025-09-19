using System.Collections.Generic;
using UnityEngine;

namespace StoryGen.Domain
{
    [CreateAssetMenu(fileName = "WorldDatabase", menuName = "StoryGen/World Database")]
    public class WorldDatabase : ScriptableObject
    {
        public List<CountryDef> countries = new();
        public List<CharacterDef> characters = new();
        public List<EventDef> events = new();
    }
}
