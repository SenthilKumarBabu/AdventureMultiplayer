using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace DA_Assets.DM.Tests
{
    public class DefineSyncServiceTests
    {
        private List<string> _originalDefines;
        private readonly List<string> _symbolsToReset = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _originalDefines = DefineSyncService.GetDefines();
            _symbolsToReset.Clear();
        }

        [Test]
        public void GetDefines_ReturnsNonNullList()
        {
            Assert.IsNotNull(DefineSyncService.GetDefines());
        }

        [Test]
        public void Apply_EnabledItem_AddsSymbol()
        {
            const string symbol = "DM_TEST_APPLY_ADD_98765";
            Track(symbol);

            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "define_add", "X, Y", symbol);
            item.IsEnabled = true;
            EditorUtility.SetDirty(item);

            DefineSyncService.Apply(new List<DependencyItem> { item });

            Assert.Contains(symbol, DefineSyncService.GetDefines());
        }

        [Test]
        public void Apply_DisabledItem_RemovesSymbol()
        {
            const string symbol = "DM_TEST_APPLY_REMOVE_98765";
            Track(symbol);

            List<string> defines = DefineSyncService.GetDefines();
            defines.Add(symbol);
            DefineSyncService.SetDefines(defines);

            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "define_remove", "X, Y", symbol);
            item.IsEnabled = false;
            EditorUtility.SetDirty(item);

            DefineSyncService.Apply(new List<DependencyItem> { item });

            CollectionAssert.DoesNotContain(DefineSyncService.GetDefines(), symbol);
        }

        [Test]
        public void Apply_PreservesForeignDefines()
        {
            const string foreign = "DM_TEST_FOREIGN_98765";
            const string unrelated = "DM_TEST_UNRELATED_98765";
            Track(foreign);
            Track(unrelated);

            List<string> defines = DefineSyncService.GetDefines();
            defines.Add(foreign);
            DefineSyncService.SetDefines(defines);

            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "define_preserve", "X, Y", unrelated);
            item.IsEnabled = false;
            EditorUtility.SetDirty(item);

            DefineSyncService.Apply(new List<DependencyItem> { item });

            Assert.Contains(foreign, DefineSyncService.GetDefines());
        }

        [Test]
        public void Apply_ExternalReadd_RemovesSymbolOnNextApply()
        {
            const string symbol = "DM_TEST_LOOPGUARD_98765";
            Track(symbol);

            List<string> defines = DefineSyncService.GetDefines();
            defines.Add(symbol);
            DefineSyncService.SetDefines(defines);

            DependencyItem item = TestHelper.CreatePrecompiledItem(
                "define_loopguard", "X, Y", symbol);
            item.IsEnabled = false;
            EditorUtility.SetDirty(item);

            DefineSyncService.Apply(new List<DependencyItem> { item });
            CollectionAssert.DoesNotContain(DefineSyncService.GetDefines(), symbol);

            List<string> resimulated = DefineSyncService.GetDefines();
            resimulated.Add(symbol);
            DefineSyncService.SetDefines(resimulated);

            DefineSyncService.Apply(new List<DependencyItem> { item });
            CollectionAssert.DoesNotContain(DefineSyncService.GetDefines(), symbol,
                "Symbol must be removed again because the dependency is still absent.");
        }

        [TearDown]
        public void TearDown()
        {
            DefineSyncService.SetDefines(_originalDefines);
            foreach (string s in _symbolsToReset)
                DefineSyncService.ClearLoopGuard(s);
            TestHelper.CleanupTempAssets();
        }

        private void Track(string symbol)
        {
            _symbolsToReset.Add(symbol);
            DefineSyncService.ClearLoopGuard(symbol);
        }
    }
}
