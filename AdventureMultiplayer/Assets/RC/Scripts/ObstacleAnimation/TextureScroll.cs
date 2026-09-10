using UnityEngine;

namespace ithappy.rc
{
    /// <summary>
    /// Scrolls a renderer's material texture offset every frame so a flat surface
    /// reads as moving — used for treadmill / conveyor belts. Uses a
    /// MaterialPropertyBlock so it never instantiates (and leaks) the shared
    /// material. Set <see cref="scrollSpeed"/> so the visual travel matches the
    /// direction the belt pushes the player.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class TextureScroll : MonoBehaviour
    {
        [Tooltip("UV units per second. X scrolls along the U axis, Y along V.")]
        public Vector2 scrollSpeed = new Vector2(0f, -0.6f);

        [Tooltip("Shader texture property to scroll. URP Lit / SimpleLit use _BaseMap.")]
        public string textureProperty = "_BaseMap";

        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;
        private int _stId;
        private Vector4 _baseST = new Vector4(1f, 1f, 0f, 0f);
        private Vector2 _offset;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();
            _stId = Shader.PropertyToID(textureProperty + "_ST");

            var mat = _renderer.sharedMaterial;
            if (mat != null && mat.HasProperty(_stId))
                _baseST = mat.GetVector(_stId);
        }

        private void Update()
        {
            _offset += scrollSpeed * Time.deltaTime;
            _offset.x %= 1f;
            _offset.y %= 1f;

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetVector(_stId, new Vector4(_baseST.x, _baseST.y,
                                             _baseST.z + _offset.x,
                                             _baseST.w + _offset.y));
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
