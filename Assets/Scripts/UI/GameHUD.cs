using UnityEngine;

namespace EchoIslands
{
    public class GameHUD : MonoBehaviour
    {
        public MainMenu menu;
        public ToolController tools;

        int level = 1;
        string notice, focus, timerText, clockText;
        float noticeUntil, nextFocus;
        int shownSeconds = -1, shownPulse = int.MinValue;
        GUIStyle small, centered, timer;

        static readonly Color Warm = new(.99f,.76f,.46f);
        static readonly Color Cold = new(.39f,.95f,.86f);
        static readonly Color Ink = new(.022f,.035f,.047f,.90f);

        public void SetLevel(int value)
        {
            level = Mathf.Clamp(value,1,5);
            focus = notice = null;
            noticeUntil = nextFocus = 0f;
            if (level == 2) Notify("Удерживайте пробел для дальнего прыжка.", 3.3f);
        }

        public void Notify(string message, float seconds = 2.4f)
        {
            notice = message;
            noticeUntil = Time.unscaledTime + seconds;
        }

        public void ShowWin()
        {
            if (menu != null) menu.ShowVictory();
        }

        void Styles()
        {
            if (centered != null) return;
            small = Style(13,FontStyle.Normal,TextAnchor.MiddleCenter);
            centered = Style(15,FontStyle.Bold,TextAnchor.MiddleCenter);
            timer = Style(26,FontStyle.Bold,TextAnchor.MiddleCenter);
        }

        static GUIStyle Style(int size, FontStyle weight, TextAnchor anchor)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = weight,
                alignment = anchor,
                wordWrap = true
            };
            style.normal.textColor = new Color(.93f,.94f,.91f);
            return style;
        }

        static void Fill(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void Panel(Rect rect, Color accent)
        {
            Fill(rect,Ink);
            Fill(new Rect(rect.x,rect.y,rect.width,2f),accent);
        }

        string LookHint()
        {
            var pad = GravityPad.Nearby;
            if (pad != null && !pad.Ready)
            {
                if (level == 5) return tools != null && tools.clock ? "Q — запитать гравитационную печать" : "Для печати нужны часы";
                return pad.requiredWorld == WorldKind.Dark ? "ЛКМ — перейти в тёмный мир" : "ЛКМ — перейти в светлый мир";
            }

            if (tools != null && tools.Aim(out var hit))
            {
                var pickup = hit.collider.GetComponentInParent<ToolPickup>();
                if (pickup != null) return pickup.hint;
                var pedestal = hit.collider.GetComponentInParent<StaffPedestal>();
                if (pedestal != null) return pedestal.Hint;
                var orb = hit.collider.GetComponentInParent<Orb>();
                if (orb != null && !orb.carried && (WorldState.Instance == null || WorldState.Instance.IsReal(orb.home,orb.transform.position)))
                    return "E — забрать сферу";

                var portal = hit.collider.GetComponentInParent<PortalFrame>();
                if (portal != null)
                {
                    if (portal.IsActivated) return "Пройдите сквозь портал";
                    if (portal.IsAbsorbing) return "Сфера растворяется";
                    if (portal.HasOrb) return "Положите посох на пьедестал";
                    return "E — отдать сферу порталу";
                }
            }

            if (pad != null) return pad.barrier ? "Пройдите через гравитационный барьер" : "Наступите на гравитационную плиту";
            return null;
        }

        void Update()
        {
            if (menu != null && menu.IsOpen) return;
            if (Time.unscaledTime < nextFocus) return;
            nextFocus = Time.unscaledTime + .075f;
            focus = LookHint();
        }

        void DrawClock(float width, Color accent)
        {
            var flow = LevelFlow.Instance;
            if (flow == null) return;
            int left = Mathf.CeilToInt(flow.Level5Remaining);
            if (left != shownSeconds)
            {
                shownSeconds = left;
                timerText = $"{left/60:00}:{left%60:00}";
            }

            float x = (width-148f)*.5f;
            bool hasClock = tools != null && tools.clock;
            float pulse = hasClock ? tools.ClockRemaining : 0f;
            Panel(new Rect(x,16f,148f,hasClock ? 69f : 43f),left <= 30 ? new Color(1f,.36f,.26f) : accent);
            GUI.Label(new Rect(x,18f,148f,38f),timerText,timer);
            if (!hasClock) return;

            int tick = pulse > 0f ? Mathf.CeilToInt(pulse*10f) : tools.ClockCooldown > 0f ? -1 : 0;
            if (tick != shownPulse)
            {
                shownPulse = tick;
                clockText = tick > 0 ? $"{tick/10f:0.0} с" : tick == -1 ? "Пауза часов" : "Q — часы";
            }
            GUI.Label(new Rect(x,56f,148f,21f),clockText,small);
            if (pulse > 0f)
                Fill(new Rect(x+8f,79f,132f * Mathf.Clamp01(pulse/tools.clockDuration),3f),Cold);
        }

        void OnGUI()
        {
            if ((menu != null && menu.IsOpen) || Event.current.type != EventType.Repaint) return;
            Styles();
            float sw = Screen.width, sh = Screen.height;
            bool dark = WorldState.Instance != null && WorldState.Instance.Current == WorldKind.Dark;
            Color accent = dark ? Cold : Warm;
            if (level == 5) DrawClock(sw,accent);

            Fill(new Rect(sw*.5f-2f,sh*.5f-2f,4f,4f),new Color(0f,0f,0f,.85f));
            Fill(new Rect(sw*.5f-1f,sh*.5f-1f,2f,2f),accent);
            if (!string.IsNullOrEmpty(focus))
            {
                float width = Mathf.Min(380f,sw-36f);
                var p = new Rect((sw-width)*.5f,sh*.62f,width,40f);
                Panel(p,accent);
                GUI.Label(p,focus,centered);
            }

            if (Time.unscaledTime < noticeUntil && !string.IsNullOrEmpty(notice))
            {
                float width = Mathf.Min(sw-36f,450f);
                var p = new Rect((sw-width)*.5f,sh-97f,width,49f);
                Panel(p,accent);
                GUI.Label(new Rect(p.x+8f,p.y,p.width-16f,p.height),notice,centered);
            }
        }
    }
}
