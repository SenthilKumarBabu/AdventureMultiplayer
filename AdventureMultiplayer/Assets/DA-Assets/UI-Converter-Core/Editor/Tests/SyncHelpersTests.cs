#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.UCC.Tests.Editor
{
    public sealed class SyncHelpersTests
    {
        private GameObject firstRoot;
        private GameObject secondRoot;

        [TearDown]
        public void TearDown()
        {
            Selection.activeObject = null;

            if (secondRoot != null)
                UnityEngine.Object.DestroyImmediate(secondRoot);

            if (firstRoot != null)
                UnityEngine.Object.DestroyImmediate(firstRoot);
        }

        [Test]
        public void ConverterReferenceField_UsesConverterBaseType()
        {
            FieldInfo field = typeof(SyncData).GetField("fcu", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            Assert.That(field.FieldType, Is.EqualTo(typeof(ConverterBase)));
        }

        [TestCase("DA_Assets.FCU.FigmaConverterUnity, DA_Assets.FCU")]
        [TestCase("DA_Assets.MGOCU.MasterGoConverterUnity, DA_Assets.MGOCU")]
        public void ConverterReferenceField_SerializesConcreteConverter(string assemblyQualifiedTypeName)
        {
            Type converterType = Type.GetType(assemblyQualifiedTypeName);
            if (converterType == null)
                Assert.Ignore($"{assemblyQualifiedTypeName} is not installed.");

            GameObject root = new GameObject(converterType.Name);
            firstRoot = root;
            ConverterBase converter = (ConverterBase)root.AddComponent(converterType);
            SyncHelper syncHelper = CreateSyncHelper(root.transform, "Child");
            syncHelper.Data.ConverterBase = converter;

            SerializedObject serializedObject = new SerializedObject(syncHelper);
            SerializedProperty converterProperty = serializedObject.FindProperty("data.fcu");

            Assert.That(converterProperty, Is.Not.Null);
            Assert.That(converterProperty.objectReferenceValue, Is.SameAs(converter));
        }

        [Test]
        public void ConverterBase_MissingReference_ResolvesNearestHierarchyConverter()
        {
            SyncHelpersTestConverter converter = CreateConverter("First");
            SyncHelper syncHelper = CreateSyncHelper(converter.transform, "Child");

            Assert.That(syncHelper.Data.ConverterBase, Is.SameAs(converter));
        }

        [Test]
        public void ConverterBase_StaleReference_UsesNearestHierarchyConverter()
        {
            SyncHelpersTestConverter firstConverter = CreateConverter("First");
            SyncHelpersTestConverter secondConverter = CreateConverter("Second");
            SyncHelper syncHelper = CreateSyncHelper(firstConverter.transform, "Child");
            syncHelper.Data.ConverterBase = secondConverter;

            Assert.That(syncHelper.Data.ConverterBase, Is.SameAs(firstConverter));
        }

        [Test]
        public void ConverterBase_OuterAncestorReference_UsesNearestNestedConverter()
        {
            SyncHelpersTestConverter outerConverter = CreateConverter("Outer");
            SyncHelpersTestConverter nestedConverter = CreateConverter("Nested", outerConverter.transform);
            SyncHelper syncHelper = CreateSyncHelper(nestedConverter.transform, "Child");
            syncHelper.Data.ConverterBase = outerConverter;

            Assert.That(syncHelper.Data.ConverterBase, Is.SameAs(nestedConverter));
        }

        [Test]
        public void GetAllSyncHelpers_NestedConverter_ExcludesNestedHierarchy()
        {
            SyncHelpersTestConverter firstConverter = CreateConverter("First");
            SyncHelper firstHelper = CreateSyncHelper(firstConverter.transform, "FirstChild");
            SyncHelpersTestConverter nestedConverter = CreateConverter("Nested", firstConverter.transform);
            CreateSyncHelper(nestedConverter.transform, "NestedChild");

            SyncHelper[] result = firstConverter.SyncHelpers.GetAllSyncHelpers();

            Assert.That(result, Is.EqualTo(new[] { firstHelper }));
        }

        [Test]
        public void SetFcuToAllChilds_NestedConverter_ExcludesNestedHierarchy()
        {
            SyncHelpersTestConverter outerConverter = CreateConverter("Outer");
            SyncHelper outerHelper = CreateSyncHelper(outerConverter.transform, "OuterChild");
            SyncHelpersTestConverter nestedConverter = CreateConverter("Nested", outerConverter.transform);
            SyncHelper nestedHelper = CreateSyncHelper(nestedConverter.transform, "NestedChild");
            nestedHelper.Data.ConverterBase = nestedConverter;
            int count = 0;

            outerConverter.SyncHelpers.SetFcuToAllChilds(
                outerConverter.gameObject,
                ref count,
                CancellationToken.None);

            FieldInfo field = typeof(SyncData).GetField("fcu", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(field.GetValue(outerHelper.Data), Is.SameAs(outerConverter));
            Assert.That(field.GetValue(nestedHelper.Data), Is.SameAs(nestedConverter));
        }

        [Test]
        public void TryGetSelectedConverter_SelectedDescendant_ReturnsNearestConverter()
        {
            SyncHelpersTestConverter converter = CreateConverter("First");
            SyncHelper syncHelper = CreateSyncHelper(converter.transform, "Child");
            Selection.activeGameObject = syncHelper.gameObject;

            Type contextMenuType = Type.GetType("DA_Assets.UCC.ContextMenuItems, DA_Assets.UCC.Editor");
            Assert.That(contextMenuType, Is.Not.Null);

            MethodInfo method = contextMenuType.GetMethod(
                "TryGetSelectedConverter",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);

            object[] arguments = { null };
            bool found = (bool)method.Invoke(null, arguments);

            Assert.That(found, Is.True);
            Assert.That(arguments[0], Is.SameAs(converter));
        }

        private SyncHelpersTestConverter CreateConverter(string name, Transform parent = null)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);

            if (firstRoot == null)
                firstRoot = gameObject;
            else if (parent == null)
                secondRoot = gameObject;

            SyncHelpersTestConverter converter = gameObject.AddComponent<SyncHelpersTestConverter>();
            converter.InitServices();
            return converter;
        }

        private static SyncHelper CreateSyncHelper(Transform parent, string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);

            SyncHelper syncHelper = gameObject.AddComponent<SyncHelper>();
            syncHelper.Data = new SyncData
            {
                GameObject = gameObject
            };
            return syncHelper;
        }

        private sealed class SyncHelpersTestConverter : ConverterBase
        {
            public override IConvConfig Config => FcuConfig.Instance;
        }
    }
}
#endif
