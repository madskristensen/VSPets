using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VSPets.Models;

namespace VSPets.Animation
{
    /// <summary>
    /// Renders procedural pet sprites with frame-based animations.
    /// Draws pets with moving legs for walk/run cycles.
    /// </summary>
    public class ProceduralSpriteRenderer
    {
        private static readonly Lazy<ProceduralSpriteRenderer> _instance =
            new Lazy<ProceduralSpriteRenderer>(() => new ProceduralSpriteRenderer());

        private readonly Dictionary<string, BitmapSource> _frameCache =
            new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);

        private readonly object _cacheLock = new object();

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static ProceduralSpriteRenderer Instance => _instance.Value;

        private ProceduralSpriteRenderer() { }

        /// <summary>
        /// Gets the number of animation frames for a given state.
        /// </summary>
        public int GetFrameCount(PetState state)
        {
            return state switch
            {
                PetState.Idle => 2,      // Subtle breathing frames
                PetState.Walking => 4,    // Walk cycle
                PetState.Running => 4,    // Run cycle (faster)
                PetState.Sleeping => 2,   // Breathing while asleep
                PetState.Happy => 2,      // Excited/waving
                PetState.Exiting => 4,    // Walk cycle (leaving)
                PetState.Entering => 4,   // Walk cycle (entering)
                _ => 1
            };
        }

        /// <summary>
        /// Gets the frame duration in seconds for a given state.
        /// </summary>
        public double GetFrameDuration(PetState state)
        {
            return state switch
            {
                PetState.Idle => 0.5,     // Slow breathing
                PetState.Walking => 0.15,  // Normal walk cycle
                PetState.Running => 0.08,  // Fast run cycle
                PetState.Sleeping => 0.8,  // Very slow breathing
                PetState.Happy => 0.2,     // Quick excited motion
                PetState.Exiting => 0.15,  // Normal walk (exiting)
                PetState.Entering => 0.15, // Normal walk (entering)
                _ => 0.3
            };
        }

        /// <summary>
        /// Renders a sprite frame for the given pet configuration.
        /// </summary>
        public BitmapSource RenderFrame(PetType petType, PetColor color, PetState state, int frame, int size)
        {
            var key = $"{petType}_{color}_{state}_{frame}_{size}";

            lock (_cacheLock)
            {
                if (_frameCache.TryGetValue(key, out var cached))
                {
                    return cached;
                }

                var sprite = CreateSprite(petType, color, state, frame, size);
                _frameCache[key] = sprite;
                return sprite;
            }
        }

