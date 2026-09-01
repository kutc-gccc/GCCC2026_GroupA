using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GCCC.BoardGame.Presentation.Views
{
    public sealed class RuntimeSpriteFactory : IDisposable
    {
        private Texture2D squareTexture;
        private Texture2D circleTexture;
        private Texture2D frameTexture;
        private Texture2D triangleTexture;

        public RuntimeSpriteFactory()
        {
            SquareSprite = CreateSquareSprite();
            CircleSprite = CreateCircleSprite();
            FrameSprite = CreateFrameSprite();
            TriangleSprite = CreateTriangleSprite();
        }

        public Sprite SquareSprite { get; }

        public Sprite CircleSprite { get; }

        /// <summary>中を塗らない正方形の枠。選択中・戦闘可能・合体候補の表示に使う。</summary>
        public Sprite FrameSprite { get; }

        /// <summary>上向きの三角形。下向きにするには縦方向のスケールを反転する。</summary>
        public Sprite TriangleSprite { get; }

        public void Dispose()
        {
            DestroyGeneratedObject(SquareSprite);
            DestroyGeneratedObject(CircleSprite);
            DestroyGeneratedObject(FrameSprite);
            DestroyGeneratedObject(TriangleSprite);
            DestroyGeneratedObject(squareTexture);
            DestroyGeneratedObject(circleTexture);
            DestroyGeneratedObject(frameTexture);
            DestroyGeneratedObject(triangleTexture);
            squareTexture = null;
            circleTexture = null;
            frameTexture = null;
            triangleTexture = null;
        }

        private Sprite CreateSquareSprite()
        {
            squareTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Board Square Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            squareTexture.SetPixel(0, 0, Color.white);
            squareTexture.Apply();

            Sprite sprite = Sprite.Create(squareTexture, new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f), 1f);
            sprite.name = "Board Square Sprite";
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private Sprite CreateCircleSprite()
        {
            const int resolution = 64;
            circleTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = "Board Piece Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Color[] pixels = new Color[resolution * resolution];
            Vector2 center = new Vector2((resolution - 1) * 0.5f, (resolution - 1) * 0.5f);
            float radius = resolution * 0.46f;
            for (int row = 0; row < resolution; row++)
            {
                for (int column = 0; column < resolution; column++)
                {
                    float distance = Vector2.Distance(new Vector2(column, row), center);
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[row * resolution + column] = new Color(1f, 1f, 1f, alpha);
                }
            }

            circleTexture.SetPixels(pixels);
            circleTexture.Apply();
            Sprite sprite = Sprite.Create(circleTexture,
                new Rect(0f, 0f, resolution, resolution),
                new Vector2(0.5f, 0.5f), resolution);
            sprite.name = "Board Piece Sprite";
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private Sprite CreateFrameSprite()
        {
            const int resolution = 64;
            // 枠の太さ。セルの一辺に対する比率で決める。
            const int thickness = 5;
            frameTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = "Board Frame Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Color[] pixels = new Color[resolution * resolution];
            for (int row = 0; row < resolution; row++)
            {
                for (int column = 0; column < resolution; column++)
                {
                    bool isEdge =
                        row < thickness ||
                        row >= resolution - thickness ||
                        column < thickness ||
                        column >= resolution - thickness;

                    pixels[row * resolution + column] =
                        new Color(1f, 1f, 1f, isEdge ? 1f : 0f);
                }
            }

            frameTexture.SetPixels(pixels);
            frameTexture.Apply();
            Sprite sprite = Sprite.Create(frameTexture,
                new Rect(0f, 0f, resolution, resolution),
                new Vector2(0.5f, 0.5f), resolution);
            sprite.name = "Board Frame Sprite";
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private Sprite CreateTriangleSprite()
        {
            const int resolution = 64;
            triangleTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = "Board Triangle Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Color[] pixels = new Color[resolution * resolution];
            float size = resolution - 1f;
            for (int row = 0; row < resolution; row++)
            {
                // 下辺から上の頂点へ向かって、行ごとに許容する幅を狭めていく。
                float heightRatio = row / size;
                float halfWidth = (1f - heightRatio) * size * 0.5f;
                float centerColumn = size * 0.5f;

                for (int column = 0; column < resolution; column++)
                {
                    float distance = Mathf.Abs(column - centerColumn);
                    float alpha = Mathf.Clamp01(halfWidth - distance + 0.5f);
                    pixels[row * resolution + column] = new Color(1f, 1f, 1f, alpha);
                }
            }

            triangleTexture.SetPixels(pixels);
            triangleTexture.Apply();
            Sprite sprite = Sprite.Create(triangleTexture,
                new Rect(0f, 0f, resolution, resolution),
                new Vector2(0.5f, 0.5f), resolution);
            sprite.name = "Board Triangle Sprite";
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private static void DestroyGeneratedObject(Object generatedObject)
        {
            if (generatedObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(generatedObject);
            }
            else
            {
                Object.DestroyImmediate(generatedObject);
            }
        }
    }
}
