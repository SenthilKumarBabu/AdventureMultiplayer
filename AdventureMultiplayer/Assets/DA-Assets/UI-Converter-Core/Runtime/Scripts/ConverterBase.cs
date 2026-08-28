#if UNITY_EDITOR
















using DA_Assets.Extensions;
using DA_Assets.UCC.Drawers;
using DA_Assets.Shared.MCP;
using System;
using UnityEngine;

#pragma warning disable CS0649

namespace DA_Assets.UCC
{
    [Serializable]
    [DisallowMultipleComponent]
    public abstract class ConverterBase : MonoBehaviour, IMcpInstance
    {
        public abstract IConvConfig Config { get; }
        public virtual ImportMode? ForcedImportMode => null;

        public ProjectImporter ProjectImporter = new ProjectImporter();
        public SettingsLinker Settings = new SettingsLinker();
        public FcuEvents Events = new FcuEvents();
        public PrefabCreator PrefabCreator = new PrefabCreator();
        public SnapshotSettings SnapshotSettings = new SnapshotSettings();
        public virtual IAuthProvider AuthProvider => null;
        public RequestSender RequestSender = new RequestSender();
        public AssetTools AssetTools = new AssetTools();
        public CurrentProject CurrentProject = new CurrentProject();

        public InspectorDrawer InspectorDrawer = new InspectorDrawer();
        public EditorEventHandlers EditorEventHandlers = new EditorEventHandlers();
        public EditorDelegateHolder EditorDelegateHolder = new EditorDelegateHolder();

        public CanvasDrawer CanvasDrawer = new CanvasDrawer();
        public ProjectCacher ProjectCacher = new ProjectCacher();
        public ProjectDownloader ProjectDownloader = new ProjectDownloader();

        public ScriptGenerator ScriptGenerator = new ScriptGenerator();
        public FolderCreator FolderCreator = new FolderCreator();
        public HashSetter HashGenerator = new HashSetter();
        public NameHumanizer NameHumanizer = new NameHumanizer();
        public FontDownloader FontDownloader = new FontDownloader();
        public FontLoader FontLoader = new FontLoader();
        public GraphicHelpers GraphicHelpers = new GraphicHelpers();
        public TagSetter TagSetter = new TagSetter();

        public NameSetter NameSetter = new NameSetter();
        public SyncHelpers SyncHelpers = new SyncHelpers();
        public TransformSetter TransformSetter = new TransformSetter();
        public LayoutUpdateDataCreator LayoutUpdateDataCreator = new LayoutUpdateDataCreator();

        public ImageTypeSetter ImageTypeSetter = new ImageTypeSetter();
        public SpriteProcessor SpriteProcessor = new SpriteProcessor();
        public SpriteGenerator SpriteGenerator = new SpriteGenerator();
        public SpriteColorizer SpriteColorizer = new SpriteColorizer();
        public SpritePathSetter SpritePathSetter = new SpritePathSetter();
        public SpriteDownloader SpriteDownloader = new SpriteDownloader();
        public SpriteSlicer SpriteSlicer = new SpriteSlicer();
        public SpriteDuplicateRemover SpriteDuplicateRemover = new SpriteDuplicateRemover();

#if NOVA_UI_EXISTS
        public NovaDrawer NovaDrawer = new NovaDrawer();
#endif
        public IUitkConverter UITK_Converter { get; set; }

        [SerializeField] string guid;
        public string Guid => guid.CreateShortGuid(out guid);
        public string McpInstanceId => Guid;
        public string McpDisplayName => name;

        private void OnValidate()
        {
            InitServices();
        }

        public void InitServices()
        {
            Settings.Init(this);
            if (ForcedImportMode.HasValue)
                Settings.MainSettings.ImportMode = ForcedImportMode.Value;

            ProjectImporter.Init(this);
            SnapshotSettings.Init(this);
            EditorEventHandlers.Init(this);
            Events.Init(this);
            PrefabCreator.Init(this);
            InspectorDrawer.Init(this);
            if (AuthProvider is FcuBase authBase)
                authBase.Init(this);
            EditorDelegateHolder.Init(this);
            RequestSender.Init(this);
            AssetTools.Init(this);
            CurrentProject.Init(this);

            CanvasDrawer.Init(this);
            ProjectCacher.Init(this);
            ProjectDownloader.Init(this);
            ImageTypeSetter.Init(this);
            ScriptGenerator.Init(this);
            FolderCreator.Init(this);
            HashGenerator.Init(this);
            NameHumanizer.Init(this);
            FontDownloader.Init(this);
            FontLoader.Init(this);
            GraphicHelpers.Init(this);
            TagSetter.Init(this);
            SpriteProcessor.Init(this);
            NameSetter.Init(this);
            SyncHelpers.Init(this);
            TransformSetter.Init(this);
            SpriteGenerator.Init(this);
            SpriteColorizer.Init(this);
            SpritePathSetter.Init(this);
            SpriteDownloader.Init(this);
            SpriteSlicer.Init(this);
            SpriteDuplicateRemover.Init(this);
            LayoutUpdateDataCreator.Init(this);

#if NOVA_UI_EXISTS
            NovaDrawer.Init(this);
#endif
#if UNITY_EDITOR && FCU_UITK_EXT_EXISTS
            if (UITK_Converter == null && Config?.UitkConverterScript != null)
            {
                var type = Config.UitkConverterScript.GetClass();
                UITK_Converter = (IUitkConverter)Activator.CreateInstance(type);
            }
            UITK_Converter?.Init(this);
#endif
        }

        public void Reset()
        {
            InitServices();
#if DABUTTON_EXISTS
            this.Settings.ButtonSettings.DAB_Settings.Reset();
#endif
#if UNITY_EDITOR


            UnityEditor.EditorApplication.delayCall += () => OnResetPerformed?.Invoke(this);
#endif
        }

#if UNITY_EDITOR

        public static event System.Action<ConverterBase> OnResetPerformed;
#endif
    }

    [Serializable]
    public abstract class FcuBase
    {
        [SerializeField]
        protected ConverterBase monoBeh;
        protected IConvConfig Config => monoBeh.Config;

        public virtual void Init(ConverterBase monoBeh)
        {
            if (this.monoBeh != null)
                return;

            this.monoBeh = monoBeh;
        }
    }
}
#endif