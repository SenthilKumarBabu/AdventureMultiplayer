using System.Collections;
using System.IO;
using DA_Assets.DAI;
using DA_Assets.Singleton;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DA_Assets.UCC.Tests.Editor
{
    public sealed class ConverterWindowTests
    {
        [Test]
        public void FcuLocExtensions_Localize_DoesNotInitializeLocalizator()
        {
            string filePath = Path.Combine(
                Application.dataPath,
                "DA-Assets",
                "UI-Converter-Core",
                "Runtime",
                "Scripts",
                "Infrastructure",
                "Extensions",
                "FcuLocExtensions.cs");

            string source = File.ReadAllText(filePath);

            Assert.That(source, Does.Not.Contain(".Init().GetLocalizedText"));
        }

        [Test]
        public void FreshOpen_BindsTargetAndSerializedObject()
        {
            var target = CreateTarget();
            TestWindow window = null;

            try
            {
                window = TestWindow.GetInstance(null, target, Vector2.one, false, "Test");

                Assert.That(window.MonoBeh, Is.EqualTo(target));
                Assert.That(window.SerializedObject, Is.Not.Null);
                Assert.That(window.SerializedObject.targetObject, Is.EqualTo(target));
            }
            finally
            {
                CloseWindow(window);
                DestroyTarget(target);
            }
        }

        [Test]
        public void RepeatedGetInstance_WithSameTarget_ReturnsSameWindow()
        {
            var target = CreateTarget();
            TestWindow first = null;
            TestWindow second = null;

            try
            {
                first = TestWindow.GetInstance(null, target, Vector2.one, false, "Test");
                second = TestWindow.GetInstance(null, target, Vector2.one, false, "Test");

                Assert.That(second, Is.EqualTo(first));
            }
            finally
            {
                CloseWindow(first);
                if (second != first)
                {
                    CloseWindow(second);
                }
                DestroyTarget(target);
            }
        }

        [Test]
        public void Unbind_ClearsTargetState()
        {
            var target = CreateTarget();
            TestWindow window = null;

            try
            {
                window = TestWindow.GetInstance(null, target, Vector2.one, false, "Test");
                window.ForceUnbind();

                Assert.That(window.MonoBeh, Is.Null);
                Assert.That(window.SerializedObject, Is.Null);
                Assert.That(window.HasLocalizator, Is.False);
            }
            finally
            {
                CloseWindow(window);
                DestroyTarget(target);
            }
        }

        [Test]
        public void OnTargetBound_FiresOnBind()
        {
            var target = CreateTarget();
            TestWindow window = null;

            try
            {
                window = TestWindow.GetInstance(null, target, Vector2.one, false, "Test");

                Assert.That(window.BoundCount, Is.EqualTo(1));
            }
            finally
            {
                CloseWindow(window);
                DestroyTarget(target);
            }
        }

        [Test]
        public void OnTargetUnbound_FiresOnDisable()
        {
            var target = CreateTarget();
            TestWindow window = null;

            try
            {
                window = TestWindow.GetInstance(null, target, Vector2.one, false, "Test");
                window.InvokeDisable();

                Assert.That(window.UnboundCount, Is.EqualTo(1));
            }
            finally
            {
                CloseWindow(window);
                DestroyTarget(target);
            }
        }

        [Test]
        public void Localizator_CanBeAssignedOnTargetBound()
        {
            var target = CreateTarget();
            TestWindow window = null;

            try
            {
                window = TestWindow.GetInstance(null, target, Vector2.one, false, "Test");

                Assert.That(window.HasLocalizator, Is.True);
            }
            finally
            {
                CloseWindow(window);
                DestroyTarget(target);
            }
        }

        [Test]
        public void Localize_IsProvidedByBaseWindow()
        {
            var target = CreateTarget();
            TestWindow window = null;

            try
            {
                window = TestWindow.GetInstance(null, target, Vector2.one, false, "Test");

                Assert.That(window.Localize(TestLocKey.test_missing), Is.EqualTo(nameof(TestLocKey.test_missing)));
            }
            finally
            {
                CloseWindow(window);
                DestroyTarget(target);
            }
        }

        [UnityTest]
        public IEnumerator ResetFiltering_IgnoresForeignConverter()
        {
            var owner = CreateConverter();
            var other = CreateConverter();
            FcuSettingsWindow window = null;

            yield return null;
            yield return null;

            try
            {
                window = FcuSettingsWindow.GetInstance(null, owner, Vector2.one, false, "Test");
                window.rootVisualElement.Add(new Label("marker"));

                other.Reset();
                yield return null;
                yield return null;

                Assert.That(window.rootVisualElement.childCount, Is.EqualTo(1));
                Assert.That(((Label)window.rootVisualElement[0]).text, Is.EqualTo("marker"));
            }
            finally
            {
                CloseWindow(window);
                DestroyTarget(owner);
                DestroyTarget(other);
            }
        }

        [Test]
        public void DomainReloadReconcile_RecreatesSerializedObjectAndBinds()
        {
            var target = CreateTarget();
            TestWindow window = null;

            try
            {
                window = TestWindow.GetInstance(null, target, Vector2.one, false, "Test");
                window.SerializedObject = null;
                window.InvokeEnable();

                Assert.That(window.SerializedObject, Is.Not.Null);
                Assert.That(window.SerializedObject.targetObject, Is.EqualTo(target));
                Assert.That(window.BoundCount, Is.EqualTo(2));
            }
            finally
            {
                CloseWindow(window);
                DestroyTarget(target);
            }
        }

        private static TestTarget CreateTarget()
        {
            return new GameObject("LinkedEditorWindowTestTarget").AddComponent<TestTarget>();
        }

        private static TestConverter CreateConverter()
        {
            return new GameObject("FcuSettingsWindowTestTarget").AddComponent<TestConverter>();
        }

        private static void DestroyTarget(MonoBehaviour target)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target.gameObject);
            }
        }

        private static void CloseWindow(EditorWindow window)
        {
            if (window != null)
            {
                Object.DestroyImmediate(window);
            }
        }

        private sealed class TestWindow : LinkedEditorWindow<TestWindow, UnityEditor.Editor, TestTarget>
        {
            public int BoundCount { get; private set; }
            public int UnboundCount { get; private set; }
            public bool HasLocalizator => localizator != null;

            public void ForceUnbind() => Unbind();
            public void InvokeEnable() => OnEnable();
            public void InvokeDisable() => OnDisable();

            protected override void OnTargetBound()
            {
                BoundCount++;
                localizator = new InternalLocalizator();
            }

            protected override void OnTargetUnbound()
            {
                UnboundCount++;
            }
        }

        private sealed class TestTarget : MonoBehaviour
        {
        }

        private sealed class TestConverter : ConverterBase
        {
            public override IConvConfig Config => FcuConfig.Instance;
        }

        private enum TestLocKey
        {
            test_missing
        }
    }
}
