using UnityEngine;

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

        Texture2D pixel;
        GUIStyle title, subtitle, text, tiny, tileLabel, tileLabelDim;

        public bool IsOpen => open;

        void Awake()
        {
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            pixel.wrapMode = TextureWrapMode.Repeat;
        }

        void Start()
        {
            AudioListener.volume = PlayerPrefs.GetFloat("echo_volume", .85f);
            Open();
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

            title = MakeLabel(34, FontStyle.Bold, new Color(.98f, .96f, .92f));
            subtitle = MakeLabel(13, FontStyle.Normal, new Color(.92f, .90f, .86f));
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
            GUI.DrawTexture(rect, pixel);
            GUI.color = old;
        }

        void DrawScene(float sw, float sh)
        {
            // Sky
            Fill(new Rect(0, 0, sw * .5f, sh), new Color(.89f, .65f, .40f, 1f));
            Fill(new Rect(sw * .5f, 0, sw * .5f, sh), new Color(.20f, .20f, .24f, 1f));
            Fill(new Rect(sw * .5f - 1.5f, 0, 3f, sh), new Color(.91f, .91f, .90f, .13f));

            // Clouds left
            Fill(new Rect(42f, 54f, 180f, 16f), new Color(1f, .95f, .82f, .38f));
            Fill(new Rect(76f, 92f, 136f, 13f), new Color(1f, .95f, .84f, .28f));
            Fill(new Rect(sw * .44f, 48f, 122f, 15f), new Color(1f, .95f, .84f, .32f));
            Fill(new Rect(sw * .60f, 88f, 114f, 12f), new Color(.64f, .70f, .74f, .14f));

            // Background mountain range left
            Fill(new Rect(sw * .28f, sh * .22f, sw * .24f, sh * .17f), new Color(.88f, .39f, .31f, 1f));
            Fill(new Rect(sw * .16f, sh * .32f, sw * .26f, sh * .08f), new Color(.50f, .21f, .43f, 1f));
            Fill(new Rect(sw * .34f, sh * .34f, sw * .17f, sh * .06f), new Color(.56f, .25f, .48f, 1f));

            // Dead ridge right
            Fill(new Rect(sw * .56f, sh * .27f, sw * .22f, sh * .13f), new Color(.29f, .31f, .35f, 1f));
            Fill(new Rect(sw * .75f, sh * .24f, sw * .18f, sh * .16f), new Color(.24f, .26f, .30f, 1f));
            Fill(new Rect(sw * .62f, sh * .35f, sw * .24f, sh * .07f), new Color(.18f, .19f, .22f, 1f));

            // Forest layers left
            Fill(new Rect(0, sh * .38f, sw * .54f, sh * .11f), new Color(.36f, .14f, .35f, 1f));
            Fill(new Rect(0, sh * .43f, sw * .54f, sh * .14f), new Color(.24f, .12f, .28f, 1f));
            Fill(new Rect(0, sh * .49f, sw * .54f, sh * .18f), new Color(.16f, .10f, .20f, 1f));

            // Forest layers right - dead / cold
            Fill(new Rect(sw * .46f, sh * .40f, sw * .54f, sh * .11f), new Color(.19f, .16f, .24f, 1f));
            Fill(new Rect(sw * .46f, sh * .46f, sw * .54f, sh * .16f), new Color(.14f, .12f, .18f, 1f));
            Fill(new Rect(sw * .46f, sh * .52f, sw * .54f, sh * .20f), new Color(.10f, .09f, .13f, 1f));

            // Foreground valley silhouettes left/right
            Fill(new Rect(0, sh * .78f, sw * .22f, sh * .22f), new Color(.07f, .06f, .10f, 1f));
            Fill(new Rect(sw * .80f, sh * .73f, sw * .20f, sh * .27f), new Color(.05f, .05f, .08f, 1f));

            // Central mist/portal seam
            float pulse = .75f + Mathf.Sin(Time.unscaledTime * 1.1f) * .08f;
            Fill(new Rect(sw * .5f - 26f, sh * .30f, 52f, sh * .44f), new Color(.82f, .89f, .95f, .08f * pulse));
            Fill(new Rect(sw * .5f - 2f, sh * .24f, 4f, sh * .50f), new Color(.93f, .96f, .97f, .10f));

            // Subtle left overlay for readability.
            Fill(new Rect(36f, 36f, 360f, sh - 72f), new Color(.06f, .05f, .07f, .14f));
        }

        bool MenuButton(Rect rect, string label, bool active = true)
        {
            Event e = Event.current;
            bool hover = rect.Contains(e.mousePosition) && active;

            // outer shadow / base
            Fill(new Rect(rect.x, rect.y + 3f, rect.width, rect.height), new Color(0f, 0f, 0f, .18f));
            Fill(rect, new Color(.07f, .07f, .09f, .50f));

            // split stone slab
            Fill(new Rect(rect.x + 1f, rect.y + 1f, rect.width * .5f - 1f, rect.height - 2f),
                active ? new Color(.57f, .48f, .34f, hover ? .92f : .82f) : new Color(.32f, .32f, .32f, .70f));
            Fill(new Rect(rect.x + rect.width * .5f, rect.y + 1f, rect.width * .5f - 1f, rect.height - 2f),
                active ? new Color(.20f, .24f, .28f, hover ? .94f : .84f) : new Color(.26f, .26f, .28f, .74f));

            Fill(new Rect(rect.x, rect.y, rect.width, 1f), new Color(1f, 1f, 1f, .12f));
            Fill(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(0f, 0f, 0f, .25f));
            Fill(new Rect(rect.x + 16f, rect.yMax - 7f, rect.width - 32f, 1.5f), hover ? new Color(.98f, .98f, .95f, .92f) : new Color(.95f, .95f, .92f, .35f));

            GUI.Label(new Rect(rect.x + 16f, rect.y + 7f, rect.width - 24f, rect.height - 12f), label, active ? tileLabel : tileLabelDim);
            if (hover) GUI.Label(new Rect(rect.x + rect.width - 26f, rect.y + 7f, 16f, rect.height - 12f), ">", tileLabel);

            return active && GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        void DrawListHeader(float x, float y)
        {
            GUI.Label(new Rect(x, y, 320f, 52f), "ECHO ISLANDS", title);
            GUI.Label(new Rect(x, y + 58f, 350f, 40f), "Два мира: живой свет и мёртвая тьма. Откройте путь между ними.", subtitle);
        }

        void OnGUI()
        {
            if (!open) return;
            InitStyles();

            float scale = Mathf.Clamp(Screen.height / 900f, .72f, 1.16f);
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float sw = Screen.width / scale;
            float sh = Screen.height / scale;

            DrawScene(sw, sh);

            float listX = Mathf.Max(52f, sw * .07f);
            float listY = sh * .13f;
            float listWidth = Mathf.Min(330f, sw * .28f);

            DrawListHeader(listX, listY);
            DrawPage(listX, listY + 116f, listWidth);

            GUI.Label(new Rect(listX, sh - 44f, 360f, 18f), "LOW POLY  ·  DUNGEON OBBY  ·  TWO WORLDS", tiny);
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

            if (MenuButton(new Rect(x, y + 290f, width, 40f), "ГОТОВО / НАЗАД"))
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
            if (pixel != null) Destroy(pixel);
        }
    }
}
