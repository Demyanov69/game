using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoIslands
{
    /// <summary>Painterly split-world menu with simple fantasy stone menu tiles.</summary>
    public class MainMenu : MonoBehaviour
    {
        public FirstPersonController fps;
        public LevelFlow flow;

        enum Page { Home, Pause, Chapters, Settings, Win }
        Page page = Page.Home, returnPage = Page.Home;
        bool open = true, confirmReset, startedPlay;

        static bool? resumeFirstLevel;
        Texture2D backdrop, shade;
        GUIStyle title, subtitle, text, tiny, tileLabel, tileLabelDim;

        public bool IsOpen => open;

        void Awake()
        {
            backdrop = Resources.Load<Texture2D>("EchoMenu/Vault");
            shade = new Texture2D(128,1,TextureFormat.RGBA32,false);
            shade.wrapMode = TextureWrapMode.Clamp;
            for (int i=0; i<shade.width; i++)
                shade.SetPixel(i,0,new Color(.024f,.033f,.045f,Mathf.Lerp(.98f,.02f,i/(shade.width-1f))));
            shade.Apply();
        }

        IEnumerator Start()
        {
            AudioListener.volume = PlayerPrefs.GetFloat("echo_volume",.85f);
            Open();
            if (!resumeFirstLevel.HasValue) yield break;
            bool restore = resumeFirstLevel.Value;
            resumeFirstLevel = null;
            yield return null;
            CloseAndPlay(1,restore);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!open) Open();
                else if (page == Page.Pause) Resume();
                else if (page == Page.Settings || page == Page.Chapters)
                {
                    page = returnPage;
                    confirmReset = false;
                }
            }
        }

        public void Open()
        {
            if (startedPlay && flow != null) flow.SaveCurrent();
            open = true;
            page = startedPlay ? Page.Pause : Page.Home;
            confirmReset = false;
            if (fps != null) fps.SetControl(false);
            Time.timeScale = 0f;
        }

        void Resume()
        {
            open = false;
            Time.timeScale = 1f;
            if (fps != null) fps.SetControl(true);
        }

        public void OnStart() => CloseAndPlay(1, false);
        public void OnContinue() => CloseAndPlay(GameProgress.SavedLevel, GameProgress.HasSave);

        public void OnLoadLevel(int level)
        {
            if (level > GameProgress.HighestUnlocked) return;
            CloseAndPlay(level, false);
        }

        void CloseAndPlay(int level, bool restore)
        {
            var scene = SceneManager.GetActiveScene();
            if (level == 1 && startedPlay && scene.buildIndex >= 0)
            {
                resumeFirstLevel = restore;
                SceneManager.LoadScene(scene.buildIndex);
                return;
            }
            open = false;
            startedPlay = true;
            Time.timeScale = 1f;
            if (flow != null) flow.Begin(level, restore);
            if (fps != null) fps.SetControl(true);
        }

        public void ShowVictory()
        {
            open = true;
            page = Page.Win;
            if (fps != null) fps.SetControl(false);
            Time.timeScale = 0f;
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void InitStyles()
        {
            if (title != null) return;

            title = MakeLabel(52, FontStyle.Bold, new Color(.98f, .96f, .92f));
            subtitle = MakeLabel(15, FontStyle.Normal, new Color(.92f, .90f, .86f));
            text = MakeLabel(14, FontStyle.Normal, new Color(.95f, .95f, .94f));
            tiny = MakeLabel(11, FontStyle.Normal, new Color(.82f, .82f, .82f));
            tileLabel = MakeLabel(15, FontStyle.Bold, new Color(.98f, .97f, .94f));
            tileLabelDim = MakeLabel(14, FontStyle.Bold, new Color(.80f, .80f, .80f));
        }

        GUIStyle MakeLabel(int size, FontStyle style, Color color)
        {
            var g = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = style,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            g.normal.textColor = color;
            return g;
        }

        void Fill(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        void DrawScene(float sw, float sh)
        {
            Fill(new Rect(0,0,sw,sh),new Color(.026f,.038f,.056f));
            if (backdrop != null)
                GUI.DrawTexture(new Rect(0f,0f,sw,sh),backdrop,ScaleMode.ScaleAndCrop);
            GUI.DrawTexture(new Rect(0f,0f,Mathf.Min(sw,800f),sh),shade,ScaleMode.StretchToFill);
            Fill(new Rect(0f,0f,sw,4f),new Color(.76f,.52f,.27f));
        }

        bool MenuButton(Rect rect, string label, bool active = true)
        {
            Event e = Event.current;
            bool hover = rect.Contains(e.mousePosition) && active;

            Color edge = active ? new Color(.77f,.54f,.31f) : new Color(.25f,.30f,.34f);
            Fill(new Rect(rect.x,rect.y+2f,rect.width,rect.height),new Color(0f,0f,0f,.30f));
            Fill(rect,active && hover ? new Color(.23f,.16f,.12f,.98f) : new Color(.055f,.078f,.10f,.93f));
            Fill(new Rect(rect.x,rect.y,2f,rect.height),edge);
            Fill(new Rect(rect.x,rect.yMax-1f,rect.width,1f),new Color(.48f,.31f,.16f,.40f));
            GUI.Label(new Rect(rect.x+15f,rect.y+2f,rect.width-40f,rect.height-4f),
                label,active?tileLabel:tileLabelDim);
            if (active && hover)
                GUI.Label(new Rect(rect.xMax-24f,rect.y+2f,17f,rect.height-4f),"›",tileLabel);

            return active && GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        void DrawListHeader(float x, float y)
        {
            GUI.Label(new Rect(x, y, 430f, 73f), "Echo Islands", title);
            GUI.Label(new Rect(x, y + 77f, 355f, 29f), "Светлый мир  /  Проклятый мир", subtitle);
        }

        void OnGUI()
        {
            if (!open) return;
            InitStyles();

            float scale = Mathf.Min(1.25f,Screen.height / 690f,Screen.width / 530f);
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float sw = Screen.width / scale;
            float sh = Screen.height / scale;

            DrawScene(sw, sh);

            float listX = Mathf.Clamp(sw * .055f,30f,100f);
            float listY = Mathf.Min(sh*.14f,122f);
            float listWidth = Mathf.Min(333f, sw-listX-36f);

            DrawListHeader(listX, listY);
            DrawPage(listX, listY + 116f, listWidth);

            GUI.matrix = old;
        }

        void DrawPage(float x, float y, float width)
        {
            switch (page)
            {
                case Page.Home: Home(x, y, width); break;
                case Page.Pause: Pause(x, y, width); break;
                case Page.Chapters: Chapters(x, y, width); break;
                case Page.Settings: Settings(x, y, width); break;
                case Page.Win: Win(x, y, width); break;
            }
        }

        void Home(float x, float y, float width)
        {
            float h = 42f;
            if (MenuButton(new Rect(x, y, width, h), "НОВАЯ ИГРА")) OnStart();
            if (MenuButton(new Rect(x, y + 50f, width, h), "ПРОДОЛЖИТЬ", GameProgress.HasSave)) OnContinue();
            if (MenuButton(new Rect(x, y + 100f, width, h), "ВЫБОР УРОВНЯ")) { returnPage = Page.Home; page = Page.Chapters; }
            if (MenuButton(new Rect(x, y + 150f, width, h), "НАСТРОЙКИ")) { returnPage = Page.Home; page = Page.Settings; }
            if (MenuButton(new Rect(x, y + 200f, width, h), "ВЫХОД")) OnQuit();
        }

        void Pause(float x, float y, float width)
        {
            GUI.Label(new Rect(x, y - 18f, width, 20f), "Пауза", text);
            float h = 42f;
            if (MenuButton(new Rect(x, y + 10f, width, h), "ПРОДОЛЖИТЬ")) Resume();
            if (MenuButton(new Rect(x, y + 60f, width, h), "СОХРАНИТЬ")) if (flow != null) flow.SaveCurrent();
            if (MenuButton(new Rect(x, y + 110f, width, h), "ПЕРЕИГРАТЬ УРОВЕНЬ")) CloseAndPlay(flow != null ? flow.current : 1, false);
            if (MenuButton(new Rect(x, y + 160f, width, h), "ВЫБОР УРОВНЯ")) { returnPage = Page.Pause; page = Page.Chapters; }
            if (MenuButton(new Rect(x, y + 210f, width, h), "НАСТРОЙКИ")) { returnPage = Page.Pause; page = Page.Settings; }
            if (MenuButton(new Rect(x, y + 260f, width, h), "ГЛАВНОЕ МЕНЮ")) page = Page.Home;
        }

        void Chapters(float x, float y, float width)
        {
            GUI.Label(new Rect(x, y - 18f, width, 20f), "Выбор уровня", text);
            float h = 40f;
            for (int i = 1; i <= 5; i++)
            {
                bool unlocked = i <= GameProgress.HighestUnlocked;
                string label = i.ToString("00") + "   " + LevelFlow.LevelNames[i] + (unlocked ? "" : "   /   ЗАКРЫТО");
                if (MenuButton(new Rect(x, y + 8f + (i - 1) * 48f, width, h), label, unlocked)) OnLoadLevel(i);
            }
            if (MenuButton(new Rect(x, y + 8f + 5 * 48f, width, h), "НАЗАД")) page = returnPage;
        }

        void Settings(float x, float y, float width)
        {
            GUI.Label(new Rect(x, y - 18f, width, 20f), "Настройки", text);
            float lineY = y + 8f;
            GUI.Label(new Rect(x, lineY, width, 18f), "Громкость   " + Mathf.RoundToInt(PlayerPrefs.GetFloat("echo_volume", .85f) * 100f) + "%", tiny);
            float vol = PlayerPrefs.GetFloat("echo_volume", .85f);
            float newVol = GUI.HorizontalSlider(new Rect(x, lineY + 22f, width, 18f), vol, 0f, 1f);
            if (!Mathf.Approximately(vol, newVol))
            {
                AudioListener.volume = newVol;
                PlayerPrefs.SetFloat("echo_volume", newVol);
            }

            lineY += 54f;
            float sens = PlayerPrefs.GetFloat("echo_sensitivity", 2.2f);
            GUI.Label(new Rect(x, lineY, width, 18f), "Чувствительность   " + sens.ToString("0.0"), tiny);
            float newSens = GUI.HorizontalSlider(new Rect(x, lineY + 22f, width, 18f), sens, .45f, 5f);
            if (!Mathf.Approximately(sens, newSens))
            {
                PlayerPrefs.SetFloat("echo_sensitivity", newSens);
                if (fps != null) fps.lookSensitivity = newSens;
            }

            lineY += 58f;
            if (MenuButton(new Rect(x, lineY, width, 38f), Screen.fullScreen ? "ПОЛНЫЙ ЭКРАН: ВКЛ" : "ПОЛНЫЙ ЭКРАН: ВЫКЛ")) Screen.fullScreen = !Screen.fullScreen;
            if (MenuButton(new Rect(x, lineY + 46f, width, 38f), QualitySettings.vSyncCount > 0 ? "V-SYNC: ВКЛ" : "V-SYNC: ВЫКЛ")) QualitySettings.vSyncCount = QualitySettings.vSyncCount > 0 ? 0 : 1;

            if (!confirmReset)
            {
                if (MenuButton(new Rect(x, lineY + 92f, width, 38f), "СБРОСИТЬ СОХРАНЕНИЯ")) confirmReset = true;
            }
            else
            {
                GUI.Label(new Rect(x, lineY + 90f, width, 18f), "Удалить все сохранения?", tiny);
                if (MenuButton(new Rect(x, lineY + 114f, width, 38f), "ДА, УДАЛИТЬ")) { GameProgress.Reset(); confirmReset = false; }
                if (MenuButton(new Rect(x, lineY + 160f, width, 38f), "ОТМЕНА")) confirmReset = false;
            }

            if (MenuButton(new Rect(x, y + (confirmReset ? 332f : 282f), width, 40f), "ГОТОВО / НАЗАД"))
            {
                PlayerPrefs.Save();
                page = returnPage;
                confirmReset = false;
            }
        }

        void Win(float x, float y, float width)
        {
            GUI.Label(new Rect(x, y - 18f, width, 20f), "Все порталы пробуждены.", text);
            GUI.Label(new Rect(x, y + 8f, width, 36f), "Путь завершён. Можно начать заново или выбрать любую главу.", subtitle);
            if (MenuButton(new Rect(x, y + 64f, width, 42f), "ИГРАТЬ С НАЧАЛА")) OnStart();
            if (MenuButton(new Rect(x, y + 114f, width, 42f), "ВЫБОР УРОВНЯ")) { returnPage = Page.Win; page = Page.Chapters; }
            if (MenuButton(new Rect(x, y + 164f, width, 42f), "ВЫХОД")) OnQuit();
        }

        void OnDestroy()
        {
            if (shade != null) Destroy(shade);
        }
    }
}
