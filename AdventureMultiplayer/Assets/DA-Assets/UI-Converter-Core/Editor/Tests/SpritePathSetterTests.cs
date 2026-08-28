using DA_Assets.UCC.Model;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DA_Assets.UCC.Tests.Editor
{
    public sealed class SpritePathSetterTests
    {
        [Test]
        public void IsExistingSpriteSizeValid_GenerativeRejectsLargerCanvas()
        {
            bool valid = SpritePathSetter.IsExistingSpriteSizeValid(
                FcuImageType.Generative,
                new Vector2Int(590, 1084),
                new Vector2Int(466, 960));

            Assert.That(valid, Is.False);
        }

        [Test]
        public void IsExistingSpriteSizeValid_DownloadableAcceptsLargerCanvas()
        {
            bool valid = SpritePathSetter.IsExistingSpriteSizeValid(
                FcuImageType.Downloadable,
                new Vector2Int(590, 1084),
                new Vector2Int(466, 960));

            Assert.That(valid, Is.True);
        }

        [Test]
        public void Build_GenerativeNodesWithSameRenderHashAndDifferentSourceSizeUseDifferentRenderKeys()
        {
            Node compactNode = CreateSpriteNode(
                "9:616",
                FcuImageType.Generative,
                1213702458,
                new Vector2(464, 960),
                new Vector2(464, 960));

            Node expandedNode = CreateSpriteNode(
                "9:443",
                FcuImageType.Generative,
                1213702458,
                new Vector2(464, 960),
                new Vector2(570, 1091));

            SpriteIdentityCache cache = SpriteIdentityCacheBuilder.Build(
                new List<Node> { compactNode, expandedNode });

            int compactKey = cache.GetRenderKey(compactNode);
            int expandedKey = cache.GetRenderKey(expandedNode);

            Assert.That(compactKey, Is.Not.EqualTo(expandedKey));
            Assert.That(cache.GetGroup(compactKey), Has.Count.EqualTo(1));
            Assert.That(cache.GetGroup(expandedKey), Has.Count.EqualTo(1));
            Assert.That(cache.UniqueRepresentatives, Has.Count.EqualTo(2));
        }

        [Test]
        public void Build_DownloadableNodesWithSameRenderHashAndDifferentSourceSizeShareRenderKey()
        {
            Node compactNode = CreateSpriteNode(
                "downloadable-small",
                FcuImageType.Downloadable,
                1213702458,
                new Vector2(464, 960),
                new Vector2(464, 960));

            Node expandedNode = CreateSpriteNode(
                "downloadable-large",
                FcuImageType.Downloadable,
                1213702458,
                new Vector2(464, 960),
                new Vector2(570, 1091));

            SpriteIdentityCache cache = SpriteIdentityCacheBuilder.Build(
                new List<Node> { compactNode, expandedNode });

            int compactKey = cache.GetRenderKey(compactNode);
            int expandedKey = cache.GetRenderKey(expandedNode);

            Assert.That(compactKey, Is.EqualTo(expandedKey));
            Assert.That(cache.GetGroup(compactKey), Has.Count.EqualTo(2));
            Assert.That(cache.UniqueRepresentatives, Has.Count.EqualTo(1));
        }

        private static Node CreateSpriteNode(
            string id,
            FcuImageType imageType,
            int renderHash,
            Vector2 size,
            Vector2 renderSize)
        {
            return new Node
            {
                Id = id,
                Type = NodeType.FRAME,
                Size = size,
                AbsoluteBoundingBox = new BoundingBox
                {
                    Width = size.x,
                    Height = size.y
                },
                AbsoluteRenderBounds = new BoundingBox
                {
                    Width = renderSize.x,
                    Height = renderSize.y
                },
                Data = new SyncData
                {
                    FcuImageType = imageType,
                    RenderHash = renderHash,
                    Graphic = new FGraphic()
                }
            };
        }
    }
}