using Kirurobo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace ScrollRoad.EditorTools
{
    // Sahneler tamamen koddan kurulur (elle sahne düzenlemesi yok → tekrar üretilebilir).
    // Menü: ScrollRoad → Sahneleri Kur. Ayrıca Player/URP ayarlarını saydam pencere için zorlar.
    public static class ScrollRoadSceneBuilder
    {
        public const string ScenesFolder = "Assets/Scenes";
        public const string MainMenuScenePath = ScenesFolder + "/" + SceneNames.MainMenu + ".unity";
        public const string GameScenePath = ScenesFolder + "/" + SceneNames.Game + ".unity";

        const string SpriteUnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        // Ortak UI paleti (minimalist, düz).
        static readonly Color SkyColor = new Color(0.84f, 0.93f, 0.95f);
        static readonly Color Ink = new Color(0.16f, 0.19f, 0.24f);
        static readonly Color ButtonColor = new Color(0.16f, 0.19f, 0.24f, 0.92f);
        static readonly Color CardColor = new Color(0.11f, 0.13f, 0.17f, 1f);
        static readonly Color DimColor = new Color(0.05f, 0.06f, 0.08f, 0.62f);
        static readonly Color TextLight = new Color(0.95f, 0.95f, 0.93f);
        static readonly Color Accent = new Color(0.93f, 0.45f, 0.36f); // arabanın mercan rengi

        [MenuItem("ScrollRoad/Sahneleri Kur")]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(ScenesFolder)) AssetDatabase.CreateFolder("Assets", "Scenes");

            ConfigureProjectSettings();
            BuildMainMenuScene();
            BuildGameScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };

            EditorSceneManager.OpenScene(MainMenuScenePath);
            Debug.Log("ScrollRoad sahneleri kuruldu: MainMenu + Game, Build Settings güncellendi.");
        }

        // UniWindowController saydamlığı D3D12'de hiç, D3D11'de de flip-model swapchain açıkken
        // çalışmıyor (UniWinC README, Limitations). URP'de ayrıca HDR kapalı olmalı (alfa kanalı korunsun).
        public static void ConfigureProjectSettings()
        {
            PlayerSettings.productName = "ScrollRoad";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.useFlipModelSwapchain = false;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Vehicle/car_coral.png");
            if (icon != null) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

            foreach (var asset in new[] { GraphicsSettings.defaultRenderPipeline, QualitySettings.renderPipeline })
            {
                if (asset is UniversalRenderPipelineAsset urp && urp.supportsHDR)
                {
                    urp.supportsHDR = false;
                    EditorUtility.SetDirty(urp);
                }
            }
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ Ana menü

        static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = CreateCamera(SkyColor);
            cam.orthographic = true;

            var canvas = CreateCanvas("MenuCanvas", 0);

            var title = CreateText(canvas.transform, "Title", "ScrollRoad", 96, new Vector2(0, 250), new Vector2(1000, 140), Ink);
            title.fontStyle = FontStyle.Bold;
            CreateText(canvas.transform, "Tagline", "kaydır, yol al.", 32, new Vector2(0, 170), new Vector2(800, 50), new Color(Ink.r, Ink.g, Ink.b, 0.7f));

            var carPreview = new GameObject("CarPreview", typeof(RectTransform), typeof(Image));
            carPreview.transform.SetParent(canvas.transform, false);
            var carImage = carPreview.GetComponent<Image>();
            carImage.sprite = LoadArtSprite("Assets/Art/Vehicle/car_coral.png");
            carImage.preserveAspect = true;
            SetRect(carPreview.GetComponent<RectTransform>(), new Vector2(0, 50), new Vector2(300, 140));

            var playButton = CreateButton(canvas.transform, "PlayButton", "Oyna", new Vector2(0, -90), new Vector2(320, 72), Accent);
            var statsButton = CreateButton(canvas.transform, "StatsButton", "İstatistikler", new Vector2(0, -180), new Vector2(320, 64));
            var settingsButton = CreateButton(canvas.transform, "SettingsButton", "Ayarlar", new Vector2(0, -260), new Vector2(320, 64));
            var quitButton = CreateButton(canvas.transform, "QuitButton", "Çıkış", new Vector2(0, -340), new Vector2(320, 64));

            var steamText = CreateText(canvas.transform, "SteamStatus", "", 22, Vector2.zero, new Vector2(600, 40), new Color(Ink.r, Ink.g, Ink.b, 0.6f));
            steamText.alignment = TextAnchor.LowerLeft;
            Anchor(steamText.rectTransform, new Vector2(0, 0), new Vector2(24, 20), new Vector2(0, 0));

            var controller = new GameObject("MainMenuController").AddComponent<MainMenuController>();
            Bind(controller, "playButton", playButton);
            Bind(controller, "quitButton", quitButton);
            Bind(controller, "steamStatusText", steamText);

            CreateSettingsPanel(canvas.transform, settingsButton);
            CreateStatsPanel(canvas.transform, statsButton, null, null);

            var sound = new GameObject("SoundController").AddComponent<SoundController>();
            Bind(sound, "uiClickClip", LoadClip("click_001"));

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        // ------------------------------------------------------------------ Oyun

        static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = CreateCamera(SkyColor);

            var roadGo = new GameObject("RoadDrive");
            var drive = roadGo.AddComponent<RoadDrive>();

            var windowGo = new GameObject("CompanionWindow");
            var uni = windowGo.AddComponent<UniWindowController>();
            uni.isHitTestEnabled = false;          // CompanionWindowController saydamlığa geçerken açar
            uni.autoSwitchCameraBackground = false; // arka planı RoadSceneView yönetiyor
            uni.currentCamera = cam;
            uni.hitTestType = UniWindowController.HitTestType.Opacity;
            var companion = windowGo.AddComponent<CompanionWindowController>();
            Bind(companion, "uniWindow", uni);

            var view = roadGo.AddComponent<RoadSceneView>();
            Bind(view, "drive", drive);
            Bind(view, "companionWindow", companion);
            Bind(view, "viewCamera", cam);
            Bind(view, "carSprite", LoadArtSprite("Assets/Art/Vehicle/car_coral.png"));
            Bind(view, "citySprite", LoadArtSprite("Assets/Art/City/city_skyline.png"));
            Bind(view, "hillsSprite", LoadArtSprite("Assets/Art/Background/hills_far.png"));
            Bind(view, "spriteMaterial", AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath));

            var secret = roadGo.AddComponent<SecretZoneView>();
            Bind(secret, "drive", drive);
            var stats = roadGo.AddComponent<StatsTracker>();
            Bind(stats, "drive", drive);
            var sync = roadGo.AddComponent<LeaderboardSync>();
            Bind(sync, "drive", drive);

            var sound = roadGo.AddComponent<SoundController>();
            Bind(sound, "drive", drive);
            BindArray(sound, "stepClips", new Object[] { LoadClip("tick_001"), LoadClip("tick_002"), LoadClip("tick_004") });
            Bind(sound, "turnClip", LoadClip("scratch_001"));
            Bind(sound, "cityReachedClip", LoadClip("city_reached"));
            Bind(sound, "uiClickClip", LoadClip("click_001"));

            var hud = CreateHud(drive, view, secret, companion, out var hudMenuButton, out var hudCompanionButton);
            CreatePauseUi(drive, stats, companion, hudMenuButton, hudCompanionButton);
            CreateCompanionHud(drive, secret, companion);

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        static HudController CreateHud(RoadDrive drive, RoadSceneView view, SecretZoneView secret,
            CompanionWindowController companion, out Button menuButton, out Button companionButton)
        {
            var canvas = CreateCanvas("HudCanvas", 0);
            var root = new GameObject("HudRoot", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            Stretch(root.GetComponent<RectTransform>());

            var speed = CreateText(root.transform, "Speed", "", 56, Vector2.zero, new Vector2(700, 70), Ink);
            speed.fontStyle = FontStyle.Bold;
            speed.alignment = TextAnchor.UpperLeft;
            Anchor(speed.rectTransform, new Vector2(0, 1), new Vector2(44, -32), new Vector2(0, 1));

            var distance = CreateText(root.transform, "Distance", "", 36, Vector2.zero, new Vector2(700, 48), Ink);
            distance.alignment = TextAnchor.UpperLeft;
            Anchor(distance.rectTransform, new Vector2(0, 1), new Vector2(46, -104), new Vector2(0, 1));

            var record = CreateText(root.transform, "Record", "", 26, Vector2.zero, new Vector2(700, 36), Ink);
            record.alignment = TextAnchor.UpperLeft;
            Anchor(record.rectTransform, new Vector2(0, 1), new Vector2(46, -152), new Vector2(0, 1));

            menuButton = CreateButton(root.transform, "MenuButton", "Menü  (Esc)", Vector2.zero, new Vector2(210, 52), fontSize: 22);
            Anchor((RectTransform)menuButton.transform, new Vector2(1, 1), new Vector2(-40, -36), new Vector2(1, 1));
            companionButton = CreateButton(root.transform, "CompanionButton", "Masaüstü  (F9)", Vector2.zero, new Vector2(230, 52), fontSize: 22);
            Anchor((RectTransform)companionButton.transform, new Vector2(1, 1), new Vector2(-266, -36), new Vector2(1, 1));

            // Şehre ilerleme çizgisi: sol üst bilgi bloğunun altında (yol kenarlarda ekranın altına kıvrıldığı
            // için alt kenara konmadı). Dolgu = anlık konum, işaret = bu ayki en uzak nokta.
            var track = new GameObject("ProgressTrack", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(root.transform, false);
            var trackRect = track.GetComponent<RectTransform>();
            trackRect.sizeDelta = new Vector2(380, 6);
            Anchor(trackRect, new Vector2(0, 1), new Vector2(46, -200), new Vector2(0, 1));

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            fill.GetComponent<Image>().color = Accent;
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

            var marker = new GameObject("MaxMarker", typeof(RectTransform), typeof(Image));
            marker.transform.SetParent(track.transform, false);
            marker.GetComponent<Image>().color = Ink;
            var markerRect = marker.GetComponent<RectTransform>();
            markerRect.anchorMin = Vector2.zero;
            markerRect.anchorMax = new Vector2(0, 1);
            markerRect.sizeDelta = new Vector2(4, 14);

            var city = CreateText(root.transform, "CityRemaining", "", 22, Vector2.zero, new Vector2(380, 30), Ink);
            city.alignment = TextAnchor.UpperLeft;
            Anchor(city.rectTransform, new Vector2(0, 1), new Vector2(46, -214), new Vector2(0, 1));

            var whisper = CreateText(root.transform, "Whisper", "", 38, new Vector2(0, 190), new Vector2(1200, 60), SecretZoneView.WhisperColor);
            whisper.fontStyle = FontStyle.Italic;

            var toast = CreateText(root.transform, "OfflineToast", "", 26, Vector2.zero, new Vector2(1000, 40), Ink);
            Anchor(toast.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(0.5f, 1));

            var hud = new GameObject("HudController").AddComponent<HudController>();
            Bind(hud, "drive", drive);
            Bind(hud, "view", view);
            Bind(hud, "secretZone", secret);
            Bind(hud, "companionWindow", companion);
            Bind(hud, "hudRoot", root);
            Bind(hud, "speedText", speed);
            Bind(hud, "distanceText", distance);
            Bind(hud, "recordText", record);
            Bind(hud, "cityText", city);
            Bind(hud, "whisperText", whisper);
            Bind(hud, "toastText", toast);
            Bind(hud, "progressTrack", track.GetComponent<Image>());
            Bind(hud, "progressFill", fillRect);
            Bind(hud, "maxMarker", markerRect);
            BindArray(hud, "inkGraphics", new Object[] { marker.GetComponent<Image>() });

            CreateCelebrationBanner(canvas.transform, drive, companion);
            return hud;
        }

        static void CreateCelebrationBanner(Transform canvas, RoadDrive drive, CompanionWindowController companion)
        {
            var banner = new GameObject("CityBanner", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            banner.transform.SetParent(canvas, false);
            banner.GetComponent<Image>().color = CardColor;
            SetRect(banner.GetComponent<RectTransform>(), new Vector2(0, 170), new Vector2(940, 190));

            var title = CreateText(banner.transform, "Title", "ŞEHRE ULAŞTIN!", 58, new Vector2(0, 30), new Vector2(900, 80), new Color(0.98f, 0.80f, 0.28f));
            title.fontStyle = FontStyle.Bold;
            // Gizli reveal kancası: kim şehre ulaştı, ay sonuna kadar rakiplerden gizli.
            CreateText(banner.transform, "Subtitle", "Rakiplerin bunu görmüyor — ay sonunu bekle.", 26, new Vector2(0, -45), new Vector2(900, 40), TextLight);

            banner.SetActive(false);
            var controller = new GameObject("CelebrationController").AddComponent<CelebrationController>();
            Bind(controller, "drive", drive);
            Bind(controller, "companionWindow", companion);
            Bind(controller, "bannerGroup", banner.GetComponent<CanvasGroup>());
        }

        static void CreatePauseUi(RoadDrive drive, StatsTracker stats, CompanionWindowController companion, Button hudMenuButton, Button hudCompanionButton)
        {
            var canvas = CreateCanvas("PauseCanvas", 10);

            var pausePanel = CreateDim(canvas.transform, "PausePanel");
            var card = CreateCard(pausePanel.transform, "Card", new Vector2(480, 760));
            CreateText(card.transform, "Title", "Duraklatıldı", 44, new Vector2(0, 320), new Vector2(440, 70), TextLight).fontStyle = FontStyle.Bold;

            string[] labels = { "Devam Et", "Bu Ayın Sıralaması", "Geçen Ayın Sonuçları", "İstatistikler", "Ayarlar", "Masaüstü Modu", "Ana Menü", "Çıkış" };
            var buttons = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                var color = i == 0 ? Accent : new Color(0.24f, 0.27f, 0.33f);
                buttons[i] = CreateButton(card.transform, labels[i].Replace(" ", "") + "Button", labels[i], new Vector2(0, 230 - i * 74), new Vector2(380, 60), color);
            }

            var settings = CreateSettingsPanel(pausePanel.transform, buttons[4]);
            var statsCtrl = CreateStatsPanel(pausePanel.transform, buttons[3], drive, stats);

            var lbPanel = CreateDim(canvas.transform, "LeaderboardPanel");
            var lbCard = CreateCard(lbPanel.transform, "Card", new Vector2(820, 640));
            var lbTitle = CreateText(lbCard.transform, "Title", "", 40, new Vector2(0, 260), new Vector2(760, 60), TextLight);
            lbTitle.fontStyle = FontStyle.Bold;
            var lbText = CreateText(lbCard.transform, "Entries", "", 26, new Vector2(0, 10), new Vector2(720, 400), TextLight);
            lbText.alignment = TextAnchor.UpperLeft;
            var lbClose = CreateButton(lbCard.transform, "CloseButton", "Kapat", new Vector2(0, -250), new Vector2(240, 60));

            pausePanel.SetActive(false);
            lbPanel.SetActive(false);
            var pause = new GameObject("PauseController").AddComponent<PauseController>();
            Bind(pause, "drive", drive);
            Bind(pause, "companionWindow", companion);
            Bind(pause, "pausePanel", pausePanel);
            Bind(pause, "leaderboardPanel", lbPanel);
            Bind(pause, "leaderboardTitle", lbTitle);
            Bind(pause, "leaderboardText", lbText);
            Bind(pause, "hudMenuButton", hudMenuButton);
            Bind(pause, "hudCompanionButton", hudCompanionButton);
            Bind(pause, "resumeButton", buttons[0]);
            Bind(pause, "leaderboardButton", buttons[1]);
            Bind(pause, "revealButton", buttons[2]);
            Bind(pause, "companionButton", buttons[5]);
            Bind(pause, "mainMenuButton", buttons[6]);
            Bind(pause, "quitButton", buttons[7]);
            Bind(pause, "leaderboardCloseButton", lbClose);
            BindArray(pause, "subPanels", new Object[] { settings.Panel, statsCtrl.Panel });
        }

        static SettingsController CreateSettingsPanel(Transform parent, Button openButton)
        {
            var panel = CreateDim(parent, "SettingsPanel");
            var card = CreateCard(panel.transform, "Card", new Vector2(760, 520));
            CreateText(card.transform, "Title", "Ayarlar", 44, new Vector2(0, 200), new Vector2(700, 70), TextLight).fontStyle = FontStyle.Bold;

            var volumeLabel = CreateText(card.transform, "VolumeLabel", "Ses", 30, new Vector2(-260, 90), new Vector2(160, 50), TextLight);
            volumeLabel.alignment = TextAnchor.MiddleLeft;
            var slider = CreateSlider(card.transform, "VolumeSlider", new Vector2(40, 90), new Vector2(400, 40));
            var volumeValue = CreateText(card.transform, "VolumeValue", "", 26, new Vector2(300, 90), new Vector2(110, 50), TextLight);

            var toggle = CreateToggle(card.transform, "StartInCompanionToggle", "Oyun masaüstü modunda başlasın", new Vector2(-30, 0), new Vector2(600, 50));
            var note = CreateText(card.transform, "Note", "Masaüstü modu: küçük, saydam, hep üstte pencere. F9 ile aç / kapat.", 22,
                new Vector2(0, -70), new Vector2(680, 60), new Color(TextLight.r, TextLight.g, TextLight.b, 0.6f));
            note.horizontalOverflow = HorizontalWrapMode.Wrap;

            var close = CreateButton(card.transform, "CloseButton", "Kapat", new Vector2(0, -190), new Vector2(240, 60));

            panel.SetActive(false); // Editor'de sahne temiz görünsün (runtime'da da kapalı başlar)
            var controller = new GameObject("SettingsController").AddComponent<SettingsController>();
            Bind(controller, "settingsPanel", panel);
            Bind(controller, "volumeSlider", slider);
            Bind(controller, "volumeValueText", volumeValue);
            Bind(controller, "startInCompanionToggle", toggle);
            Bind(controller, "openButton", openButton);
            Bind(controller, "closeButton", close);
            return controller;
        }

        static StatsController CreateStatsPanel(Transform parent, Button openButton, RoadDrive drive, StatsTracker stats)
        {
            var panel = CreateDim(parent, "StatsPanel");
            var card = CreateCard(panel.transform, "Card", new Vector2(760, 640));
            CreateText(card.transform, "Title", "İstatistikler", 44, new Vector2(0, 260), new Vector2(700, 70), TextLight).fontStyle = FontStyle.Bold;
            var text = CreateText(card.transform, "StatsText", "", 28, new Vector2(0, 0), new Vector2(640, 420), TextLight);
            text.alignment = TextAnchor.UpperLeft;
            var close = CreateButton(card.transform, "CloseButton", "Kapat", new Vector2(0, -250), new Vector2(240, 60));

            panel.SetActive(false);
            var controller = new GameObject("StatsController").AddComponent<StatsController>();
            Bind(controller, "drive", drive);
            Bind(controller, "stats", stats);
            Bind(controller, "statsPanel", panel);
            Bind(controller, "statsText", text);
            Bind(controller, "openButton", openButton);
            Bind(controller, "closeButton", close);
            return controller;
        }

        // Masaüstü arkadaşı arayüzü: sabit piksel ölçekli (küçük pencerede okunur kalsın diye).
        static void CreateCompanionHud(RoadDrive drive, SecretZoneView secret, CompanionWindowController companion)
        {
            var canvasGo = new GameObject("CompanionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            canvasGo.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            var root = new GameObject("CompanionRoot", typeof(RectTransform));
            root.transform.SetParent(canvasGo.transform, false);
            Stretch(root.GetComponent<RectTransform>());

            var km = CreateText(root.transform, "Km", "", 15, Vector2.zero, new Vector2(200, 22), TextLight);
            km.fontStyle = FontStyle.Bold;
            km.alignment = TextAnchor.UpperLeft;
            Anchor(km.rectTransform, new Vector2(0, 1), new Vector2(8, -6), new Vector2(0, 1));
            var outline = km.gameObject.AddComponent<Outline>(); // her masaüstü arka planında okunur kalsın
            outline.effectColor = new Color(0.08f, 0.09f, 0.12f, 0.9f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            var menuButton = CreateButton(root.transform, "MenuButton", "≡", Vector2.zero, new Vector2(26, 26), fontSize: 20);
            Anchor((RectTransform)menuButton.transform, new Vector2(1, 1), new Vector2(-6, -6), new Vector2(1, 1));

            var panel = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            panel.GetComponent<Image>().color = CardColor;
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(150, 70);
            Anchor(panelRect, new Vector2(1, 1), new Vector2(-6, -36), new Vector2(1, 1));
            var exitButton = CreateButton(panel.transform, "ExitCompanionButton", "Tam pencere", new Vector2(0, 16), new Vector2(138, 28), fontSize: 14);
            var quitButton = CreateButton(panel.transform, "QuitButton", "Çıkış", new Vector2(0, -17), new Vector2(138, 28), fontSize: 14);

            var whisper = CreateText(root.transform, "Whisper", "", 13, Vector2.zero, new Vector2(300, 20), SecretZoneView.WhisperColor);
            whisper.fontStyle = FontStyle.Italic;
            Anchor(whisper.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(0.5f, 0));

            var controller = new GameObject("CompanionHudController").AddComponent<CompanionHudController>();
            Bind(controller, "drive", drive);
            Bind(controller, "secretZone", secret);
            Bind(controller, "companionWindow", companion);
            Bind(controller, "root", root);
            Bind(controller, "kmText", km);
            Bind(controller, "whisperText", whisper);
            Bind(controller, "menuButton", menuButton);
            Bind(controller, "menuPanel", panel);
            Bind(controller, "exitCompanionButton", exitButton);
            Bind(controller, "quitButton", quitButton);
        }

        // ------------------------------------------------------------------ yardımcılar

        static Camera CreateCamera(Color background)
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0, 0, -10);
            var cam = go.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.allowHDR = false;   // saydam pencere: alfa kanalı korunmalı
            cam.allowMSAA = false;
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            return cam;
        }

        static GameObject CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        static GameObject CreateDim(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = DimColor;
            Stretch(go.GetComponent<RectTransform>());
            return go;
        }

        static GameObject CreateCard(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = CardColor;
            SetRect(go.GetComponent<RectTransform>(), Vector2.zero, size);
            return go;
        }

        static Text CreateText(Transform parent, string name, string content, int fontSize, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = GetFont();
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(go.GetComponent<RectTransform>(), pos, size);
            return text;
        }

        static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color? color = null, int fontSize = 28)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color ?? ButtonColor;
            SetRect(go.GetComponent<RectTransform>(), pos, size);
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.colorMultiplier = 1.15f;
            button.colors = colors;
            CreateText(go.transform, "Text", label, fontSize, Vector2.zero, size, TextLight);
            return button;
        }

        static Slider CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), pos, size);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            bg.GetComponent<Image>().color = new Color(0.30f, 0.33f, 0.40f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.4f);
            bgRect.anchorMax = new Vector2(1, 0.6f);
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = new Vector2(0, 0.4f);
            faRect.anchorMax = new Vector2(1, 0.6f);
            faRect.offsetMin = new Vector2(6, 0);
            faRect.offsetMax = new Vector2(-6, 0);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = Accent;
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var haRect = handleArea.GetComponent<RectTransform>();
            haRect.anchorMin = Vector2.zero;
            haRect.anchorMax = Vector2.one;
            haRect.offsetMin = new Vector2(10, 0);
            haRect.offsetMax = new Vector2(-10, 0);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            handle.GetComponent<Image>().color = TextLight;
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            return slider;
        }

        static Toggle CreateToggle(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), pos, size);

            var box = new GameObject("Background", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(go.transform, false);
            box.GetComponent<Image>().color = new Color(0.30f, 0.33f, 0.40f);
            var boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = boxRect.anchorMax = new Vector2(0, 0.5f);
            boxRect.pivot = new Vector2(0, 0.5f);
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = new Vector2(36, 36);

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(box.transform, false);
            check.GetComponent<Image>().color = Accent;
            var checkRect = check.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(7, 7);
            checkRect.offsetMax = new Vector2(-7, -7);

            var text = CreateText(go.transform, "Label", label, 26, Vector2.zero, new Vector2(size.x - 52, size.y), TextLight);
            text.alignment = TextAnchor.MiddleLeft;
            var textRect = text.rectTransform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0, 0.5f);
            textRect.pivot = new Vector2(0, 0.5f);
            textRect.anchoredPosition = new Vector2(52, 0);

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.isOn = false;
            return toggle;
        }

        static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 pivot)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static void Bind(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"Alan bulunamadı: {target.GetType().Name}.{field}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BindArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite LoadArtSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogError($"Sprite yüklenemedi: {path}");
            return sprite;
        }

        static AudioClip LoadClip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{name}.ogg");
            if (clip == null) Debug.LogError($"Ses bulunamadı: {name}");
            return clip;
        }

        static Font GetFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
