using UnityEngine;

namespace EchoIslands
{
    public class GameHUD : MonoBehaviour
    {
        public MainMenu menu;
        public ToolController tools;

        int level = 1;
        string notification;
        float noticeUntil;
        float introUntil;
        bool introInitialized;

        GUIStyle tiny, hint;

        public void SetLevel(int value)
        {
            level = Mathf.Clamp(value, 1, 5);
            {
                introUntil = Time.unscaledTime + 7.5f;
                introInitialized = true;
            }
        }

        public void Notify(string message, float seconds = 2.4f)
        {
            notification = message;
            noticeUntil = Time.unscaledTime + seconds;
        }

        public void ShowWin()
        {
            if (menu != null) menu.ShowVictory();
        }

        void Styles()
        {
            if (tiny != null) return;
            tiny = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            tiny.normal.textColor = new Color(.76f, .83f, .88f);
            hint = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true, alignment = TextAnchor.MiddleLeft };
            hint.normal.textColor = new Color(.93f, .97f, 1f);
        }

        void Panel(Rect rect, Color tint)
        {
            var old = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        string LookHint()
        {
            var pad = GravityPad.Nearby;
            if (pad != null && WorldState.Instance != null)
                return WorldState.Instance.Target == pad.requiredWorld ? "G — повернуть гравитацию" :
                    pad.requiredWorld == WorldKind.Dark ? "ЛКМ — нужен тёмный мир" : "ЛКМ — нужен светлый мир";
            var cam = Camera.main;
            if (cam == null) return "";
            if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit,
                tools != null ? tools.useDistance : 3.6f, ~0, QueryTriggerInteraction.Collide)) return "";

            var pickup = hit.collider.GetComponentInParent<ToolPickup>();
            if (pickup != null) return pickup.hint;

            var orb = hit.collider.GetComponentInParent<Orb>();
            if (orb != null && !orb.carried && (WorldState.Instance == null || WorldState.Instance.IsReal(orb.home, orb.transform.position)))
                return "E — забрать сферу";

            var portal = hit.collider.GetComponentInParent<PortalFrame>();
            if (portal != null)
                return portal.IsActivated ? "E — войти в портал" : "E — отдать сферу порталу";


            return "";
        }

        string IntroText()
        {
            if (level == 5)
                return "WASD — движение\nПРОБЕЛ — прыжок\nQ — запустить часы\nE — взаимодействие\nESC — меню";
            if (level == 3)
                return "WASD — движение\nПРОБЕЛ — прыжок\nЛКМ — смена мира\nG — смена гравитации\nE — взаимодействие\nESC — меню";
            return "WASD — движение\nПРОБЕЛ — прыжок\nЛКМ — смена мира\nE — взаимодействие\nG — действие у гравитационных рун\nESC — меню";
        }

        void OnGUI()
        {
            if (menu != null && menu.IsOpen) return;
            Styles();
            float sw = Screen.width;
            float sh = Screen.height;
            if (!introInitialized)
            {
                introUntil = Time.unscaledTime + 7.5f;
                introInitialized = true;
            }
            if (Time.unscaledTime < introUntil)
            {
                Rect intro = new Rect(14f, 14f, 214f, 108f);
                Panel(intro, new Color(.04f, .07f, .10f, .62f));
                GUI.Label(new Rect(intro.x + 12f, intro.y + 10f, intro.width - 20f, intro.height - 16f), IntroText(), hint);
            }
            GUI.Label(new Rect(14f, sh - 28f, 220f, 16f), "E — действие   ·   G — гравитация", tiny);
            if (level == 5 && LevelFlow.Instance != null)
            {
                int left = Mathf.CeilToInt(LevelFlow.Instance.Level5Remaining);
                string phase = tools != null && tools.ClockRemaining > 0f
                    ? "   ·   Часы " + tools.ClockRemaining.ToString("0.0") + " с" : "";
                GUI.Label(new Rect(sw-180f,12f,170f,20f),$"{left/60}:{left%60:00}{phase}",tiny);
            }
            string focusHint = LookHint();
            if (!string.IsNullOrEmpty(focusHint))
                GUI.Label(new Rect(sw - 210f, sh - 28f, 196f, 16f), focusHint, tiny);
            if (Time.unscaledTime < noticeUntil && !string.IsNullOrEmpty(notification))
            {
                Rect note = new Rect(sw * .5f - 110f, 18f, 220f, 22f);
                Panel(note, new Color(.03f, .08f, .11f, .45f));
                GUI.Label(new Rect(note.x + 10f, note.y + 3f, note.width - 20f, 16f), notification, tiny);
            }
        }
    }
}
