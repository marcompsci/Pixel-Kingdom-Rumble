using UnityEngine;

namespace PKR.EditorTools
{
    /// <summary>
    /// A small paper-doll generator for placeholder fighter sprites (16x24, faces right, bottom pivot), so each
    /// Versus fighter gets an original, readable silhouette from a few colors and parts. Real art replaces these.
    /// </summary>
    public static class FighterArt
    {
        public enum Build { Slim, Normal, Bulky, Small }
        public enum Head { Hair, Hood, Helmet, Band, Ears, Horns, Cap, Crown, Mohawk, Bald }
        public enum Extra { None, Cape, Scarf, Staff, Blade, Tail, Gloves, Board, Ball, Antenna, Wings }

        public struct Look
        {
            public Build build;
            public Head head;
            public Extra extra;
            public Color32 skin, hair, top, accent, pants, shoes, eye;
        }

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 Ink = new Color32(24, 20, 32, 255);

        public static Sprite Make(string file, Look l) => PlaceholderArt.GetOrCreate(file, 16, 24, true, (x, y) => Pixel(l, x, y));

        static Color32 Pixel(Look l, int x, int y)
        {
            int bodyL = 4, bodyR = 11, headL = 5, headR = 11;
            switch (l.build)
            {
                case Build.Slim: bodyL = 5; bodyR = 10; break;
                case Build.Bulky: bodyL = 3; bodyR = 12; headL = 4; break;
                case Build.Small: bodyL = 5; bodyR = 10; headL = 5; headR = 10; break;
            }
            // Small fighters are drawn 3 pixels lower (shorter legs) and leave the top rows empty.
            int top = l.build == Build.Small ? 20 : 23;
            int legTop = l.build == Build.Small ? 4 : 6;
            int torsoTop = l.build == Build.Small ? 11 : 14;
            int headBottom = torsoTop + 1, headTop = top - 3;

            // Extras drawn behind/around the body first.
            var e = ExtraPixel(l, x, y, bodyL, bodyR, legTop, torsoTop, headTop);
            if (e.a > 0) return e;

            if (y <= 1) // shoes
                return (x >= bodyL + 1 && x <= bodyL + 2) || (x >= bodyR - 2 && x <= bodyR - 1) ? l.shoes : Clear;
            if (y <= legTop) // legs
            {
                bool leg = (x >= bodyL + 1 && x <= bodyL + 2) || (x >= bodyR - 2 && x <= bodyR - 1);
                if (l.build == Build.Bulky) leg = (x >= bodyL + 1 && x <= bodyL + 3) || (x >= bodyR - 3 && x <= bodyR - 1);
                return leg ? l.pants : Clear;
            }
            if (y <= torsoTop) // torso + arms
            {
                if (x < bodyL - 1 || x > bodyR + 1) return Clear;
                bool arm = x == bodyL - 1 || x == bodyR + 1;
                if (arm)
                {
                    if (y < legTop + 3) return Clear;
                    bool hand = y == legTop + 3;
                    if (l.extra == Extra.Gloves && y <= legTop + 4) return l.accent;
                    return hand ? l.skin : l.top;
                }
                if (y == legTop + 1) return l.accent;                                // belt
                if (y >= torsoTop - 1 && (x == bodyL || x == bodyR)) return l.accent; // shoulders
                return l.top;
            }
            if (y <= headTop) // face
            {
                if (x < headL || x > headR) return Clear;
                if (y == headTop - 1 && x == headR - 1) return l.eye.a > 0 ? l.eye : Ink;
                if (y == headBottom && x >= headR - 2) return l.skin;
                return l.skin;
            }
            if (y <= top) return HeadPixel(l, x, y, headL, headR, headTop, top);
            return Clear;
        }

