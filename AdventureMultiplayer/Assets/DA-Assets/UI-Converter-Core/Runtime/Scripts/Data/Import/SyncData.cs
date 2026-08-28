#if UNITY_EDITOR
using DA_Assets.Extensions;
using DA_Assets.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

#pragma warning disable IDE0052

namespace DA_Assets.UCC.Model
{
    [Serializable]
    public class SyncData : IHaveId
    {

        [SerializeField] string id;
        public string Id { get => id; set => id = value; }

        [SerializeField] string projectId;
        public string ProjectId { get => projectId; set => projectId = value; }

        [SerializeField] FNames names;
        public FNames Names { get => names; set => names = value; }

        [Space]


        [SerializeField] GameObject gameObject;
        public GameObject GameObject { get => gameObject; set => gameObject = value; }

        [SerializeField] ConverterBase fcu;
        public ConverterBase ConverterBase
        {
            get
            {
                if (gameObject == null)
                    return fcu;

                return gameObject.GetComponentInParent<ConverterBase>(true);
            }
            set => fcu = value;
        }

        [Space]


        [SerializeField] GameObject rootFrameGO;
        [SerializeField] SyncData rootFrameSD;
        public SyncData RootFrame
        {
            get
            {
                if (rootFrameGO == null)
                    return rootFrameSD;

                SyncHelper sh = rootFrameGO.GetComponent<SyncHelper>();

                if (sh == null || sh.Data == null)
                    return rootFrameSD;
                else
                    return sh.Data;
            }
            set
            {
                if (value?.GameObject != null)
                    rootFrameGO = value.GameObject;

                if (rootFrameGO == null)
                {
                    rootFrameSD = value;
                    return;
                }

                SyncHelper sh = rootFrameGO.GetComponent<SyncHelper>();

                if (sh != null)
                    sh.Data = value;
                else
                    rootFrameSD = value;
            }
        }

        [Clear] public Node Parent { get; set; }

        [SerializeField, Clear] int parentIndex;
        public int ParentIndex { get => parentIndex; set => parentIndex = value; }

        [SerializeField] List<int> childIndexes = new List<int>();
        public List<int> ChildIndexes { get => childIndexes; set => childIndexes = value; }

        [SerializeField, HideInInspector] List<FcuHierarchy> hierarchy = new List<FcuHierarchy>();
        public List<FcuHierarchy> Hierarchy { get => hierarchy; set => hierarchy = value; }

        public string NameHierarchy
        {
            get
            {
                if (hierarchy.IsEmpty())
                    return null;

                string h = string.Join(FcuConfig.HierarchyDelimiter.ToString(), hierarchy.Select(x => x.Name));
                return h;
            }
        }

        public int HierarchyLevel { get; set; }
        public int SiblingIndex { get; set; }

        [Space]


        [SerializeField] List<FcuTag> tags = new List<FcuTag>();
        public List<FcuTag> Tags { get => tags; set => tags = value; }

        [SerializeField, Clear] List<ReasonEntry> reasons = new List<ReasonEntry>();
        public List<ReasonEntry> Reasons { get => reasons; set => reasons = value; }

        [SerializeField, Clear] SerializedDictionary<ReasonKey, string> reasonArgs = new SerializedDictionary<ReasonKey, string>();
        public SerializedDictionary<ReasonKey, string> ReasonArgs { get => reasonArgs; set => reasonArgs = value; }



        [SerializeField] FcuImageType fcuImageType;
        public FcuImageType FcuImageType { get => fcuImageType; set => fcuImageType = value; }

        [SerializeField] ButtonComponent buttonComponent;
        public ButtonComponent ButtonComponent { get => buttonComponent; set => buttonComponent = value; }

        [Space]


        [SerializeField, Clear] FRect rect;
        public FRect FRect { get => rect; set => rect = value; }
        [Clear] public bool HasCachedGlobalRect { get; set; }
        [Clear] public bool HasTransformComputationCache { get; set; }
        [Clear] public bool ManualWhiteColor { get; set; }
        [Clear] public float CachedMatrixAngle { get; set; }
        [Clear] public float CachedFigmaRotationAngle { get; set; }
        [Clear] public float CachedAbsoluteMatrixAngle { get; set; }
        [Clear] public float CachedAbsoluteFigmaRotationAngle { get; set; }
        [Clear] public bool CachedHasRotatedAncestor { get; set; }

