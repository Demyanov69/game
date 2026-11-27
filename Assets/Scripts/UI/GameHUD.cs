using UnityEngine;

namespace EchoIslands
{
    public class GameHUD : MonoBehaviour
    {
        public MainMenu menu;
        public ToolController tools;

        int level = 1;
        string notice, room, roomHint;
        float noticeUntil, roomUntil;
        GUIStyle heading, small, centered, timer;

        static readonly Color Warm = new(.99f,.76f,.46f);
        static readonly Color Cold = new(.39f,.95f,.86f);
        static readonly Color Ink = new(.022f,.035f,.047f,.90f);

        public void SetLevel(int value)
        {
            level = Mathf.Clamp(value,1,5);
            room = roomHint = notice = null;
            roomUntil = noticeUntil = 0f;
        }

        public void ShowRoom(string title, string hint)
        {
            room = title;
            roomHint = hint;
            roomUntil = Time.unscaledTime + 6f;
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
            if (heading != null) return;
            heading = Style(18,FontStyle.Bold,TextAnchor.MiddleLeft);
            small = Style(13,FontStyle.Normal,TextAnchor.MiddleLeft);
            centered = Style(15,FontStyle.Bold,TextAnchor.MiddleCenter);
            timer = Style(28,FontStyle.Bold,TextAnchor.MiddleLeft);
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

        string Objective()
        {
            if (Time.unscaledTime < roomUntil && !string.IsNullOrEmpty(roomHint)) return roomHint;
            return level switch
            {
                1 => tools != null && !tools.staff ? "Идите по тропе к реке. E — взять парящий посох." :
                     "ЛКМ — сменить мир. За рекой найдите сферу и портал.",
                2 => "ЛКМ — сменить мир. Найдите сферу в последнем зале.",
                3 => "ЛКМ — сменить мир. Печати меняют гравитацию при проходе.",
                4 => "ЛКМ — сменить мир. Ищите сферу, алтарь и портал.",
                5 => tools != null && !tools.clock ? "Возьмите часы на пьедестале перед первым пролётом." :
                     "Q — короткий переход. Откройте портал до конца отсчёта.",
                _ => ""
            };
        }

        void OnGUI()
        {
            if (menu != null && menu.IsOpen) return;
            Styles();
            float sw = Screen.width, sh = Screen.height;
            bool dark = WorldState.Instance != null && WorldState.Instance.Current == WorldKind.Dark;
            Color accent = dark ? Cold : Warm;
            float panelWidth = Mathf.Min(370f,sw * .61f);
            Panel(new Rect(18f,18f,panelWidth,102f),accent);
            GUI.Label(new Rect(30f,25f,panelWidth-24f,27f),LevelFlow.LevelNames[level],heading);
            string state = (dark ? "Тёмный мир" : "Светлый мир") + (string.IsNullOrEmpty(room) ? "" : "   ·   " + room);
            GUI.Label(new Rect(30f,51f,panelWidth-24f,22f),state,small);
            GUI.Label(new Rect(30f,72f,panelWidth-24f,40f),Objective(),small);
            GUI.Label(new Rect(18f,sh-33f,270f,21f),"WASD — движение    Пробел — прыжок    Esc — меню",small);

            if (level == 5 && LevelFlow.Instance != null)
            {
                int left = Mathf.CeilToInt(LevelFlow.Instance.Level5Remaining);
                float right = sw - 196f;
                Color clockAccent = left <= 60 ? new Color(1f,.36f,.26f) : accent;
                Panel(new Rect(right,18f,178f,83f),clockAccent);
                GUI.Label(new Rect(right+12f,21f,146f,40f),$"{left/60:00}:{left%60:00}",timer);
                if (tools != null && tools.clock)
                {
                    string label = tools.ClockRemaining > 0f ? $"Импульс  {tools.ClockRemaining:0.0} с" :
                        tools.ClockCooldown > 0f ? "Часы заряжаются" : "Q — запустить часы";
                    GUI.Label(new Rect(right+12f,58f,154f,24f),label,small);
                    Fill(new Rect(right+12f,87f,154f,3f),new Color(.21f,.27f,.27f));
                    if (tools.ClockRemaining > 0f)
                        Fill(new Rect(right+12f,87f,154f * Mathf.Clamp01(tools.ClockRemaining/tools.clockDuration),3f),Cold);
                }
            }

            Fill(new Rect(sw*.5f-2f,sh*.5f-2f,4f,4f),new Color(0f,0f,0f,.85f));
            Fill(new Rect(sw*.5f-1f,sh*.5f-1f,2f,2f),accent);
            string focus = LookHint();
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
