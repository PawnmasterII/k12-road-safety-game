using UnityEngine;

namespace SafeStreets
{
    /// <summary>Quest data: help N friends cross the road (the win condition).</summary>
    [CreateAssetMenu(fileName = "NewQuest", menuName = "SafeStreets/Quest")]
    public class QuestData : ScriptableObject
    {
        public string questName;
        public int requiredCrossings = 3;
    }

    /// <summary>
    /// Tracks crossing progress and updates the HUD. Win condition for the prototype:
    /// help the required number of NPCs cross. Fires OnAllCrossed when done.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        public int helped { get; private set; }
        public int required = 3;
        public bool Complete => helped >= required;

        public static event System.Action<int, int> OnProgress;   // (helped, required)
        public static event System.Action OnAllCrossed;

        private TextMesh _hud;
        private bool _fired;

        private void Awake()
        {
            Instance = this;
        }

        public void RegisterHud(TextMesh hud, QuestData data)
        {
            if (Instance == null) Instance = this;   // edit-mode builds: Awake() hasn't run
            _hud = hud;
            if (data != null) required = data.requiredCrossings;
            Refresh();
        }

        public void RecordCrossing(string npcName)
        {
            if (Complete) return;
            helped++;
            Refresh();
            if (Complete && !_fired)
            {
                _fired = true;
                OnAllCrossed?.Invoke();
            }
        }

        private void Refresh()
        {
            if (_hud != null) _hud.text = $"Helped: {helped} / {required}";
            OnProgress?.Invoke(helped, required);
        }
    }
}