        [SerializeField, Clear] UguiTransformData uguiTransformData;
        public UguiTransformData UguiTransformData { get => uguiTransformData; set => uguiTransformData = value; }

#if NOVA_UI_EXISTS
        [SerializeField, Clear] NovaTransformData novaTransformData;
        public NovaTransformData NovaTransformData { get => novaTransformData; set => novaTransformData = value; }
#endif

        [SerializeField, Clear] Transform parentTransform;
        public Transform ParentTransform { get => parentTransform; set => parentTransform = value; }

        [SerializeField, Clear] Transform parentTransformRect;
        public Transform ParentTransformRect { get => parentTransformRect; set => parentTransformRect = value; }

        [Space]


        [SerializeField, Clear] FGraphic graphic;
        public FGraphic Graphic { get => graphic; set => graphic = value; }

        [SerializeField, Clear] string spritePath;
        public string SpritePath { get => spritePath; set => spritePath = value; }

        [SerializeField, Clear] Vector2Int spriteSize;
        public Vector2Int SpriteSize { get => spriteSize; set => spriteSize = value; }

        [SerializeField, Clear] Vector2 maxSpriteSize;
        public Vector2 MaxSpriteSize { get => maxSpriteSize; set => maxSpriteSize = value; }

        [SerializeField, Clear] ImageFormat imageFormat;
        public ImageFormat ImageFormat { get => imageFormat; set => imageFormat = value; }

        [SerializeField, Clear] float scale;
        public float Scale { get => scale; set => scale = value; }

        [SerializeField, Clear] string link;
        public string Link { get => link; set => link = value; }

        [Space]


        [SerializeField] bool needDownload;
        public bool NeedDownload { get => needDownload; set => needDownload = value; }

        [SerializeField, Clear] bool needGenerate;
        public bool NeedGenerate { get => needGenerate; set => needGenerate = value; }

        [SerializeField, Clear] bool useImageLinearMaterial;
        public bool UseImageLinearMaterial { get => useImageLinearMaterial; set => useImageLinearMaterial = value; }

        [SerializeField] bool forceImage;
        public bool ForceImage { get => forceImage; set => forceImage = value; }

        [SerializeField] bool forceContainer;
        public bool ForceContainer { get => forceContainer; set => forceContainer = value; }

        [SerializeField] bool isMutual;
        public bool IsMutual { get => isMutual; set => isMutual = value; }

        [SerializeField] bool isEmpty;
        public bool IsEmpty { get => isEmpty; set => isEmpty = value; }

        [SerializeField] bool isOverlappedByStroke;
        public bool IsOverlappedByStroke { get => isOverlappedByStroke; set => isOverlappedByStroke = value; }

        [SerializeField] bool isInsideDownloadable;
        public bool InsideDownloadable { get => isInsideDownloadable; set => isInsideDownloadable = value; }

        [SerializeField, Clear] bool hasFontAsset;
        public bool HasFontAsset { get => hasFontAsset; set => hasFontAsset = value; }

        [Space]


        [SerializeField] int objectHash;
        public int ObjectHash { get => objectHash; set => objectHash = value; }

        [SerializeField, Clear] int renderHash;
        public int RenderHash { get => renderHash; set => renderHash = value; }




        [SerializeField, Clear] int downloadAttempsCount;
        public int DownloadAttempsCount { get => downloadAttempsCount; set => downloadAttempsCount = value; }

        [Clear] public string UitkType { get; set; }
        [Clear] public XmlElement XmlElement { get; set; }

#if UNITY_2021_3_OR_NEWER
        public UnityEngine.UIElements.UIDocument UIDocument { get; set; }
#endif

        [SerializeField, Clear] GameObject rectGameObject;
        public GameObject RectGameObject { get => rectGameObject; set => rectGameObject = value; }
    }
}
#endif
