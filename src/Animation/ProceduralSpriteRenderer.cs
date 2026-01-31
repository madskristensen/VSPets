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
            new(() => new ProceduralSpriteRenderer());

        private readonly Dictionary<string, BitmapSource> _frameCache =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly object _cacheLock = new();

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
                if (_frameCache.TryGetValue(key, out BitmapSource cached))
                {
                    return cached;
                }

                BitmapSource sprite = CreateSprite(petType, color, state, frame, size);
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
            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                Color baseColor = GetPetBaseColor(petType, color);
                Color accentColor = GetPetAccentColor(petType, color);
                Color eyeColor = Colors.Black;
                Pen outlinePen = CreateOutlinePen(color, size / 32.0); // Scaled outline

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
            Color baseColor = GetPetBaseColor(petType, color);
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
            Color? outlineColor = GetOutlineColor(color);
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
            Brush baseBrush = CreateBrush(baseColor);
            Brush accentBrush = CreateBrush(accentColor);
            Brush eyeBrush = CreateBrush(eyeColor);
            Brush pinkBrush = CreateBrush(Color.FromRgb(255, 180, 180));
            Brush blushBrush = CreateBrush(Color.FromArgb(80, 255, 150, 150));
            Brush whiteBrush = CreateBrush(Colors.White);

            // Get leg positions based on state and frame
            (var frontLegOffset, var backLegOffset, var bodyBob) = GetLegPositions(state, frame);

            // Chubby body (rounder, more oval)
            var bodyY = 16 * scale + bodyBob * scale;
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(16 * scale, bodyY),
                10 * scale, 7 * scale);

            // Cute belly (lighter, rounder)
            dc.DrawEllipse(accentBrush, null,
                new Point(16 * scale, (bodyY + 1 * scale)),
                6 * scale, 4 * scale);

            // Stubby front legs
            var frontLeg1Y = 21 * scale + frontLegOffset * scale;
            var frontLeg2Y = 21 * scale - frontLegOffset * scale;
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(10 * scale, frontLeg1Y + 3 * scale), 2.5 * scale, 4 * scale);
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(14 * scale, frontLeg2Y + 3 * scale), 2.5 * scale, 4 * scale);

            // Stubby back legs
            var backLeg1Y = 21 * scale - backLegOffset * scale;
            var backLeg2Y = 21 * scale + backLegOffset * scale;
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(20 * scale, backLeg1Y + 3 * scale), 2.5 * scale, 4 * scale);
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(24 * scale, backLeg2Y + 3 * scale), 2.5 * scale, 4 * scale);

            // Cute paws (little ovals)
            dc.DrawEllipse(accentBrush, null, new Point(10 * scale, frontLeg1Y + 6 * scale), 2 * scale, 1.2 * scale);
            dc.DrawEllipse(accentBrush, null, new Point(14 * scale, frontLeg2Y + 6 * scale), 2 * scale, 1.2 * scale);
            dc.DrawEllipse(accentBrush, null, new Point(20 * scale, backLeg1Y + 6 * scale), 2 * scale, 1.2 * scale);
            dc.DrawEllipse(accentBrush, null, new Point(24 * scale, backLeg2Y + 6 * scale), 2 * scale, 1.2 * scale);

            // Big round head (kawaii proportions)
            var headY = 9 * scale + bodyBob * 0.5 * scale;
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(8 * scale, headY),
                8 * scale, 7 * scale);

            // Cute triangular ears
            DrawTriangle(dc, baseBrush,
                new Point(2 * scale, (headY - 2 * scale)),
                new Point(0 * scale, (headY - 9 * scale)),
                new Point(6 * scale, (headY - 4 * scale)));
            DrawTriangle(dc, baseBrush,
                new Point(10 * scale, (headY - 4 * scale)),
                new Point(16 * scale, (headY - 9 * scale)),
                new Point(14 * scale, (headY - 2 * scale)));

            // Inner ears (pink)
            DrawTriangle(dc, pinkBrush,
                new Point(2.5 * scale, (headY - 3 * scale)),
                new Point(1.5 * scale, (headY - 7 * scale)),
                new Point(5 * scale, (headY - 4.5 * scale)));
            DrawTriangle(dc, pinkBrush,
                new Point(11 * scale, (headY - 4.5 * scale)),
                new Point(14.5 * scale, (headY - 7 * scale)),
                new Point(13.5 * scale, (headY - 3 * scale)));

            // Big cute eyes
            DrawCuteEyes(dc, scale, headY, state, eyeBrush);

            // Rosy cheeks (blush)
            dc.DrawEllipse(blushBrush, null, new Point(3 * scale, (headY + 2 * scale)), 2 * scale, 1.2 * scale);
            dc.DrawEllipse(blushBrush, null, new Point(13 * scale, (headY + 2 * scale)), 2 * scale, 1.2 * scale);

            // Tiny pink nose
            dc.DrawEllipse(pinkBrush, null,
                new Point(8 * scale, (headY + 1.5 * scale)),
                1.2 * scale, 0.8 * scale);

            // Little mouth (w shape when happy)
            var mouthPen = new Pen(eyeBrush, 0.6 * scale);
            mouthPen.Freeze();
            if (state == PetState.Happy)
            {
                // Happy "w" mouth
                dc.DrawLine(mouthPen, new Point(6 * scale, (headY + 3 * scale)), new Point(7.5 * scale, (headY + 4 * scale)));
                dc.DrawLine(mouthPen, new Point(7.5 * scale, (headY + 4 * scale)), new Point(8 * scale, (headY + 3 * scale)));
                dc.DrawLine(mouthPen, new Point(8 * scale, (headY + 3 * scale)), new Point(8.5 * scale, (headY + 4 * scale)));
                dc.DrawLine(mouthPen, new Point(8.5 * scale, (headY + 4 * scale)), new Point(10 * scale, (headY + 3 * scale)));
            }
            else if (state != PetState.Sleeping)
            {
                // Simple curved mouth
                dc.DrawLine(mouthPen, new Point(7 * scale, (headY + 3 * scale)), new Point(9 * scale, (headY + 3 * scale)));
            }

            // Whiskers (shorter, cuter)
            var whiskerPen = new Pen(CreateBrush(Color.FromRgb(100, 100, 100)), 0.4 * scale);
            whiskerPen.Freeze();
            dc.DrawLine(whiskerPen, new Point(2 * scale, (headY + 1 * scale)), new Point(5 * scale, (headY + 1.5 * scale)));
            dc.DrawLine(whiskerPen, new Point(2.5 * scale, (headY + 2.5 * scale)), new Point(5.5 * scale, (headY + 2.5 * scale)));
            dc.DrawLine(whiskerPen, new Point(11 * scale, (headY + 1.5 * scale)), new Point(14 * scale, (headY + 1 * scale)));
            dc.DrawLine(whiskerPen, new Point(10.5 * scale, (headY + 2.5 * scale)), new Point(13.5 * scale, (headY + 2.5 * scale)));

            // Fluffy tail
            DrawCatTail(dc, baseBrush, scale, bodyY, state, frame);
        }

        private void DrawCuteEyes(DrawingContext dc, double scale, double headY, PetState state, Brush eyeBrush)
        {
            Brush whiteBrush = CreateBrush(Colors.White);
            Brush highlightBrush = CreateBrush(Colors.White);

            if (state == PetState.Sleeping)
            {
                // Peaceful closed eyes (curved lines)
                var eyePen = new Pen(eyeBrush, 1 * scale);
                eyePen.Freeze();
                // Draw curved lines for closed eyes
                dc.DrawArc(eyePen, new Point(5 * scale, (headY - 0.5 * scale)), 1.5 * scale, 0, 180);
                dc.DrawArc(eyePen, new Point(11 * scale, (headY - 0.5 * scale)), 1.5 * scale, 0, 180);
            }
            else if (state == PetState.Happy)
            {
                // Super happy sparkly eyes (^ ^)
                var eyePen = new Pen(eyeBrush, 1.2 * scale);
                eyePen.Freeze();
                dc.DrawLine(eyePen, new Point(3.5 * scale, (headY + 0.5 * scale)), new Point(5 * scale, (headY - 2 * scale)));
                dc.DrawLine(eyePen, new Point(5 * scale, (headY - 2 * scale)), new Point(6.5 * scale, (headY + 0.5 * scale)));
                dc.DrawLine(eyePen, new Point(9.5 * scale, (headY + 0.5 * scale)), new Point(11 * scale, (headY - 2 * scale)));
                dc.DrawLine(eyePen, new Point(11 * scale, (headY - 2 * scale)), new Point(12.5 * scale, (headY + 0.5 * scale)));
            }
            else
            {
                // Big round sparkly eyes
                // Eye whites
                dc.DrawEllipse(whiteBrush, null, new Point(5 * scale, (headY - 0.5 * scale)), 2.8 * scale, 2.8 * scale);
                dc.DrawEllipse(whiteBrush, null, new Point(11 * scale, (headY - 0.5 * scale)), 2.8 * scale, 2.8 * scale);

                // Pupils (big and cute)
                dc.DrawEllipse(eyeBrush, null, new Point(5.3 * scale, (headY - 0.3 * scale)), 2 * scale, 2.2 * scale);
                dc.DrawEllipse(eyeBrush, null, new Point(10.7 * scale, (headY - 0.3 * scale)), 2 * scale, 2.2 * scale);

                // Sparkle highlights (makes them look alive and cute)
                dc.DrawEllipse(highlightBrush, null, new Point(4.3 * scale, (headY - 1.3 * scale)), 0.8 * scale, 0.8 * scale);
                dc.DrawEllipse(highlightBrush, null, new Point(9.7 * scale, (headY - 1.3 * scale)), 0.8 * scale, 0.8 * scale);
                // Smaller secondary highlight
                dc.DrawEllipse(highlightBrush, null, new Point(5.8 * scale, (headY + 0.5 * scale)), 0.4 * scale, 0.4 * scale);
                dc.DrawEllipse(highlightBrush, null, new Point(11.2 * scale, (headY + 0.5 * scale)), 0.4 * scale, 0.4 * scale);
            }
        }

        private void DrawCatTail(DrawingContext dc, Brush brush, double scale, double bodyY, PetState state, int frame)
        {
            var tailWag = state == PetState.Happy ? Math.Sin(frame * Math.PI) * 4 : Math.Sin(frame * Math.PI * 0.3) * 1;

            // Fluffy curved tail
            var pen = new Pen(brush, 3.5 * scale)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            pen.Freeze();

            var geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(25 * scale, bodyY - 2 * scale), false, false);
                ctx.BezierTo(
                    new Point(28 * scale, (bodyY - 6 * scale + tailWag * scale)),
                    new Point(30 * scale, (bodyY - 10 * scale + tailWag * scale)),
                    new Point(28 * scale, (bodyY - 13 * scale + tailWag * scale)),
                    true, true);
            }
            geometry.Freeze();
            dc.DrawGeometry(null, pen, geometry);
        }

        #endregion

        #region Dog Drawing

        private void DrawDog(DrawingContext dc, int size, Color baseColor, Color accentColor, Color eyeColor, PetState state, int frame, Pen outlinePen = null)
        {
            var scale = size / 32.0;
            Brush baseBrush = CreateBrush(baseColor);
            Brush accentBrush = CreateBrush(accentColor);
            Brush eyeBrush = CreateBrush(eyeColor);
            Brush noseBrush = CreateBrush(Color.FromRgb(40, 40, 40));
            Brush tongueBrush = CreateBrush(Color.FromRgb(255, 130, 150));
            Brush blushBrush = CreateBrush(Color.FromArgb(80, 255, 150, 150));
            Brush whiteBrush = CreateBrush(Colors.White);

            (var frontLegOffset, var backLegOffset, var bodyBob) = GetLegPositions(state, frame);

            // Chubby round body
            var bodyY = 16 * scale + bodyBob * scale;
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(17 * scale, bodyY),
                10 * scale, 7 * scale);

            // Cute belly patch
            dc.DrawEllipse(accentBrush, null,
                new Point(17 * scale, (bodyY + 1.5 * scale)),
                6 * scale, 4 * scale);

            // Stubby legs
            DrawCuteDogLegs(dc, baseBrush, accentBrush, scale, frontLegOffset, backLegOffset, outlinePen);

            // Big round head
            var headY = 10 * scale + bodyBob * 0.5 * scale;
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(8 * scale, headY),
                8 * scale, 7 * scale);

            // Cute snout (rounder)
            dc.DrawEllipse(accentBrush, outlinePen,
                new Point(4 * scale, (headY + 2 * scale)),
                4 * scale, 3 * scale);

            // Floppy ears (rounder, cuter)
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(1 * scale, (headY + 2 * scale)),
                3 * scale, 6 * scale);
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(14 * scale, (headY + 2 * scale)),
                3 * scale, 6 * scale);

            // Big cute eyes
            DrawCuteDogEyes(dc, scale, headY, state, eyeBrush);

            // Rosy cheeks
            dc.DrawEllipse(blushBrush, null, new Point(3 * scale, (headY + 3 * scale)), 2 * scale, 1.2 * scale);
            dc.DrawEllipse(blushBrush, null, new Point(13 * scale, (headY + 3 * scale)), 2 * scale, 1.2 * scale);

            // Cute round nose
            dc.DrawEllipse(noseBrush, null,
                new Point(2 * scale, (headY + 1.5 * scale)),
                1.8 * scale, 1.3 * scale);
            // Nose shine
            dc.DrawEllipse(whiteBrush, null,
                new Point(1.5 * scale, (headY + 1 * scale)),
                0.5 * scale, 0.4 * scale);

            // Happy mouth/tongue
            var mouthPen = new Pen(eyeBrush, 0.6 * scale);
            mouthPen.Freeze();
            if (state == PetState.Happy || state == PetState.Running)
            {
                // Open mouth with tongue
                dc.DrawEllipse(tongueBrush, null,
                    new Point(3 * scale, (headY + 5 * scale)),
                    2.5 * scale, 3 * scale);
                // Cute "w" smile
                dc.DrawLine(mouthPen, new Point(1.5 * scale, (headY + 3.5 * scale)), new Point(3 * scale, (headY + 4.5 * scale)));
                dc.DrawLine(mouthPen, new Point(3 * scale, (headY + 4.5 * scale)), new Point(4.5 * scale, (headY + 3.5 * scale)));
            }
            else if (state != PetState.Sleeping)
            {
                // Simple smile
                dc.DrawArc(mouthPen, new Point(3 * scale, (headY + 3.5 * scale)), 1.5 * scale, 200, 140);
            }

            // Wagging tail
            DrawCuteDogTail(dc, baseBrush, scale, bodyY, state, frame);
        }

        private void DrawCuteDogLegs(DrawingContext dc, Brush baseBrush, Brush accentBrush, double scale, double frontOffset, double backOffset, Pen outlinePen = null)
        {
            // Stubby front legs
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(10 * scale, (22 + frontOffset) * scale), 2.5 * scale, 4 * scale);
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(14 * scale, (22 - frontOffset) * scale), 2.5 * scale, 4 * scale);

            // Stubby back legs
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(21 * scale, (22 - backOffset) * scale), 2.5 * scale, 4 * scale);
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(25 * scale, (22 + backOffset) * scale), 2.5 * scale, 4 * scale);

            // Cute round paws
            dc.DrawEllipse(accentBrush, null, new Point(10 * scale, (26 + frontOffset) * scale), 2.2 * scale, 1.3 * scale);
            dc.DrawEllipse(accentBrush, null, new Point(14 * scale, (26 - frontOffset) * scale), 2.2 * scale, 1.3 * scale);
            dc.DrawEllipse(accentBrush, null, new Point(21 * scale, (26 - backOffset) * scale), 2.2 * scale, 1.3 * scale);
            dc.DrawEllipse(accentBrush, null, new Point(25 * scale, (26 + backOffset) * scale), 2.2 * scale, 1.3 * scale);
        }

        private void DrawCuteDogEyes(DrawingContext dc, double scale, double headY, PetState state, Brush eyeBrush)
        {
            Brush whiteBrush = CreateBrush(Colors.White);

            if (state == PetState.Sleeping)
            {
                var eyePen = new Pen(eyeBrush, 1 * scale);
                eyePen.Freeze();
                dc.DrawArc(eyePen, new Point(5 * scale, (headY - 0.5 * scale)), 1.5 * scale, 0, 180);
                dc.DrawArc(eyePen, new Point(11 * scale, (headY - 0.5 * scale)), 1.5 * scale, 0, 180);
            }
            else if (state == PetState.Happy)
            {
                // Super happy sparkly eyes
                var eyePen = new Pen(eyeBrush, 1.2 * scale);
                eyePen.Freeze();
                dc.DrawLine(eyePen, new Point(3.5 * scale, (headY + 0.5 * scale)), new Point(5 * scale, (headY - 2 * scale)));
                dc.DrawLine(eyePen, new Point(5 * scale, (headY - 2 * scale)), new Point(6.5 * scale, (headY + 0.5 * scale)));
                dc.DrawLine(eyePen, new Point(9.5 * scale, (headY + 0.5 * scale)), new Point(11 * scale, (headY - 2 * scale)));
                dc.DrawLine(eyePen, new Point(11 * scale, (headY - 2 * scale)), new Point(12.5 * scale, (headY + 0.5 * scale)));
            }
            else
            {
                // Big round puppy eyes
                dc.DrawEllipse(whiteBrush, null, new Point(5 * scale, (headY - 0.5 * scale)), 3 * scale, 3 * scale);
                dc.DrawEllipse(whiteBrush, null, new Point(11 * scale, (headY - 0.5 * scale)), 3 * scale, 3 * scale);
                dc.DrawEllipse(eyeBrush, null, new Point(5.3 * scale, (headY - 0.2 * scale)), 2 * scale, 2.2 * scale);
                dc.DrawEllipse(eyeBrush, null, new Point(10.7 * scale, (headY - 0.2 * scale)), 2 * scale, 2.2 * scale);
                // Sparkle highlights
                dc.DrawEllipse(whiteBrush, null, new Point(4.3 * scale, (headY - 1.2 * scale)), 0.9 * scale, 0.9 * scale);
                dc.DrawEllipse(whiteBrush, null, new Point(9.7 * scale, (headY - 1.2 * scale)), 0.9 * scale, 0.9 * scale);
                dc.DrawEllipse(whiteBrush, null, new Point(5.8 * scale, (headY + 0.5 * scale)), 0.4 * scale, 0.4 * scale);
                dc.DrawEllipse(whiteBrush, null, new Point(11.2 * scale, (headY + 0.5 * scale)), 0.4 * scale, 0.4 * scale);
            }
        }

        private void DrawCuteDogTail(DrawingContext dc, Brush brush, double scale, double bodyY, PetState state, int frame)
        {
            // Dogs wag tail enthusiastically
            var wagSpeed = state == PetState.Happy ? 2.5 : (state == PetState.Walking || state == PetState.Running ? 1.5 : 0.3);
            var wagAmount = Math.Sin(frame * Math.PI * wagSpeed) * 5;

            var pen = new Pen(brush, 4 * scale)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            pen.Freeze();

            // Curved wagging tail
            var geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(26 * scale, (bodyY - 3 * scale)), false, false);
                ctx.BezierTo(
                    new Point(28 * scale, (bodyY - 6 * scale + wagAmount * scale * 0.5)),
                    new Point(30 * scale, (bodyY - 9 * scale + wagAmount * scale)),
                    new Point(29 * scale, (bodyY - 12 * scale + wagAmount * scale)),
                    true, true);
            }
            geometry.Freeze();
            dc.DrawGeometry(null, pen, geometry);
        }

        #endregion

        #region Fox Drawing

        private void DrawFox(DrawingContext dc, int size, Color baseColor, Color accentColor, Color eyeColor, PetState state, int frame, Pen outlinePen = null)
        {
            var scale = size / 32.0;
            Brush baseBrush = CreateBrush(baseColor);
            Brush accentBrush = CreateBrush(accentColor);
            Brush whiteBrush = CreateBrush(Colors.White);
            Brush eyeBrush = CreateBrush(eyeColor);
            Brush noseBrush = CreateBrush(Color.FromRgb(40, 40, 40));
            Brush blushBrush = CreateBrush(Color.FromArgb(80, 255, 150, 150));

            (var frontLegOffset, var backLegOffset, var bodyBob) = GetLegPositions(state, frame);

            // Chubby fluffy body
            var bodyY = 16 * scale + bodyBob * scale;
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(17 * scale, bodyY),
                10 * scale, 7 * scale);

            // White belly/chest patch
            dc.DrawEllipse(whiteBrush, null,
                new Point(15 * scale, (bodyY + 1 * scale)),
                6 * scale, 4 * scale);

            // Stubby legs with dark socks
            DrawCuteFoxLegs(dc, baseBrush, scale, frontLegOffset, backLegOffset, outlinePen);

            // Big round head
            var headY = 10 * scale + bodyBob * 0.5 * scale;
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(7 * scale, headY),
                7 * scale, 6 * scale);

            // White face markings (larger, cuter)
            dc.DrawEllipse(whiteBrush, null,
                new Point(6 * scale, (headY + 1.5 * scale)),
                4 * scale, 3.5 * scale);

            // Cute pointed snout (smaller)
            dc.DrawEllipse(whiteBrush, null,
                new Point(2 * scale, (headY + 2 * scale)),
                3 * scale, 2 * scale);

            // Big fluffy ears
            DrawTriangle(dc, baseBrush,
                new Point(2 * scale, (headY - 2 * scale)),
                new Point(-1 * scale, (headY - 10 * scale)),
                new Point(6 * scale, (headY - 4 * scale)));
            DrawTriangle(dc, baseBrush,
                new Point(9 * scale, (headY - 4 * scale)),
                new Point(15 * scale, (headY - 10 * scale)),
                new Point(12 * scale, (headY - 2 * scale)));

            // Inner ears (dark/pink)
            Brush innerEarBrush = CreateBrush(Color.FromRgb(80, 40, 20));
            DrawTriangle(dc, innerEarBrush,
                new Point(2.5 * scale, (headY - 3 * scale)),
                new Point(0.5 * scale, (headY - 7.5 * scale)),
                new Point(5 * scale, (headY - 4.5 * scale)));
            DrawTriangle(dc, innerEarBrush,
                new Point(9.5 * scale, (headY - 4.5 * scale)),
                new Point(13.5 * scale, (headY - 7.5 * scale)),
                new Point(11.5 * scale, (headY - 3 * scale)));

            // Big cute eyes
            DrawCuteFoxEyes(dc, scale, headY, state, eyeBrush);

            // Rosy cheeks
            dc.DrawEllipse(blushBrush, null, new Point(3 * scale, (headY + 3 * scale)), 1.8 * scale, 1.2 * scale);
            dc.DrawEllipse(blushBrush, null, new Point(11 * scale, (headY + 3 * scale)), 1.8 * scale, 1.2 * scale);

            // Cute button nose
            dc.DrawEllipse(noseBrush, null,
                new Point(1 * scale, (headY + 1.5 * scale)),
                1.3 * scale, 1 * scale);
            // Nose shine
            dc.DrawEllipse(whiteBrush, null,
                new Point(0.5 * scale, (headY + 1 * scale)),
                0.4 * scale, 0.3 * scale);

            // Little smile
            var mouthPen = new Pen(eyeBrush, 0.5 * scale);
            mouthPen.Freeze();
            if (state == PetState.Happy)
            {
                dc.DrawLine(mouthPen, new Point(0 * scale, (headY + 3 * scale)), new Point(1.5 * scale, (headY + 4 * scale)));
                dc.DrawLine(mouthPen, new Point(1.5 * scale, (headY + 4 * scale)), new Point(3 * scale, (headY + 3 * scale)));
            }

            // Super fluffy tail with white tip
            DrawCuteFoxTail(dc, baseBrush, whiteBrush, scale, bodyY, state, frame);
        }

        private void DrawCuteFoxLegs(DrawingContext dc, Brush baseBrush, double scale, double frontOffset, double backOffset, Pen outlinePen = null)
        {
            Brush sockBrush = CreateBrush(Color.FromRgb(50, 40, 35));

            // Stubby front legs
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(10 * scale, (22 + frontOffset) * scale), 2.3 * scale, 4 * scale);
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(14 * scale, (22 - frontOffset) * scale), 2.3 * scale, 4 * scale);

            // Stubby back legs
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(21 * scale, (22 - backOffset) * scale), 2.3 * scale, 4 * scale);
            dc.DrawEllipse(baseBrush, outlinePen,
                new Point(25 * scale, (22 + backOffset) * scale), 2.3 * scale, 4 * scale);

            // Dark "socks" (paws)
            dc.DrawEllipse(sockBrush, null, new Point(10 * scale, (26 + frontOffset) * scale), 2 * scale, 1.3 * scale);
            dc.DrawEllipse(sockBrush, null, new Point(14 * scale, (26 - frontOffset) * scale), 2 * scale, 1.3 * scale);
            dc.DrawEllipse(sockBrush, null, new Point(21 * scale, (26 - backOffset) * scale), 2 * scale, 1.3 * scale);
            dc.DrawEllipse(sockBrush, null, new Point(25 * scale, (26 + backOffset) * scale), 2 * scale, 1.3 * scale);
        }

        private void DrawCuteFoxEyes(DrawingContext dc, double scale, double headY, PetState state, Brush eyeBrush)
        {
            Brush whiteBrush = CreateBrush(Colors.White);
            Brush amberBrush = CreateBrush(Color.FromRgb(255, 190, 50));

            if (state == PetState.Sleeping)
            {
                var eyePen = new Pen(eyeBrush, 1 * scale);
                eyePen.Freeze();
                dc.DrawArc(eyePen, new Point(4.5 * scale, (headY - 0.5 * scale)), 1.5 * scale, 0, 180);
                dc.DrawArc(eyePen, new Point(9.5 * scale, (headY - 0.5 * scale)), 1.5 * scale, 0, 180);
            }
            else if (state == PetState.Happy)
            {
                // Sly happy fox eyes (^ ^)
                var eyePen = new Pen(eyeBrush, 1.2 * scale);
                eyePen.Freeze();
                dc.DrawLine(eyePen, new Point(3 * scale, (headY + 0.5 * scale)), new Point(4.5 * scale, (headY - 2 * scale)));
                dc.DrawLine(eyePen, new Point(4.5 * scale, (headY - 2 * scale)), new Point(6 * scale, (headY + 0.5 * scale)));
                dc.DrawLine(eyePen, new Point(8 * scale, (headY + 0.5 * scale)), new Point(9.5 * scale, (headY - 2 * scale)));
                dc.DrawLine(eyePen, new Point(9.5 * scale, (headY - 2 * scale)), new Point(11 * scale, (headY + 0.5 * scale)));
            }
            else
            {
                // Big sparkly amber eyes
                dc.DrawEllipse(amberBrush, null, new Point(4.5 * scale, (headY - 0.5 * scale)), 2.5 * scale, 2.5 * scale);
                dc.DrawEllipse(amberBrush, null, new Point(9.5 * scale, (headY - 0.5 * scale)), 2.5 * scale, 2.5 * scale);
                dc.DrawEllipse(eyeBrush, null, new Point(4.8 * scale, (headY - 0.2 * scale)), 1.5 * scale, 1.8 * scale);
                dc.DrawEllipse(eyeBrush, null, new Point(9.2 * scale, (headY - 0.2 * scale)), 1.5 * scale, 1.8 * scale);
                // Sparkle highlights
                dc.DrawEllipse(whiteBrush, null, new Point(3.8 * scale, (headY - 1.2 * scale)), 0.8 * scale, 0.8 * scale);
                dc.DrawEllipse(whiteBrush, null, new Point(8.2 * scale, (headY - 1.2 * scale)), 0.8 * scale, 0.8 * scale);
                dc.DrawEllipse(whiteBrush, null, new Point(5.2 * scale, (headY + 0.5 * scale)), 0.35 * scale, 0.35 * scale);
                dc.DrawEllipse(whiteBrush, null, new Point(9.8 * scale, (headY + 0.5 * scale)), 0.35 * scale, 0.35 * scale);
            }
        }

        private void DrawCuteFoxTail(DrawingContext dc, Brush baseBrush, Brush whiteBrush, double scale, double bodyY, PetState state, int frame)
        {
            var wagAmount = state == PetState.Happy ? Math.Sin(frame * Math.PI) * 4 : Math.Sin(frame * Math.PI * 0.4) * 1.5;

            // Extra fluffy tail
            var tailGeometry = new StreamGeometry();
            using (StreamGeometryContext ctx = tailGeometry.Open())
            {
                ctx.BeginFigure(new Point(26 * scale, (bodyY - 1 * scale)), true, true);
                ctx.BezierTo(
                    new Point(29 * scale, (bodyY - 5 * scale + wagAmount * scale)),
                    new Point(33 * scale, (bodyY - 9 * scale + wagAmount * scale)),
                    new Point(31 * scale, (bodyY - 14 * scale + wagAmount * scale)),
                    true, true);
                ctx.BezierTo(
                    new Point(29 * scale, (bodyY - 12 * scale + wagAmount * scale)),
                    new Point(25 * scale, (bodyY - 5 * scale)),
                    new Point(26 * scale, (bodyY - 1 * scale)),
                    true, true);
            }
            tailGeometry.Freeze();
            dc.DrawGeometry(baseBrush, null, tailGeometry);

            // Big white fluffy tail tip
            dc.DrawEllipse(whiteBrush, null,
                new Point(31 * scale, (bodyY - 13 * scale + wagAmount * scale)),
                3 * scale, 4 * scale);
        }

        #endregion

        #region Other Pets

        private void DrawClippy(DrawingContext dc, int size, PetState state, int frame)
        {
            var scale = size / 32.0;

            // Classic paperclip silver/gray color
            Brush wireBrush = CreateBrush(Color.FromRgb(160, 165, 175));
            Brush wireHighlight = CreateBrush(Color.FromRgb(200, 205, 215));
            Brush eyeBrush = CreateBrush(Colors.Black);
            Brush eyeWhiteBrush = CreateBrush(Colors.White);

            var bodyBob = state == PetState.Walking || state == PetState.Running ? Math.Sin(frame * Math.PI) * 1.5 : 0;

            // Wire pen for the paperclip body
            var wirePen = new Pen(wireBrush, 3 * scale)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            wirePen.Freeze();

            // Highlight pen
            var highlightPen = new Pen(wireHighlight, 1.5 * scale)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            highlightPen.Freeze();

            // Draw classic paperclip shape - single bent wire
            // The shape is like: bottom straight up, curve at top, back down, small curve, back up a bit
            var clipGeometry = new StreamGeometry();
            using (StreamGeometryContext ctx = clipGeometry.Open())
            {
                // Start at bottom-left leg
                ctx.BeginFigure(new Point(10 * scale, (28 + bodyBob) * scale), false, false);

                // Go up the left side
                ctx.LineTo(new Point(10 * scale, (10 + bodyBob) * scale), true, true);

                // Curve around the top (big loop)
                ctx.BezierTo(
                    new Point(10 * scale, (4 + bodyBob) * scale),
                    new Point(22 * scale, (4 + bodyBob) * scale),
                    new Point(22 * scale, (10 + bodyBob) * scale),
                    true, true);

                // Go down the right side
                ctx.LineTo(new Point(22 * scale, (20 + bodyBob) * scale), true, true);

                // Small inner curve
                ctx.BezierTo(
                    new Point(22 * scale, (24 + bodyBob) * scale),
                    new Point(14 * scale, (24 + bodyBob) * scale),
                    new Point(14 * scale, (20 + bodyBob) * scale),
                    true, true);

                // Go back up a bit (inner part)
                ctx.LineTo(new Point(14 * scale, (14 + bodyBob) * scale), true, true);
            }
            clipGeometry.Freeze();
            dc.DrawGeometry(null, wirePen, clipGeometry);

            // Draw highlight on left edge
            dc.DrawLine(highlightPen,
                new Point(9 * scale, (26 + bodyBob) * scale),
                new Point(9 * scale, (12 + bodyBob) * scale));

            // Big googly eyes - the signature Clippy look!
            // Position eyes on the upper curve area
            var eyeY = (8 + bodyBob) * scale;
            var leftEyeX = 13 * scale;
            var rightEyeX = 19 * scale;

            // Large white eye backgrounds
            dc.DrawEllipse(eyeWhiteBrush, null, new Point(leftEyeX, eyeY), 3.5 * scale, 4 * scale);
            dc.DrawEllipse(eyeWhiteBrush, null, new Point(rightEyeX, eyeY), 3.5 * scale, 4 * scale);

            // Thin outline around eyes
            var eyeOutlinePen = new Pen(CreateBrush(Color.FromRgb(100, 100, 110)), 0.5 * scale);
            eyeOutlinePen.Freeze();
            dc.DrawEllipse(null, eyeOutlinePen, new Point(leftEyeX, eyeY), 3.5 * scale, 4 * scale);
            dc.DrawEllipse(null, eyeOutlinePen, new Point(rightEyeX, eyeY), 3.5 * scale, 4 * scale);

            if (state == PetState.Happy)
            {
                // Happy curved eyes
                var eyePen = new Pen(eyeBrush, 1.5 * scale);
                eyePen.Freeze();
                dc.DrawArc(eyePen, new Point(leftEyeX, (eyeY - 0.5 * scale)), 2 * scale, 200, 140);
                dc.DrawArc(eyePen, new Point(rightEyeX, (eyeY - 0.5 * scale)), 2 * scale, 200, 140);
            }
            else if (state == PetState.Sleeping)
            {
                // Closed eyes
                var eyePen = new Pen(eyeBrush, 1 * scale);
                eyePen.Freeze();
                dc.DrawLine(eyePen, new Point((leftEyeX - 2 * scale), eyeY), new Point((leftEyeX + 2 * scale), eyeY));
                dc.DrawLine(eyePen, new Point((rightEyeX - 2 * scale), eyeY), new Point((rightEyeX + 2 * scale), eyeY));
            }
            else
            {
                // Normal big pupils - looking slightly to the side
                var pupilOffsetX = 0.3 * scale;
                var pupilOffsetY = 0.5 * scale;
                dc.DrawEllipse(eyeBrush, null, new Point(leftEyeX + pupilOffsetX, eyeY + pupilOffsetY), 1.8 * scale, 2.2 * scale);
                dc.DrawEllipse(eyeBrush, null, new Point(rightEyeX + pupilOffsetX, eyeY + pupilOffsetY), 1.8 * scale, 2.2 * scale);

                // Eye shine/sparkle
                dc.DrawEllipse(eyeWhiteBrush, null, new Point(leftEyeX - 0.5 * scale, eyeY - 1 * scale), 0.8 * scale, 0.8 * scale);
                dc.DrawEllipse(eyeWhiteBrush, null, new Point(rightEyeX - 0.5 * scale, eyeY - 1 * scale), 0.8 * scale, 0.8 * scale);
            }

            // Eyebrows (raised, friendly)
            var browPen = new Pen(eyeBrush, 0.8 * scale);
            browPen.Freeze();
            dc.DrawLine(browPen, new Point((leftEyeX - 2.5 * scale), (eyeY - 5 * scale)), new Point((leftEyeX + 2 * scale), (eyeY - 4.5 * scale)));
            dc.DrawLine(browPen, new Point((rightEyeX - 2 * scale), (eyeY - 4.5 * scale)), new Point((rightEyeX + 2.5 * scale), (eyeY - 5 * scale)));
        }

        private void DrawRubberDuck(DrawingContext dc, int size, PetState state, int frame)
        {
            var scale = size / 32.0;
            Brush yellowBrush = CreateBrush(Color.FromRgb(255, 230, 80));
            Brush lightYellowBrush = CreateBrush(Color.FromRgb(255, 245, 150));
            Brush orangeBrush = CreateBrush(Color.FromRgb(255, 160, 50));
            Brush eyeBrush = CreateBrush(Colors.Black);
            Brush whiteBrush = CreateBrush(Colors.White);
            Brush blushBrush = CreateBrush(Color.FromArgb(80, 255, 150, 150));

            var bodyBob = state == PetState.Walking ? Math.Sin(frame * Math.PI) * 1.5 : 0;
            var waddle = state == PetState.Walking ? Math.Sin(frame * Math.PI * 2) * 2 : 0;

            // Chubby body
            dc.DrawEllipse(yellowBrush, null,
                new Point((16 + waddle) * scale, (18 + bodyBob) * scale),
                11 * scale, 9 * scale);

            // Body highlight
            dc.DrawEllipse(lightYellowBrush, null,
                new Point((14 + waddle) * scale, (16 + bodyBob) * scale),
                5 * scale, 4 * scale);

            // Big round head
            dc.DrawEllipse(yellowBrush, null,
                new Point((10 + waddle) * scale, (9 + bodyBob) * scale),
                8 * scale, 7 * scale);

            // Head highlight
            dc.DrawEllipse(lightYellowBrush, null,
                new Point((8 + waddle) * scale, (7 + bodyBob) * scale),
                3 * scale, 2.5 * scale);

            // Cute beak (rounder)
            dc.DrawEllipse(orangeBrush, null,
                new Point((3 + waddle) * scale, (11 + bodyBob) * scale),
                4 * scale, 2.5 * scale);
            // Beak highlight
            dc.DrawEllipse(CreateBrush(Color.FromRgb(255, 200, 100)), null,
                new Point((2 + waddle) * scale, (10 + bodyBob) * scale),
                1.5 * scale, 1 * scale);

            // Big cute eyes
            if (state == PetState.Happy)
            {
                var eyePen = new Pen(eyeBrush, 1.2 * scale);
                eyePen.Freeze();
                dc.DrawLine(eyePen, new Point((7 + waddle) * scale, (8 + bodyBob) * scale),
                    new Point((9 + waddle) * scale, (5.5 + bodyBob) * scale));
                dc.DrawLine(eyePen, new Point((9 + waddle) * scale, (5.5 + bodyBob) * scale),
                    new Point((11 + waddle) * scale, (8 + bodyBob) * scale));
            }
            else if (state == PetState.Sleeping)
            {
                var eyePen = new Pen(eyeBrush, 1 * scale);
                eyePen.Freeze();
                dc.DrawArc(eyePen, new Point((9 + waddle) * scale, (7 + bodyBob) * scale), 1.5 * scale, 0, 180);
            }
            else
            {
                // Big sparkly eye
                dc.DrawEllipse(whiteBrush, null,
                    new Point((9 + waddle) * scale, (7 + bodyBob) * scale),
                    2.5 * scale, 2.5 * scale);
                dc.DrawEllipse(eyeBrush, null,
                    new Point((9.3 + waddle) * scale, (7.2 + bodyBob) * scale),
                    1.8 * scale, 1.8 * scale);
                // Sparkle
                dc.DrawEllipse(whiteBrush, null,
                    new Point((8.3 + waddle) * scale, (6.2 + bodyBob) * scale),
                    0.7 * scale, 0.7 * scale);
            }

            // Rosy cheek
            dc.DrawEllipse(blushBrush, null,
                new Point((12 + waddle) * scale, (10 + bodyBob) * scale),
                1.8 * scale, 1.2 * scale);

            // Cute little wing
            dc.DrawEllipse(yellowBrush, null,
                new Point((19 + waddle) * scale, (16 + bodyBob) * scale),
                4.5 * scale, 5.5 * scale);
            dc.DrawEllipse(lightYellowBrush, null,
                new Point((18 + waddle) * scale, (15 + bodyBob) * scale),
                2 * scale, 2.5 * scale);

            // Cute tail feathers (rounder)
            dc.DrawEllipse(yellowBrush, null,
                new Point((27 + waddle) * scale, (17 + bodyBob) * scale),
                3 * scale, 4 * scale);
        }

        private void DrawGenericPet(DrawingContext dc, int size, Color baseColor, PetState state, int frame)
        {
            var scale = size / 32.0;
            Brush baseBrush = CreateBrush(baseColor);
            Brush eyeBrush = CreateBrush(Colors.Black);

            (var _, var _, var bodyBob) = GetLegPositions(state, frame);

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
            using (StreamGeometryContext ctx = geometry.Open())
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
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(start, false, false);
                ctx.ArcTo(end, new Size(radius, radius), 0, sweepAngle > 180, SweepDirection.Counterclockwise, true, false);
            }
            geometry.Freeze();
            dc.DrawGeometry(null, pen, geometry);
        }
    }
}
