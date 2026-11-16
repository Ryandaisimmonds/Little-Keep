using LittleKeep.Rules;
using UnityEngine;

namespace LittleKeep.Game
{
    /// A story event Stop. Create via Assets > Create > Little Keep > Event.
    [CreateAssetMenu(menuName = "Little Keep/Event", fileName = "Event")]
    public sealed class EventAsset : ScriptableObject
    {
        public EventDef def = new EventDef();
    }
}