        static Color32 HeadPixel(Look l, int x, int y, int headL, int headR, int headTop, int top)
        {
            switch (l.head)
            {
                case Head.Hair: return x >= headL - 1 && x <= headR && !(y == top && (x == headL - 1 || x == headR)) ? l.hair : Clear;
                case Head.Hood: return x >= headL - 1 && x <= headR + 1 ? l.hair : Clear;
                case Head.Helmet:
                    if (y == top) return x >= headL + 1 && x <= headR - 1 ? l.hair : Clear;
                    return x >= headL - 1 && x <= headR + 1 ? (y == headTop + 1 ? l.accent : l.hair) : Clear;
                case Head.Band:
                    if (y == headTop + 1) return x >= headL && x <= headR ? l.accent : x == headL - 1 || x == headL - 2 ? l.accent : Clear;
                    return x >= headL && x <= headR - 1 ? l.hair : Clear;
                case Head.Ears:
                    if (y == top) return x == headL || x == headR ? l.hair : Clear;
                    if (y == top - 1) return x == headL || x == headL + 1 || x == headR || x == headR - 1 ? l.hair : Clear;
                    return x >= headL && x <= headR ? l.hair : Clear;
                case Head.Horns:
                    if (y == top) return x == headL - 1 || x == headR + 1 ? l.accent : Clear;
                    if (y == top - 1) return x == headL || x == headR ? l.accent : x > headL && x < headR ? l.hair : Clear;
                    return x >= headL && x <= headR ? l.hair : Clear;
                case Head.Cap:
                    if (y == headTop + 1) return x >= headL - 1 && x <= headR + 3 ? l.accent : Clear; // brim
                    return x >= headL && x <= headR ? l.hair : Clear;
                case Head.Crown:
                    if (y == top) return x >= headL && x <= headR && (x - headL) % 2 == 0 ? l.accent : Clear;
                    if (y == top - 1) return x >= headL && x <= headR ? l.accent : Clear;
                    return x >= headL && x <= headR ? l.hair : Clear;
                case Head.Mohawk:
                    if (y >= top - 1) return x >= headL + 2 && x <= headR - 2 ? l.hair : Clear;
                    return x >= headL + 1 && x <= headR - 1 ? l.hair : Clear;
                case Head.Bald: return y == headTop + 1 && x >= headL && x <= headR ? l.skin : Clear;
            }
            return Clear;
        }

        static Color32 ExtraPixel(Look l, int x, int y, int bodyL, int bodyR, int legTop, int torsoTop, int headTop)
        {
            switch (l.extra)
            {
                case Extra.Cape: // behind the back (left side, the fighter faces right)
                    if (y > legTop && y <= torsoTop && x >= bodyL - 3 && x <= bodyL - 2) return l.accent;
                    return Clear;
                case Extra.Scarf:
                    if (y == torsoTop && x >= bodyL - 3 && x <= bodyR) return l.accent;
                    if (y == torsoTop - 1 && (x == bodyL - 3 || x == bodyL - 2)) return l.accent;
                    return Clear;
                case Extra.Staff:
                    if (x == bodyR + 2 && y >= 2 && y <= headTop + 2) return y >= headTop + 1 ? l.accent : new Color32(140, 98, 60, 255);
                    return Clear;
                case Extra.Blade:
                    if (x == bodyR + 2 && y >= legTop + 3 && y <= headTop + 1) return y == legTop + 3 ? l.accent : new Color32(220, 228, 240, 255);
                    if (y == legTop + 4 && x >= bodyR + 1 && x <= bodyR + 3) return l.accent;
                    return Clear;
                case Extra.Tail:
                    if (y >= legTop - 1 && y <= legTop + 2 && x >= bodyL - 3 && x < bodyL - 1 + (y - legTop + 1) / 2) return l.hair;
                    return Clear;
                case Extra.Board:
                    if (y == 2 && x >= bodyL - 2 && x <= bodyR + 2) return l.accent;
                    if (y == 1 && (x == bodyL - 1 || x == bodyR + 1)) return Ink;
                    return Clear;
                case Extra.Ball:
                    if (y <= 3 && x >= bodyR + 2 && x <= bodyR + 4 && y >= 1) return (x + y) % 2 == 0 ? Ink : new Color32(245, 245, 245, 255);
                    return Clear;
                case Extra.Antenna:
                    if (x == bodyR - 2 && y >= headTop + 2) return y == 23 || y == headTop + 4 ? l.accent : Ink;
                    return Clear;
                case Extra.Wings:
                    if (x >= bodyL - 4 && x <= bodyL - 2 && y >= torsoTop - 4 && y <= torsoTop + 1 && (x - bodyL + 4) <= (y - torsoTop + 4))
                        return l.accent;
                    return Clear;
            }
            return Clear;
        }
    }
}