        /// <summary>
        /// Clears the frame cache.
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _frameCache.Clear();
            }
        }

        private BitmapSource CreateSprite(PetType petType, PetColor color, PetState state, int frame, int size)
        {
            var dpi = 96.0;
            var renderTarget = new RenderTargetBitmap(size, size, dpi, dpi, PixelFormats.Pbgra32);

            var drawingVisual = new DrawingVisual();
            using (var dc = drawingVisual.RenderOpen())
            {
                var baseColor = GetPetBaseColor(petType, color);
                var accentColor = GetPetAccentColor(petType, color);
                var eyeColor = Colors.Black;
                var outlinePen = CreateOutlinePen(color, size / 32.0); // Scaled outline

                switch (petType)
                {
                    case PetType.Cat:
                        DrawCat(dc, size, baseColor, accentColor, eyeColor, state, frame, outlinePen);
                        break;
                    case PetType.Dog:
                        DrawDog(dc, size, baseColor, accentColor, eyeColor, state, frame, outlinePen);
                        break;
                    case PetType.Fox:
                        DrawFox(dc, size, baseColor, accentColor, eyeColor, state, frame, outlinePen);
                        break;
                    case PetType.Clippy:
                        DrawClippy(dc, size, state, frame);
                        break;
                    case PetType.RubberDuck:
                        DrawRubberDuck(dc, size, state, frame);
                        break;
                    default:
                        DrawGenericPet(dc, size, baseColor, state, frame);
                        break;
                }
            }

            renderTarget.Render(drawingVisual);
            renderTarget.Freeze();
            return renderTarget;
        }

        #region Color Helpers

        private Color GetPetBaseColor(PetType petType, PetColor color)
        {
            return color switch
            {
                PetColor.Black => Color.FromRgb(40, 40, 40),
                PetColor.White => Color.FromRgb(245, 245, 245),
                PetColor.Brown => Color.FromRgb(139, 90, 43),
                PetColor.Gray => Color.FromRgb(128, 128, 128),
                PetColor.Orange => Color.FromRgb(255, 140, 0),
                PetColor.LightBrown => Color.FromRgb(205, 170, 125),
                PetColor.Red => Color.FromRgb(180, 60, 30),
                PetColor.Yellow => Color.FromRgb(255, 220, 100),
                _ => petType switch
                {
                    PetType.Cat => Color.FromRgb(100, 100, 100),
                    PetType.Dog => Color.FromRgb(180, 140, 100),
                    PetType.Fox => Color.FromRgb(220, 120, 50),
                    _ => Color.FromRgb(150, 150, 150)
                }
            };
        }

        private Color GetPetAccentColor(PetType petType, PetColor color)
        {
            // Lighter accent for chest/belly
            var baseColor = GetPetBaseColor(petType, color);
            return Color.FromRgb(
                (byte)Math.Min(255, baseColor.R + 60),
                (byte)Math.Min(255, baseColor.G + 60),
                (byte)Math.Min(255, baseColor.B + 60));
        }

        /// <summary>
        /// Gets an outline color for light-colored pets to ensure visibility.
        /// Returns null for dark colors that don't need outlines.
        /// </summary>
        private Color? GetOutlineColor(PetColor color)
        {
            return color switch
            {
                PetColor.White => Color.FromRgb(180, 180, 180),      // Light gray outline
                PetColor.LightBrown => Color.FromRgb(120, 90, 60),   // Darker brown outline
                PetColor.Yellow => Color.FromRgb(200, 160, 60),      // Darker yellow outline
                _ => null  // No outline needed for darker colors
            };
        }

        /// <summary>
        /// Creates a pen for outlining light-colored pets.
        /// Returns null if no outline is needed.
        /// </summary>
        private Pen CreateOutlinePen(PetColor color, double thickness)
        {
            var outlineColor = GetOutlineColor(color);
            if (outlineColor == null)
            {
                return null;
            }

            var pen = new Pen(CreateBrush(outlineColor.Value), thickness);
            pen.Freeze();
            return pen;
        }

        private Brush CreateBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        #endregion

        #region Cat Drawing

        private void DrawCat(DrawingContext dc, int size, Color baseColor, Color accentColor, Color eyeColor, PetState state, int frame, Pen outlinePen = null)
        {
            var scale = size / 32.0;
            var baseBrush = CreateBrush(baseColor);
            var accentBrush = CreateBrush(accentColor);
            var eyeBrush = CreateBrush(eyeColor);
            var pinkBrush = CreateBrush(Color.FromRgb(255, 180, 180));

            // Get leg positions based on state and frame
            var (frontLegOffset, backLegOffset, bodyBob) = GetLegPositions(state, frame);

            // Body (oval) with slight bob
            var bodyY = 14 * scale + bodyBob * scale;
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(16 * scale, bodyY),
                9 * scale, 6 * scale);

            // Belly accent
            dc.DrawEllipse(accentBrush, null,
                new Point(16 * scale, (bodyY + 2 * scale)),
                5 * scale, 3 * scale);

            // Front legs (animated)
            var frontLeg1Y = 20 * scale + frontLegOffset * scale;
            var frontLeg2Y = 20 * scale - frontLegOffset * scale;
            dc.DrawRoundedRectangle(baseBrush, outlinePen,
                new Rect(10 * scale, frontLeg1Y, 3 * scale, 8 * scale),
                1 * scale, 1 * scale);
            dc.DrawRoundedRectangle(baseBrush, outlinePen,
                new Rect(14 * scale, frontLeg2Y, 3 * scale, 8 * scale),
                1 * scale, 1 * scale);

            // Back legs (animated, opposite phase)
            var backLeg1Y = 20 * scale - backLegOffset * scale;
            var backLeg2Y = 20 * scale + backLegOffset * scale;
            dc.DrawRoundedRectangle(baseBrush, outlinePen,
                new Rect(19 * scale, backLeg1Y, 3 * scale, 8 * scale),
                1 * scale, 1 * scale);
            dc.DrawRoundedRectangle(baseBrush, outlinePen,
                new Rect(23 * scale, backLeg2Y, 3 * scale, 8 * scale),
                1 * scale, 1 * scale);

            // Head
            var headY = 10 * scale + bodyBob * 0.5 * scale;
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(8 * scale, headY),
                7 * scale, 6 * scale);

            // Ears (triangular)
            DrawTriangle(dc, baseBrush,
                new Point(3 * scale, (headY - 4 * scale)),
                new Point(1 * scale, (headY - 10 * scale)),
                new Point(6 * scale, (headY - 6 * scale)));
            DrawTriangle(dc, baseBrush,
                new Point(10 * scale, (headY - 4 * scale)),
                new Point(15 * scale, (headY - 10 * scale)),
                new Point(13 * scale, (headY - 6 * scale)));

            // Inner ears (pink)
            DrawTriangle(dc, pinkBrush,
                new Point(3.5 * scale, (headY - 5 * scale)),
                new Point(2.5 * scale, (headY - 8 * scale)),
                new Point(5 * scale, (headY - 6 * scale)));
            DrawTriangle(dc, pinkBrush,
                new Point(10.5 * scale, (headY - 5 * scale)),
                new Point(13.5 * scale, (headY - 8 * scale)),
                new Point(12 * scale, (headY - 6 * scale)));

            // Eyes
            DrawCatEyes(dc, eyeBrush, scale, headY, state, frame);

            // Nose (small pink triangle)
            DrawTriangle(dc, pinkBrush,
                new Point(7 * scale, (headY + 1 * scale)),
                new Point(8 * scale, (headY - 1 * scale)),
                new Point(9 * scale, (headY + 1 * scale)));

            // Whiskers
            var whiskerPen = new Pen(eyeBrush, 0.5 * scale);
            whiskerPen.Freeze();
            dc.DrawLine(whiskerPen, new Point(2 * scale, headY), new Point(6 * scale, headY));
            dc.DrawLine(whiskerPen, new Point(2 * scale, (headY + 2 * scale)), new Point(6 * scale, (headY + 1 * scale)));
            dc.DrawLine(whiskerPen, new Point(10 * scale, headY), new Point(14 * scale, headY));
            dc.DrawLine(whiskerPen, new Point(10 * scale, (headY + 1 * scale)), new Point(14 * scale, (headY + 2 * scale)));

            // Tail (curved, animated)
            DrawCatTail(dc, baseBrush, scale, bodyY, state, frame);
        }

        private void DrawCatEyes(DrawingContext dc, Brush eyeBrush, double scale, double headY, PetState state, int frame)
        {
            var whiteBrush = CreateBrush(Colors.White);
            var pupilBrush = eyeBrush;

            if (state == PetState.Sleeping)
            {
                // Closed eyes (lines)
                var eyePen = new Pen(eyeBrush, 1 * scale);
                eyePen.Freeze();
                dc.DrawLine(eyePen, new Point(4 * scale, headY), new Point(7 * scale, headY));
                dc.DrawLine(eyePen, new Point(9 * scale, headY), new Point(12 * scale, headY));
            }
            else if (state == PetState.Happy)
            {
                // Happy eyes (^ ^)
                var eyePen = new Pen(eyeBrush, 1 * scale);
                eyePen.Freeze();
                dc.DrawLine(eyePen, new Point(4 * scale, (headY - 1 * scale)), new Point(5.5 * scale, (headY - 2.5 * scale)));
                dc.DrawLine(eyePen, new Point(5.5 * scale, (headY - 2.5 * scale)), new Point(7 * scale, (headY - 1 * scale)));
                dc.DrawLine(eyePen, new Point(9 * scale, (headY - 1 * scale)), new Point(10.5 * scale, (headY - 2.5 * scale)));
                dc.DrawLine(eyePen, new Point(10.5 * scale, (headY - 2.5 * scale)), new Point(12 * scale, (headY - 1 * scale)));
            }
            else
            {
                // Normal cat eyes (vertical pupils)
                dc.DrawEllipse(whiteBrush, null, new Point(5 * scale, (headY - 1 * scale)), 2 * scale, 2.5 * scale);
                dc.DrawEllipse(whiteBrush, null, new Point(11 * scale, (headY - 1 * scale)), 2 * scale, 2.5 * scale);
                dc.DrawEllipse(pupilBrush, null, new Point(5 * scale, (headY - 1 * scale)), 0.8 * scale, 2 * scale);
                dc.DrawEllipse(pupilBrush, null, new Point(11 * scale, (headY - 1 * scale)), 0.8 * scale, 2 * scale);
            }
        }

        private void DrawCatTail(DrawingContext dc, Brush brush, double scale, double bodyY, PetState state, int frame)
        {
            var tailWag = state == PetState.Happy ? Math.Sin(frame * Math.PI) * 3 : 0;
            var points = new[]
            {
                new Point(25 * scale, bodyY),
                new Point(28 * scale, (bodyY - 4 * scale + tailWag * scale)),
                new Point(30 * scale, (bodyY - 10 * scale + tailWag * scale)),
                new Point(29 * scale, (bodyY - 14 * scale))
            };

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(points[0], false, false);
                ctx.BezierTo(points[1], points[2], points[3], true, true);
            }
            geometry.Freeze();

            var pen = new Pen(brush, 3 * scale);
            pen.StartLineCap = PenLineCap.Round;
            pen.EndLineCap = PenLineCap.Round;
            pen.Freeze();

            dc.DrawGeometry(null, pen, geometry);
        }

        #endregion

        #region Dog Drawing

        private void DrawDog(DrawingContext dc, int size, Color baseColor, Color accentColor, Color eyeColor, PetState state, int frame, Pen outlinePen = null)
        {
            var scale = size / 32.0;
            var baseBrush = CreateBrush(baseColor);
            var accentBrush = CreateBrush(accentColor);
            var eyeBrush = CreateBrush(eyeColor);
            var noseBrush = CreateBrush(Colors.Black);
            var tongueBrush = CreateBrush(Color.FromRgb(255, 120, 140));

            var (frontLegOffset, backLegOffset, bodyBob) = GetLegPositions(state, frame);

            // Body (rounder than cat)
            var bodyY = 14 * scale + bodyBob * scale;
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(16 * scale, bodyY),
                10 * scale, 7 * scale);

            // Belly
            dc.DrawEllipse(accentBrush, null,
                new Point(16 * scale, (bodyY + 2 * scale)),
                6 * scale, 4 * scale);

            // Legs with paws
            DrawDogLegs(dc, baseBrush, accentBrush, scale, frontLegOffset, backLegOffset, outlinePen);

            // Head (rounder)
            var headY = 11 * scale + bodyBob * 0.5 * scale;
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(7 * scale, headY),
                7 * scale, 6 * scale);

            // Snout
            dc.DrawEllipse(accentBrush, outlinePen,
                new Point(3 * scale, (headY + 2 * scale)),
                4 * scale, 3 * scale);

            // Floppy ears
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(2 * scale, (headY + 1 * scale)),
                3 * scale, 5 * scale);
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(12 * scale, (headY + 1 * scale)),
                3 * scale, 5 * scale);

            // Eyes
            DrawDogEyes(dc, eyeBrush, scale, headY, state);

            // Nose
            dc.DrawEllipse(noseBrush, null,
                new Point(1 * scale, (headY + 1 * scale)),
                2 * scale, 1.5 * scale);

            // Tongue (when happy or running)
            if (state == PetState.Happy || state == PetState.Running)
            {
                dc.DrawEllipse(tongueBrush, null,
                    new Point(2 * scale, (headY + 5 * scale)),
                    2 * scale, 3 * scale);
            }

            // Tail (wagging)
            DrawDogTail(dc, baseBrush, scale, bodyY, state, frame);
        }

        private void DrawDogLegs(DrawingContext dc, Brush baseBrush, Brush accentBrush, double scale, double frontOffset, double backOffset, Pen outlinePen = null)
        {
            // Front legs
            dc.DrawRoundedRectangle(baseBrush, outlinePen,
                new Rect(8 * scale, (20 + frontOffset) * scale, 4 * scale, 9 * scale),
                2 * scale, 2 * scale);
            dc.DrawRoundedRectangle(baseBrush, outlinePen,
                new Rect(13 * scale, (20 - frontOffset) * scale, 4 * scale, 9 * scale),
                2 * scale, 2 * scale);

            // Back legs
            dc.DrawRoundedRectangle(baseBrush, outlinePen,
                new Rect(19 * scale, (20 - backOffset) * scale, 4 * scale, 9 * scale),
                2 * scale, 2 * scale);
            dc.DrawRoundedRectangle(baseBrush, outlinePen,
                new Rect(24 * scale, (20 + backOffset) * scale, 4 * scale, 9 * scale),
                2 * scale, 2 * scale);

            // Paws (lighter color)
            dc.DrawEllipse(accentBrush, outlinePen, new Point(10 * scale, (29 + frontOffset) * scale), 2.5 * scale, 1.5 * scale);
            dc.DrawEllipse(accentBrush, outlinePen, new Point(15 * scale, (29 - frontOffset) * scale), 2.5 * scale, 1.5 * scale);
            dc.DrawEllipse(accentBrush, outlinePen, new Point(21 * scale, (29 - backOffset) * scale), 2.5 * scale, 1.5 * scale);
            dc.DrawEllipse(accentBrush, outlinePen, new Point(26 * scale, (29 + backOffset) * scale), 2.5 * scale, 1.5 * scale);
        }

        private void DrawDogEyes(DrawingContext dc, Brush eyeBrush, double scale, double headY, PetState state)
        {
            var whiteBrush = CreateBrush(Colors.White);

            if (state == PetState.Sleeping)
            {
                var eyePen = new Pen(eyeBrush, 1 * scale);
                eyePen.Freeze();
                dc.DrawLine(eyePen, new Point(4 * scale, (headY - 1 * scale)), new Point(7 * scale, (headY - 1 * scale)));
                dc.DrawLine(eyePen, new Point(9 * scale, (headY - 1 * scale)), new Point(12 * scale, (headY - 1 * scale)));
            }
            else if (state == PetState.Happy)
            {
                // Happy squinty eyes
                var eyePen = new Pen(eyeBrush, 1.5 * scale);
                eyePen.Freeze();
                dc.DrawArc(eyePen, new Point(4 * scale, (headY - 1 * scale)), 2 * scale, 0, 180);
                dc.DrawArc(eyePen, new Point(10 * scale, (headY - 1 * scale)), 2 * scale, 0, 180);
            }
            else
            {
                // Normal round dog eyes
                dc.DrawEllipse(whiteBrush, null, new Point(5 * scale, (headY - 1 * scale)), 2.5 * scale, 2.5 * scale);
                dc.DrawEllipse(whiteBrush, null, new Point(11 * scale, (headY - 1 * scale)), 2.5 * scale, 2.5 * scale);
                dc.DrawEllipse(eyeBrush, null, new Point(5 * scale, (headY - 1 * scale)), 1.5 * scale, 1.5 * scale);
                dc.DrawEllipse(eyeBrush, null, new Point(11 * scale, (headY - 1 * scale)), 1.5 * scale, 1.5 * scale);
            }
        }

        private void DrawDogTail(DrawingContext dc, Brush brush, double scale, double bodyY, PetState state, int frame)
        {
            // Dogs wag tail more when happy
            var wagSpeed = state == PetState.Happy ? 2 : (state == PetState.Walking ? 0.5 : 0);
            var wagAmount = Math.Sin(frame * Math.PI * wagSpeed) * 4;

            var pen = new Pen(brush, 4 * scale);
            pen.StartLineCap = PenLineCap.Round;
            pen.EndLineCap = PenLineCap.Round;
            pen.Freeze();

            dc.DrawLine(pen,
                new Point(26 * scale, (bodyY - 2 * scale)),
                new Point((30 + wagAmount) * scale, (bodyY - 8 * scale)));
        }

        #endregion

        #region Fox Drawing

        private void DrawFox(DrawingContext dc, int size, Color baseColor, Color accentColor, Color eyeColor, PetState state, int frame, Pen outlinePen = null)
        {
            var scale = size / 32.0;
            var baseBrush = CreateBrush(baseColor);
            var accentBrush = CreateBrush(accentColor);
            var whiteBrush = CreateBrush(Colors.White);
            var eyeBrush = CreateBrush(eyeColor);
            var noseBrush = CreateBrush(Colors.Black);

            var (frontLegOffset, backLegOffset, bodyBob) = GetLegPositions(state, frame);

            // Body (sleek, longer than cat)
            var bodyY = 14 * scale + bodyBob * scale;
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(16 * scale, bodyY),
                11 * scale, 6 * scale);

            // White belly
            dc.DrawEllipse(whiteBrush, null,
                new Point(14 * scale, (bodyY + 2 * scale)),
                6 * scale, 3 * scale);

            // Legs (slender, black tips)
            DrawFoxLegs(dc, baseBrush, scale, frontLegOffset, backLegOffset, outlinePen);

            // Head (pointed snout)
            var headY = 10 * scale + bodyBob * 0.5 * scale;
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(6 * scale, headY),
                6 * scale, 5 * scale);

            // White face markings
            dc.DrawEllipse(whiteBrush, null,
                new Point(5 * scale, (headY + 1 * scale)),
                3 * scale, 3 * scale);

            // Pointed snout
            DrawTriangle(dc, baseBrush,
                new Point(0 * scale, (headY + 2 * scale)),
                new Point(3 * scale, (headY - 1 * scale)),
                new Point(3 * scale, (headY + 3 * scale)));

            // Large pointed ears
            DrawTriangle(dc, baseBrush,
                new Point(2 * scale, (headY - 3 * scale)),
                new Point(0 * scale, (headY - 11 * scale)),
                new Point(6 * scale, (headY - 5 * scale)));
            DrawTriangle(dc, baseBrush,
                new Point(8 * scale, (headY - 3 * scale)),
                new Point(12 * scale, (headY - 11 * scale)),
                new Point(11 * scale, (headY - 5 * scale)));

            // Inner ears (dark)
            var darkBrush = CreateBrush(Color.FromRgb(60, 30, 10));
            DrawTriangle(dc, darkBrush,
                new Point(2.5 * scale, (headY - 4 * scale)),
                new Point(1.5 * scale, (headY - 8 * scale)),
                new Point(5 * scale, (headY - 5.5 * scale)));
            DrawTriangle(dc, darkBrush,
                new Point(8.5 * scale, (headY - 4 * scale)),
                new Point(11 * scale, (headY - 8 * scale)),
                new Point(10 * scale, (headY - 5.5 * scale)));

            // Eyes (almond shaped)
            DrawFoxEyes(dc, eyeBrush, scale, headY, state);

            // Nose
            dc.DrawEllipse(noseBrush, null,
                new Point(0.5 * scale, (headY + 1.5 * scale)),
                1.5 * scale, 1 * scale);

            // Fluffy tail with white tip
            DrawFoxTail(dc, baseBrush, whiteBrush, scale, bodyY, state, frame);
        }

        private void DrawFoxLegs(DrawingContext dc, Brush baseBrush, double scale, double frontOffset, double backOffset, Pen outlinePen = null)
        {
            var blackBrush = CreateBrush(Color.FromRgb(30, 30, 30));

            // Front legs
            dc.DrawRoundedRectangle(baseBrush, outlinePen,
                new Rect(8 * scale, (20 + frontOffset) * scale, 3 * scale, 8 * scale),
                1 * scale, 1 * scale);
            dc.DrawRoundedRectangle(baseBrush, outlinePen,
                new Rect(12 * scale, (20 - frontOffset) * scale, 3 * scale, 8 * scale),
                1 * scale, 1 * scale);

            // Back legs
            dc.DrawRoundedRectangle(baseBrush, outlinePen,
                new Rect(20 * scale, (20 - backOffset) * scale, 3 * scale, 8 * scale),
                1 * scale, 1 * scale);
            dc.DrawRoundedRectangle(baseBrush, outlinePen,
                new Rect(24 * scale, (20 + backOffset) * scale, 3 * scale, 8 * scale),
                1 * scale, 1 * scale);

            // Black feet
            dc.DrawEllipse(blackBrush, null, new Point(9.5 * scale, (28 + frontOffset) * scale), 2 * scale, 1.5 * scale);
            dc.DrawEllipse(blackBrush, null, new Point(13.5 * scale, (28 - frontOffset) * scale), 2 * scale, 1.5 * scale);
            dc.DrawEllipse(blackBrush, null, new Point(21.5 * scale, (28 - backOffset) * scale), 2 * scale, 1.5 * scale);
            dc.DrawEllipse(blackBrush, null, new Point(25.5 * scale, (28 + backOffset) * scale), 2 * scale, 1.5 * scale);
        }

        private void DrawFoxEyes(DrawingContext dc, Brush eyeBrush, double scale, double headY, PetState state)
        {
            var whiteBrush = CreateBrush(Colors.White);
            var amberBrush = CreateBrush(Color.FromRgb(255, 180, 0));

            if (state == PetState.Sleeping)
            {
                var eyePen = new Pen(eyeBrush, 1 * scale);
                eyePen.Freeze();
                dc.DrawLine(eyePen, new Point(3 * scale, (headY - 1 * scale)), new Point(6 * scale, (headY - 1 * scale)));
                dc.DrawLine(eyePen, new Point(7 * scale, (headY - 1 * scale)), new Point(10 * scale, (headY - 1 * scale)));
            }
            else if (state == PetState.Happy)
            {
                // Sly/happy fox eyes
                var eyePen = new Pen(eyeBrush, 1 * scale);
                eyePen.Freeze();
                dc.DrawLine(eyePen, new Point(3 * scale, headY), new Point(4.5 * scale, (headY - 2 * scale)));
                dc.DrawLine(eyePen, new Point(4.5 * scale, (headY - 2 * scale)), new Point(6 * scale, headY));
                dc.DrawLine(eyePen, new Point(7 * scale, headY), new Point(8.5 * scale, (headY - 2 * scale)));
                dc.DrawLine(eyePen, new Point(8.5 * scale, (headY - 2 * scale)), new Point(10 * scale, headY));
            }
            else
            {
                // Almond eyes with amber color
                dc.DrawEllipse(amberBrush, null, new Point(4.5 * scale, (headY - 1 * scale)), 2 * scale, 1.5 * scale);
                dc.DrawEllipse(amberBrush, null, new Point(8.5 * scale, (headY - 1 * scale)), 2 * scale, 1.5 * scale);
                dc.DrawEllipse(eyeBrush, null, new Point(4.5 * scale, (headY - 1 * scale)), 1 * scale, 1.2 * scale);
                dc.DrawEllipse(eyeBrush, null, new Point(8.5 * scale, (headY - 1 * scale)), 1 * scale, 1.2 * scale);
            }
        }

        private void DrawFoxTail(DrawingContext dc, Brush baseBrush, Brush whiteBrush, double scale, double bodyY, PetState state, int frame)
        {
            var wagAmount = state == PetState.Happy ? Math.Sin(frame * Math.PI) * 3 : Math.Sin(frame * Math.PI * 0.3) * 1;

            // Fluffy tail body
            var tailGeometry = new StreamGeometry();
            using (var ctx = tailGeometry.Open())
            {
                ctx.BeginFigure(new Point(26 * scale, bodyY), true, true);
                ctx.BezierTo(
                    new Point(28 * scale, (bodyY - 6 * scale + wagAmount * scale)),
                    new Point(32 * scale, (bodyY - 10 * scale + wagAmount * scale)),
                    new Point(30 * scale, (bodyY - 14 * scale + wagAmount * scale)),
                    true, true);
                ctx.BezierTo(
                    new Point(28 * scale, (bodyY - 12 * scale + wagAmount * scale)),
                    new Point(25 * scale, (bodyY - 4 * scale)),
                    new Point(26 * scale, bodyY),
                    true, true);
            }
            tailGeometry.Freeze();
            dc.DrawGeometry(baseBrush, null, tailGeometry);

            // White tail tip
            dc.DrawEllipse(whiteBrush, null,
                new Point(30 * scale, (bodyY - 13 * scale + wagAmount * scale)),
                2 * scale, 3 * scale);
        }

        #endregion

        #region Other Pets

        private void DrawClippy(DrawingContext dc, int size, PetState state, int frame)
        {
            var scale = size / 32.0;
            var silverBrush = CreateBrush(Color.FromRgb(180, 180, 200));
            var eyeBrush = CreateBrush(Colors.Black);
            var eyeWhiteBrush = CreateBrush(Colors.White);

            var bodyBob = state == PetState.Walking || state == PetState.Running ? Math.Sin(frame * Math.PI) * 2 : 0;

            // Paperclip body shape (curved wire)
            var pen = new Pen(silverBrush, 4 * scale);
            pen.StartLineCap = PenLineCap.Round;
            pen.EndLineCap = PenLineCap.Round;
            pen.Freeze();

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(8 * scale, (28 + bodyBob) * scale), false, false);
                ctx.BezierTo(
                    new Point(4 * scale, (24 + bodyBob) * scale),
                    new Point(4 * scale, (8 + bodyBob) * scale),
                    new Point(16 * scale, (4 + bodyBob) * scale),
                    true, true);
                ctx.BezierTo(
                    new Point(28 * scale, (8 + bodyBob) * scale),
                    new Point(28 * scale, (20 + bodyBob) * scale),
                    new Point(20 * scale, (24 + bodyBob) * scale),
                    true, true);
                ctx.BezierTo(
                    new Point(12 * scale, (20 + bodyBob) * scale),
                    new Point(12 * scale, (12 + bodyBob) * scale),
                    new Point(16 * scale, (10 + bodyBob) * scale),
                    true, true);
            }
            geometry.Freeze();
            dc.DrawGeometry(null, pen, geometry);

            // Eyes on the loop
            var eyeY = (8 + bodyBob) * scale;
            dc.DrawEllipse(eyeWhiteBrush, null, new Point(13 * scale, eyeY), 3 * scale, 3 * scale);
            dc.DrawEllipse(eyeWhiteBrush, null, new Point(21 * scale, eyeY), 3 * scale, 3 * scale);

            if (state == PetState.Happy)
            {
                // Happy eyes
                var eyePen = new Pen(eyeBrush, 1.5 * scale);
                eyePen.Freeze();
                dc.DrawLine(eyePen, new Point(11 * scale, eyeY), new Point(13 * scale, (eyeY - 2 * scale)));
                dc.DrawLine(eyePen, new Point(13 * scale, (eyeY - 2 * scale)), new Point(15 * scale, eyeY));
                dc.DrawLine(eyePen, new Point(19 * scale, eyeY), new Point(21 * scale, (eyeY - 2 * scale)));
                dc.DrawLine(eyePen, new Point(21 * scale, (eyeY - 2 * scale)), new Point(23 * scale, eyeY));
            }
            else
            {
                dc.DrawEllipse(eyeBrush, null, new Point(13 * scale, eyeY), 1.5 * scale, 1.5 * scale);
                dc.DrawEllipse(eyeBrush, null, new Point(21 * scale, eyeY), 1.5 * scale, 1.5 * scale);
            }

            // Eyebrows
            var browPen = new Pen(eyeBrush, 1 * scale);
            browPen.Freeze();
            dc.DrawLine(browPen, new Point(10 * scale, (eyeY - 4 * scale)), new Point(15 * scale, (eyeY - 5 * scale)));
            dc.DrawLine(browPen, new Point(19 * scale, (eyeY - 5 * scale)), new Point(24 * scale, (eyeY - 4 * scale)));
        }

        private void DrawRubberDuck(DrawingContext dc, int size, PetState state, int frame)
        {
            var scale = size / 32.0;
            var yellowBrush = CreateBrush(Color.FromRgb(255, 220, 50));
            var orangeBrush = CreateBrush(Color.FromRgb(255, 140, 0));
            var eyeBrush = CreateBrush(Colors.Black);

            var bodyBob = state == PetState.Walking ? Math.Sin(frame * Math.PI) * 1.5 : 0;
            var waddle = state == PetState.Walking ? Math.Sin(frame * Math.PI * 2) * 2 : 0;

            // Body
            dc.DrawEllipse(yellowBrush, null,
                new Point((16 + waddle) * scale, (18 + bodyBob) * scale),
                10 * scale, 8 * scale);

            // Head
            dc.DrawEllipse(yellowBrush, null,
                new Point((10 + waddle) * scale, (10 + bodyBob) * scale),
                7 * scale, 6 * scale);

            // Beak
            dc.DrawEllipse(orangeBrush, null,
                new Point((4 + waddle) * scale, (12 + bodyBob) * scale),
                4 * scale, 2 * scale);

            // Eyes
            if (state == PetState.Happy)
            {
                var eyePen = new Pen(eyeBrush, 1 * scale);
                eyePen.Freeze();
                dc.DrawLine(eyePen, new Point((7 + waddle) * scale, (8 + bodyBob) * scale),
                    new Point((9 + waddle) * scale, (6 + bodyBob) * scale));
                dc.DrawLine(eyePen, new Point((9 + waddle) * scale, (6 + bodyBob) * scale),
                    new Point((11 + waddle) * scale, (8 + bodyBob) * scale));
            }
            else
            {
                dc.DrawEllipse(eyeBrush, null,
                    new Point((9 + waddle) * scale, (8 + bodyBob) * scale),
                    1.5 * scale, 1.5 * scale);
            }

            // Wing hint
            dc.DrawEllipse(yellowBrush, null,
                new Point((18 + waddle) * scale, (16 + bodyBob) * scale),
                4 * scale, 5 * scale);

            // Tail feathers
            DrawTriangle(dc, yellowBrush,
                new Point((26 + waddle) * scale, (16 + bodyBob) * scale),
                new Point((30 + waddle) * scale, (14 + bodyBob) * scale),
                new Point((30 + waddle) * scale, (20 + bodyBob) * scale));
        }

        private void DrawGenericPet(DrawingContext dc, int size, Color baseColor, PetState state, int frame)
        {
            var scale = size / 32.0;
            var baseBrush = CreateBrush(baseColor);
            var eyeBrush = CreateBrush(Colors.Black);

            var (_, _, bodyBob) = GetLegPositions(state, frame);

            // Simple blob body
            dc.DrawEllipse(baseBrush, null,
                new Point(16 * scale, (16 + bodyBob) * scale),
                12 * scale, 10 * scale);

            // Eyes
            dc.DrawEllipse(eyeBrush, null, new Point(12 * scale, (12 + bodyBob) * scale), 2 * scale, 2 * scale);
            dc.DrawEllipse(eyeBrush, null, new Point(20 * scale, (12 + bodyBob) * scale), 2 * scale, 2 * scale);
        }

        #endregion

        #region Animation Helpers

        private (double frontLegOffset, double backLegOffset, double bodyBob) GetLegPositions(PetState state, int frame)
        {
            switch (state)
            {
                case PetState.Walking:
                case PetState.Exiting:
                case PetState.Entering:
                    // 4-frame walk cycle
                    var walkPhase = (frame % 4) * (Math.PI / 2);
                    return (
                        Math.Sin(walkPhase) * 2,           // Front legs
                        Math.Sin(walkPhase + Math.PI) * 2, // Back legs (opposite)
                        Math.Abs(Math.Sin(walkPhase * 2)) * 0.5  // Small body bob
                    );

                case PetState.Running:
                    // Faster, more exaggerated
                    var runPhase = (frame % 4) * (Math.PI / 2);
                    return (
                        Math.Sin(runPhase) * 3,
                        Math.Sin(runPhase + Math.PI) * 3,
                        Math.Abs(Math.Sin(runPhase * 2)) * 1.5
                    );

                case PetState.Idle:
                case PetState.Sleeping:
                    // Subtle breathing bob
                    var breathPhase = (frame % 2) * Math.PI;
                    return (0, 0, Math.Sin(breathPhase) * 0.3);

                case PetState.Happy:
                    // Excited bouncing
                    var happyPhase = (frame % 2) * Math.PI;
                    return (0, 0, Math.Abs(Math.Sin(happyPhase)) * 2);

                default:
                    return (0, 0, 0);
            }
        }

        private void DrawTriangle(DrawingContext dc, Brush brush, Point p1, Point p2, Point p3)
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(p1, true, true);
                ctx.LineTo(p2, true, false);
                ctx.LineTo(p3, true, false);
            }
            geometry.Freeze();
            dc.DrawGeometry(brush, null, geometry);
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for DrawingContext.
    /// </summary>
    internal static class DrawingContextExtensions
    {
        /// <summary>
        /// Draws an arc (half ellipse).
        /// </summary>
        public static void DrawArc(this DrawingContext dc, Pen pen, Point center, double radius, double startAngle, double sweepAngle)
        {
            var startRad = startAngle * Math.PI / 180;
            var endRad = (startAngle + sweepAngle) * Math.PI / 180;

            var start = new Point(center.X + radius * Math.Cos(startRad), center.Y - radius * Math.Sin(startRad));
            var end = new Point(center.X + radius * Math.Cos(endRad), center.Y - radius * Math.Sin(endRad));

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(start, false, false);
                ctx.ArcTo(end, new Size(radius, radius), 0, sweepAngle > 180, SweepDirection.Counterclockwise, true, false);
            }
            geometry.Freeze();
            dc.DrawGeometry(null, pen, geometry);
        }
    }
}
