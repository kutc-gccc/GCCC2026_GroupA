using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GCCC.BoardGame.Presentation.Views
{
    public sealed class RuntimeSpriteFactory : IDisposable
    {
        private Texture2D squareTexture;
        private Texture2D circleTexture;

        public RuntimeSpriteFactory()
        {
            SquareSprite = CreateSquareSprite();
            CircleSprite = CreateCircleSprite();
        }

        public Sprite SquareSprite { get; }

        public Sprite CircleSprite { get; }

        public void Dispose()
        {
            DestroyGeneratedObject(SquareSprite);
            DestroyGeneratedObject(CircleSprite);
            DestroyGeneratedObject(squareTexture);
            DestroyGeneratedObject(circleTexture);
            squareTexture = null;
            circleTexture = null;
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
