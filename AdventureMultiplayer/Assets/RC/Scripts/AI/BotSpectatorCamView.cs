using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Editor debug only: one split-screen Game view camera that chases a race bot and labels its
    /// viewport with the bot's name and route status. Spawned/arranged by the editor tool
    /// BotSpectatorCams (Ctrl+Alt+B) from the BotSpectatorCam prefab — never placed in a scene.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BotSpectatorCamView : MonoBehaviour
    {
        [SerializeField] private float distance = 7f;   // how far behind the bot
        [SerializeField] private float height   = 3.5f;
        [SerializeField] private float follow   = 6f;   // smoothing (higher = snappier)

        private RaceBotBrain m_bot;
        private Camera       m_cam;
        private GUIStyle     m_style;

        public void SetTarget(RaceBotBrain bot, Rect viewport)
        {
            m_bot = bot;
            if (m_cam == null) m_cam = GetComponent<Camera>();
            m_cam.rect = viewport;
            if (bot != null) Snap();
        }

        private void Awake() => m_cam = GetComponent<Camera>();

        private void Snap()
        {
            transform.position = WantPos();
            transform.LookAt(m_bot.transform.position + Vector3.up);
        }

        private Vector3 WantPos()
        {
            Vector3 fwd = Vector3.ProjectOnPlane(m_bot.transform.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            return m_bot.transform.position - fwd.normalized * distance + Vector3.up * height;
        }

        private void LateUpdate()
        {
            if (m_bot == null) return;
            float k = 1f - Mathf.Exp(-follow * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, WantPos(), k);
            Quaternion look = Quaternion.LookRotation(m_bot.transform.position + Vector3.up - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, k);
        }

        private void OnGUI()
        {
            if (m_bot == null || m_cam == null) return;
            m_style ??= new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            Rect r = m_cam.pixelRect; // bottom-left origin; GUI is top-left
            string label = $"{m_bot.name.Replace("(Clone)", "").Trim()}  —  {m_bot.RouteStatus}";
            GUI.Box(new Rect(r.x + 8f, Screen.height - r.yMax + 8f, Mathf.Min(r.width - 16f, 420f), 26f), label, m_style);
        }
    }
}
