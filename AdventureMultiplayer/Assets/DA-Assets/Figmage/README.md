# Figmage

Figmage is a Unity UI image component and reusable baker for Figma-style fills, strokes, and effects.

Current scope:
- Isolated runtime model independent from Figma-Converter-for-Unity.
- Shader-backed PNG baker copied from the FCU sprite generation path.
- Initial EditMode smoke tests for solid fills and linear gradients.

Development follows small TDD loops: define the contract in tests, implement the minimum code that satisfies it, then refactor only after the tests are green.
