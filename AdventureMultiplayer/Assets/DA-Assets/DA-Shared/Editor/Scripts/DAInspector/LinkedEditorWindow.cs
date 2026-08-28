using UnityEditor;
using UnityEngine;
using DA_Assets.Singleton;
using System;

namespace DA_Assets.DAI
{
    public class LinkedEditorWindow<T1, T2, T3> : EditorWindow, IDAEditor
        where T1 : LinkedEditorWindow<T1, T2, T3>
        where T2 : Editor
        where T3 : MonoBehaviour
    {
        public static T1 GetInstance(T2 inspector, T3 monoBeh, Vector2 windowSize, bool fixedSize, string title)
        {
            T1[] windows = Resources.FindObjectsOfTypeAll<T1>();
            T1 instance = null;

            foreach (T1 window in windows)
            {
                if (window.monoBeh != null && window.monoBeh.GetInstanceID() == monoBeh.GetInstanceID())
                {
                    instance = window;
                    break;
                }
            }

            if (instance == null)
            {
                instance = CreateInstance<T1>();
                instance.titleContent = new GUIContent(title);
            }

            instance.Bind(inspector, monoBeh);

            instance.WindowSize = windowSize;
            instance.FixedSize = fixedSize;

            return instance;
        }

        [SerializeField] DAInspector _gui;
        public DAInspector gui { get => _gui; set => _gui = value; }

        [SerializeField] DAInspectorUITK _uitk;
        public DAInspectorUITK uitk { get => _uitk; set => _uitk = value; }

        protected T2 inspector;
        public T2 Inspector { get => inspector; set => inspector = value; }

        [SerializeField] protected T3 monoBeh;
        public T3 MonoBeh { get => monoBeh; set => monoBeh = value; }

        protected SerializedObject serializedObject;
        public SerializedObject SerializedObject { get => serializedObject; set => serializedObject = value; }

        protected InternalLocalizator localizator;

        private Vector2 windowSize = DAI_UitkConstants.DefaultWindowSize;
        public Vector2 WindowSize { get => windowSize; set => windowSize = value; }

        private bool fixedSize = false;
        public bool FixedSize { get => fixedSize; set => fixedSize = value; }

        protected virtual void OnEnable()
        {
            if (monoBeh != null)
            {
                if (serializedObject == null || serializedObject.targetObject != monoBeh)
                {
                    serializedObject = new SerializedObject(monoBeh);
                }

                OnTargetBound();
            }
        }

        protected virtual void OnDisable()
        {
            OnTargetUnbound();
        }

        protected void Bind(T2 newInspector, T3 newTarget)
        {
            bool targetChanged = monoBeh != newTarget;

            inspector = newInspector;
            monoBeh = newTarget;

            if (monoBeh != null)
            {
                if (serializedObject == null || serializedObject.targetObject != monoBeh)
                {
                    serializedObject = new SerializedObject(monoBeh);
                }
            }

            if (targetChanged)
            {
                OnTargetBound();
            }
        }

        protected void Unbind()
        {
            OnTargetUnbound();
            inspector = default;
            monoBeh = default;
            serializedObject = null;
            localizator = null;
        }

        protected virtual void OnTargetBound() { }
        protected virtual void OnTargetUnbound() { }

        public string Localize(Enum key, params object[] args) =>
            localizator.Init().GetLocalizedText(key, null, args);

        public new void Show()
        {
            Show(immediateDisplay: false);

            this.position = new Rect(
                (Screen.currentResolution.width - windowSize.x * 2) / 2,
                (Screen.currentResolution.height - windowSize.y * 2) / 2,
                windowSize.x,
                windowSize.y);

            if (fixedSize)
            {
                this.minSize = windowSize;
                this.maxSize = windowSize;
            }

            Focus();
        }
    }
}
