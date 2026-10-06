using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModelViewerApp
{
    public enum IconType
    {
        CloudUpload,
        Orbit,
        Pan,
        Zoom,
        Focus,
        Fit,
        Reset,
        Checkmark,
        Settings,
        User,
        MoreDots,
        Comment,
        ChevronDown,
        AxisGizmo
    }

    public static class UIProceduralTheme
    {
        private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        // Palette matching reference image
        public static readonly Color BgDark = new Color(0.043f, 0.059f, 0.094f, 1f);       // #0b0f18
        public static readonly Color SidebarBg = new Color(0.067f, 0.094f, 0.145f, 1f);    // #111825
        public static readonly Color CardBg = new Color(0.086f, 0.118f, 0.18f, 1f);        // #161e2e
        public static readonly Color CardSelected = new Color(0.08f, 0.15f, 0.28f, 1f);    // #142647
        public static readonly Color CardBorder = new Color(0.145f, 0.2f, 0.302f, 1f);     // #25334d
        public static readonly Color CardBorderSelected = new Color(0.23f, 0.51f, 0.98f, 1f); // #3b82f6
        public static readonly Color BluePrimary = new Color(0.145f, 0.42f, 0.94f, 1f);    // #256be7
        public static readonly Color BlueHover = new Color(0.23f, 0.51f, 0.98f, 1f);       // #3b82f6
        public static readonly Color GreenReady = new Color(0.063f, 0.725f, 0.506f, 1f);   // #10b981
        public static readonly Color GreenReadyBg = new Color(0.035f, 0.16f, 0.12f, 0.95f); // #09291f
        public static readonly Color OrangeWarning = new Color(0.96f, 0.62f, 0.043f, 1f);  // #f59e0b
        public static readonly Color TextWhite = new Color(0.97f, 0.98f, 0.99f, 1f);
        public static readonly Color TextMuted = new Color(0.58f, 0.64f, 0.72f, 1f);       // #94a3b8
        public static readonly Color TextSubtle = new Color(0.42f, 0.47f, 0.55f, 1f);

        /// <summary>
        /// Mathematically exact Inigo Quilez 2D Rounded Box SDF with anti-aliasing and 9-slice support.
        /// </summary>
        public static Sprite GetRoundedRectSprite(int width = 64, int height = 64, int cornerRadius = 12, Color? fillColor = null, Color? borderColor = null, int borderWidth = 0)
        {
            Color fill = fillColor ?? Color.white;
            Color border = borderColor ?? Color.clear;
            string key = $"rr_{width}_{height}_{cornerRadius}_{fill}_{border}_{borderWidth}";

            if (spriteCache.TryGetValue(key, out Sprite s) && s != null)
                return s;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[width * height];
            float r = cornerRadius;
            float bw = borderWidth;
            float halfW = width * 0.5f;
            float halfH = height * 0.5f;
            Vector2 boxInner = new Vector2(halfW - r, halfH - r);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(Mathf.Abs(x + 0.5f - halfW), Mathf.Abs(y + 0.5f - halfH));
                    Vector2 q = p - boxInner;
                    float exteriorDist = Mathf.Sqrt(Mathf.Max(0f, q.x) * Mathf.Max(0f, q.x) + Mathf.Max(0f, q.y) * Mathf.Max(0f, q.y));
                    float interiorDist = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
                    float dist = exteriorDist + interiorDist - r;

                    float alpha = Mathf.Clamp01(-dist + 0.5f);

                    if (alpha <= 0.001f)
                    {
                        pixels[y * width + x] = Color.clear;
                    }
                    else if (borderWidth > 0 && dist > -borderWidth)
                    {
                        Color c = border;
                        c.a *= alpha;
                        pixels[y * width + x] = c;
                    }
                    else
                    {
                        Color c = fill;
                        c.a *= alpha;
                        pixels[y * width + x] = c;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            int sliceBorder = Mathf.Min(cornerRadius + 2, Mathf.Min(width / 2 - 1, height / 2 - 1));
            Vector4 borderVector = new Vector4(sliceBorder, sliceBorder, sliceBorder, sliceBorder);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borderVector);
            spriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite GetCircleSprite(int size = 64, Color? color = null)
        {
            Color c = color ?? Color.white;
            string key = $"circle_{size}_{c}";

            if (spriteCache.TryGetValue(key, out Sprite s) && s != null)
                return s;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float r = size * 0.5f;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    float alpha = Mathf.Clamp01(r - dist + 0.5f);
                    Color pixelColor = c;
                    pixelColor.a *= alpha;
                    pixels[y * size + x] = pixelColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            spriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite GetIconSprite(IconType type, Color? color = null, int size = 64)
        {
            Color c = color ?? Color.white;
            string key = $"icon_{type}_{c}_{size}";

            if (spriteCache.TryGetValue(key, out Sprite s) && s != null)
                return s;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            float sz = size;
            float stroke = Mathf.Max(2.2f, sz * 0.07f);

            switch (type)
            {
                case IconType.Checkmark:
                    Vector2 p1 = new Vector2(sz * 0.24f, sz * 0.48f);
                    Vector2 p2 = new Vector2(sz * 0.44f, sz * 0.28f);
                    Vector2 p3 = new Vector2(sz * 0.76f, sz * 0.72f);
                    DrawSegment(pixels, size, p1, p2, stroke * 1.25f, c);
                    DrawSegment(pixels, size, p2, p3, stroke * 1.25f, c);
                    break;

                case IconType.CloudUpload:
                    // Crisp cloud outline + up arrow
                    DrawFilledCircle(pixels, size, new Vector2(sz * 0.44f, sz * 0.58f), sz * 0.16f, c);
                    DrawFilledCircle(pixels, size, new Vector2(sz * 0.60f, sz * 0.54f), sz * 0.13f, c);
                    DrawFilledCircle(pixels, size, new Vector2(sz * 0.30f, sz * 0.46f), sz * 0.11f, c);
                    DrawFilledCircle(pixels, size, new Vector2(sz * 0.70f, sz * 0.46f), sz * 0.11f, c);
                    DrawFilledBox(pixels, size, new Vector2(sz * 0.50f, sz * 0.44f), new Vector2(sz * 0.20f, sz * 0.08f), c);
                    // Up arrow cut out
                    Color arrCol = Color.white;
                    Vector2 aTip = new Vector2(sz * 0.50f, sz * 0.68f);
                    Vector2 aBottom = new Vector2(sz * 0.50f, sz * 0.34f);
                    Vector2 aLeft = new Vector2(sz * 0.38f, sz * 0.52f);
                    Vector2 aRight = new Vector2(sz * 0.62f, sz * 0.52f);
                    DrawSegment(pixels, size, aTip, aBottom, stroke * 1.3f, arrCol);
                    DrawSegment(pixels, size, aTip, aLeft, stroke * 1.3f, arrCol);
                    DrawSegment(pixels, size, aTip, aRight, stroke * 1.3f, arrCol);
                    break;

                case IconType.Orbit:
                    Vector2 orbCenter = new Vector2(sz * 0.5f, sz * 0.5f);
                    DrawCircleRing(pixels, size, orbCenter, sz * 0.32f, stroke * 1.1f, c);
                    DrawFilledCircle(pixels, size, orbCenter, sz * 0.09f, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.5f, sz * 0.08f), new Vector2(sz * 0.5f, sz * 0.22f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.5f, sz * 0.78f), new Vector2(sz * 0.5f, sz * 0.92f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.08f, sz * 0.5f), new Vector2(sz * 0.22f, sz * 0.5f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.78f, sz * 0.5f), new Vector2(sz * 0.92f, sz * 0.5f), stroke, c);
                    break;

                case IconType.Pan:
                    Vector2 panCenter = new Vector2(sz * 0.5f, sz * 0.5f);
                    DrawSegment(pixels, size, new Vector2(sz * 0.5f, sz * 0.22f), new Vector2(sz * 0.5f, sz * 0.78f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.22f, sz * 0.5f), new Vector2(sz * 0.78f, sz * 0.5f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.5f, sz * 0.80f), new Vector2(sz * 0.40f, sz * 0.68f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.5f, sz * 0.80f), new Vector2(sz * 0.60f, sz * 0.68f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.5f, sz * 0.20f), new Vector2(sz * 0.40f, sz * 0.32f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.5f, sz * 0.20f), new Vector2(sz * 0.60f, sz * 0.32f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.20f, sz * 0.5f), new Vector2(sz * 0.32f, sz * 0.40f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.20f, sz * 0.5f), new Vector2(sz * 0.32f, sz * 0.60f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.80f, sz * 0.5f), new Vector2(sz * 0.68f, sz * 0.40f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.80f, sz * 0.5f), new Vector2(sz * 0.68f, sz * 0.60f), stroke, c);
                    break;

                case IconType.Zoom:
                    Vector2 lensCenter = new Vector2(sz * 0.42f, sz * 0.58f);
                    DrawCircleRing(pixels, size, lensCenter, sz * 0.22f, stroke * 1.2f, c);
                    Vector2 hStart = new Vector2(sz * 0.58f, sz * 0.42f);
                    Vector2 hEnd = new Vector2(sz * 0.80f, sz * 0.20f);
                    DrawSegment(pixels, size, hStart, hEnd, stroke * 1.4f, c);
                    break;

                case IconType.Focus:
                    DrawSegment(pixels, size, new Vector2(sz * 0.22f, sz * 0.78f), new Vector2(sz * 0.38f, sz * 0.78f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.22f, sz * 0.78f), new Vector2(sz * 0.22f, sz * 0.62f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.78f, sz * 0.78f), new Vector2(sz * 0.62f, sz * 0.78f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.78f, sz * 0.78f), new Vector2(sz * 0.78f, sz * 0.62f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.22f, sz * 0.22f), new Vector2(sz * 0.38f, sz * 0.22f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.22f, sz * 0.22f), new Vector2(sz * 0.22f, sz * 0.38f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.78f, sz * 0.22f), new Vector2(sz * 0.62f, sz * 0.22f), stroke, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.78f, sz * 0.22f), new Vector2(sz * 0.78f, sz * 0.38f), stroke, c);
                    DrawFilledCircle(pixels, size, new Vector2(sz * 0.5f, sz * 0.5f), sz * 0.08f, c);
                    break;

                case IconType.Fit:
                    DrawSegment(pixels, size, new Vector2(sz * 0.20f, sz * 0.80f), new Vector2(sz * 0.40f, sz * 0.80f), stroke * 1.1f, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.20f, sz * 0.80f), new Vector2(sz * 0.20f, sz * 0.60f), stroke * 1.1f, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.80f, sz * 0.80f), new Vector2(sz * 0.60f, sz * 0.80f), stroke * 1.1f, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.80f, sz * 0.80f), new Vector2(sz * 0.80f, sz * 0.60f), stroke * 1.1f, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.20f, sz * 0.20f), new Vector2(sz * 0.40f, sz * 0.20f), stroke * 1.1f, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.20f, sz * 0.20f), new Vector2(sz * 0.20f, sz * 0.40f), stroke * 1.1f, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.80f, sz * 0.20f), new Vector2(sz * 0.60f, sz * 0.20f), stroke * 1.1f, c);
                    DrawSegment(pixels, size, new Vector2(sz * 0.80f, sz * 0.20f), new Vector2(sz * 0.80f, sz * 0.40f), stroke * 1.1f, c);
                    break;

                case IconType.Reset:
                    Vector2 resCenter = new Vector2(sz * 0.5f, sz * 0.5f);
                    DrawArcRing(pixels, size, resCenter, sz * 0.26f, stroke * 1.1f, -40f, 240f, c);
                    float rad = 240f * Mathf.Deg2Rad;
                    Vector2 arcEnd = resCenter + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * (sz * 0.26f);
                    DrawSegment(pixels, size, arcEnd, arcEnd + new Vector2(-sz * 0.12f, 0), stroke * 1.1f, c);
                    DrawSegment(pixels, size, arcEnd, arcEnd + new Vector2(0, sz * 0.12f), stroke * 1.1f, c);
                    break;

                case IconType.Settings:
                    Vector2 setCenter = new Vector2(sz * 0.5f, sz * 0.5f);
                    DrawCircleRing(pixels, size, setCenter, sz * 0.24f, sz * 0.10f, c);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = (i * 60f) * Mathf.Deg2Rad;
                        Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        Vector2 tInner = setCenter + d * (sz * 0.20f);
                        Vector2 tOuter = setCenter + d * (sz * 0.38f);
                        DrawSegment(pixels, size, tInner, tOuter, stroke * 1.8f, c);
                    }
                    break;

                case IconType.User:
                    Vector2 headCenter = new Vector2(sz * 0.5f, sz * 0.65f);
                    DrawFilledCircle(pixels, size, headCenter, sz * 0.16f, c);
                    Vector2 torsoCenter = new Vector2(sz * 0.5f, sz * 0.26f);
                    DrawFilledCircle(pixels, size, torsoCenter, sz * 0.25f, c);
                    for (int y = 0; y < (int)(sz * 0.18f); y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            pixels[y * size + x] = Color.clear;
                        }
                    }
                    break;

                case IconType.MoreDots:
                    DrawFilledCircle(pixels, size, new Vector2(sz * 0.5f, sz * 0.72f), sz * 0.07f, c);
                    DrawFilledCircle(pixels, size, new Vector2(sz * 0.5f, sz * 0.50f), sz * 0.07f, c);
                    DrawFilledCircle(pixels, size, new Vector2(sz * 0.5f, sz * 0.28f), sz * 0.07f, c);
                    break;

                case IconType.Comment:
                    // Clean speech bubble with tail
                    Vector2 bCenter = new Vector2(sz * 0.50f, sz * 0.56f);
                    Vector2 bExt = new Vector2(sz * 0.25f, sz * 0.18f);
                    DrawFilledBox(pixels, size, bCenter, bExt, c);
                    // 4 rounded corners
                    float cr = sz * 0.08f;
                    DrawFilledCircle(pixels, size, bCenter + new Vector2(-bExt.x + cr, bExt.y - cr), cr, c);
                    DrawFilledCircle(pixels, size, bCenter + new Vector2(bExt.x - cr, bExt.y - cr), cr, c);
                    DrawFilledCircle(pixels, size, bCenter + new Vector2(-bExt.x + cr, -bExt.y + cr), cr, c);
                    DrawFilledCircle(pixels, size, bCenter + new Vector2(bExt.x - cr, -bExt.y + cr), cr, c);
                    // Tail triangle
                    Vector2 tA = new Vector2(sz * 0.32f, sz * 0.40f);
                    Vector2 tB = new Vector2(sz * 0.22f, sz * 0.20f);
                    Vector2 tC = new Vector2(sz * 0.44f, sz * 0.38f);
                    DrawSegment(pixels, size, tA, tB, stroke * 1.5f, c);
                    DrawSegment(pixels, size, tB, tC, stroke * 1.5f, c);
                    DrawFilledCircle(pixels, size, new Vector2(sz * 0.30f, sz * 0.32f), sz * 0.06f, c);
                    break;

                case IconType.ChevronDown:
                    Vector2 chLeft = new Vector2(sz * 0.26f, sz * 0.60f);
                    Vector2 chMid = new Vector2(sz * 0.50f, sz * 0.36f);
                    Vector2 chRight = new Vector2(sz * 0.74f, sz * 0.60f);
                    DrawSegment(pixels, size, chLeft, chMid, stroke * 1.2f, c);
                    DrawSegment(pixels, size, chMid, chRight, stroke * 1.2f, c);
                    break;

                case IconType.AxisGizmo:
                    Vector2 origin = new Vector2(sz * 0.40f, sz * 0.38f);
                    Color colY = new Color(0.2f, 0.85f, 0.3f); // Green Y
                    Color colX = new Color(0.95f, 0.25f, 0.25f); // Red X
                    Color colZ = new Color(0.25f, 0.55f, 0.98f); // Blue Z

                    // Green Y Up
                    Vector2 yTip = origin + new Vector2(0, sz * 0.46f);
                    DrawSegment(pixels, size, origin, yTip, stroke * 1.2f, colY);
                    DrawSegment(pixels, size, yTip, yTip + new Vector2(-sz * 0.08f, -sz * 0.08f), stroke * 1.1f, colY);
                    DrawSegment(pixels, size, yTip, yTip + new Vector2(sz * 0.08f, -sz * 0.08f), stroke * 1.1f, colY);

                    // Red X Right
                    Vector2 xTip = origin + new Vector2(sz * 0.44f, sz * 0.12f);
                    DrawSegment(pixels, size, origin, xTip, stroke * 1.2f, colX);
                    DrawSegment(pixels, size, xTip, xTip + new Vector2(-sz * 0.09f, sz * 0.04f), stroke * 1.1f, colX);
                    DrawSegment(pixels, size, xTip, xTip + new Vector2(-sz * 0.04f, -sz * 0.09f), stroke * 1.1f, colX);

                    // Blue Z Left-Down
                    Vector2 zTip = origin + new Vector2(-sz * 0.26f, -sz * 0.22f);
                    DrawSegment(pixels, size, origin, zTip, stroke * 1.2f, colZ);
                    DrawSegment(pixels, size, zTip, zTip + new Vector2(sz * 0.09f, sz * 0.02f), stroke * 1.1f, colZ);
                    DrawSegment(pixels, size, zTip, zTip + new Vector2(sz * 0.03f, sz * 0.09f), stroke * 1.1f, colZ);

                    DrawFilledCircle(pixels, size, origin, sz * 0.06f, Color.white);
                    break;
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            spriteCache[key] = sprite;
            return sprite;
        }

        private static void DrawSegment(Color[] pixels, int size, Vector2 a, Vector2 b, float thickness, Color col)
        {
            float halfThick = thickness * 0.5f;
            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x) - thickness - 1), 0, size - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x) + thickness + 1), 0, size - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y) - thickness - 1), 0, size - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y) + thickness + 1), 0, size - 1);

            Vector2 ba = b - a;
            float lenSq = ba.sqrMagnitude;
            if (lenSq < 0.0001f) return;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float h = Mathf.Clamp01(Vector2.Dot(p - a, ba) / lenSq);
                    float dist = ((p - a) - ba * h).magnitude;
                    float alpha = Mathf.Clamp01(halfThick - dist + 0.5f);
                    if (alpha > 0f)
                    {
                        int idx = y * size + x;
                        Color src = col;
                        src.a *= alpha;
                        pixels[idx] = BlendColors(pixels[idx], src);
                    }
                }
            }
        }

        private static void DrawFilledCircle(Color[] pixels, int size, Vector2 center, float radius, Color col)
        {
            int minX = Mathf.Clamp(Mathf.FloorToInt(center.x - radius - 1), 0, size - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(center.x + radius + 1), 0, size - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(center.y - radius - 1), 0, size - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(center.y + radius + 1), 0, size - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    if (alpha > 0f)
                    {
                        int idx = y * size + x;
                        Color src = col;
                        src.a *= alpha;
                        pixels[idx] = BlendColors(pixels[idx], src);
                    }
                }
            }
        }

        private static void DrawCircleRing(Color[] pixels, int size, Vector2 center, float radius, float thickness, Color col)
        {
            float halfThick = thickness * 0.5f;
            int minX = Mathf.Clamp(Mathf.FloorToInt(center.x - radius - thickness - 1), 0, size - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(center.x + radius + thickness + 1), 0, size - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(center.y - radius - thickness - 1), 0, size - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(center.y + radius + thickness + 1), 0, size - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dist = Mathf.Abs(Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) - radius);
                    float alpha = Mathf.Clamp01(halfThick - dist + 0.5f);
                    if (alpha > 0f)
                    {
                        int idx = y * size + x;
                        Color src = col;
                        src.a *= alpha;
                        pixels[idx] = BlendColors(pixels[idx], src);
                    }
                }
            }
        }

        private static void DrawArcRing(Color[] pixels, int size, Vector2 center, float radius, float thickness, float startAngleDeg, float sweepAngleDeg, Color col)
        {
            float halfThick = thickness * 0.5f;
            int minX = Mathf.Clamp(Mathf.FloorToInt(center.x - radius - thickness - 1), 0, size - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(center.x + radius + thickness + 1), 0, size - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(center.y - radius - thickness - 1), 0, size - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(center.y + radius + thickness + 1), 0, size - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float dist = Mathf.Abs(p.magnitude - radius);
                    if (dist > halfThick + 0.5f) continue;

                    float angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    float relAngle = Mathf.DeltaAngle(startAngleDeg, angle);
                    if (relAngle < -20f || relAngle > sweepAngleDeg + 20f) continue;

                    float alpha = Mathf.Clamp01(halfThick - dist + 0.5f);
                    if (alpha > 0f)
                    {
                        int idx = y * size + x;
                        Color src = col;
                        src.a *= alpha;
                        pixels[idx] = BlendColors(pixels[idx], src);
                    }
                }
            }
        }

        private static void DrawFilledBox(Color[] pixels, int size, Vector2 center, Vector2 halfExtents, Color col)
        {
            int minX = Mathf.Clamp(Mathf.FloorToInt(center.x - halfExtents.x), 0, size - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(center.x + halfExtents.x), 0, size - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(center.y - halfExtents.y), 0, size - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(center.y + halfExtents.y), 0, size - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int idx = y * size + x;
                    pixels[idx] = BlendColors(pixels[idx], col);
                }
            }
        }

        private static Color BlendColors(Color dst, Color src)
        {
            float outA = src.a + dst.a * (1f - src.a);
            if (outA <= 0.001f) return Color.clear;
            Color outC = (src * src.a + dst * dst.a * (1f - src.a)) / outA;
            outC.a = outA;
            return outC;
        }
    }
}
