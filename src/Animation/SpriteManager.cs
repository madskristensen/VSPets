using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using VSPets.Models;

namespace VSPets.Animation
{
    /// <summary>
    /// Manages loading and caching of pet sprite animations.
    /// </summary>
    public class SpriteManager
    {
        private static readonly Lazy<SpriteManager> _instance = 
            new(() => new SpriteManager());

        private readonly Dictionary<string, BitmapImage> _spriteCache = 
            new(StringComparer.OrdinalIgnoreCase);

        private readonly object _cacheLock = new();

        /// <summary>
        /// Gets the singleton instance of the sprite manager.
        /// </summary>
        public static SpriteManager Instance => _instance.Value;

        private SpriteManager()
        {
        }

        /// <summary>
        /// Gets the sprite image for a given pet type, color, and animation state.
        /// </summary>
        /// <param name="petType">Type of pet.</param>
        /// <param name="color">Pet color.</param>
        /// <param name="animationName">Animation name (e.g., "idle", "walk").</param>
        /// <returns>The loaded sprite image, or null if not found.</returns>
        public BitmapImage GetSprite(PetType petType, PetColor color, string animationName)
        {
            var key = GetCacheKey(petType, color, animationName);

            lock (_cacheLock)
            {
                if (_spriteCache.TryGetValue(key, out BitmapImage cached))
                {
                    return cached;
                }

                BitmapImage sprite = LoadSprite(petType, color, animationName);
                if (sprite != null)
                {
                    _spriteCache[key] = sprite;
                }

                return sprite;
            }
        }

        /// <summary>
        /// Preloads all sprites for a given pet type and color.
        /// </summary>
        public void PreloadSprites(PetType petType, PetColor color)
        {
            var animations = GetAnimationNames();
            foreach (var animation in animations)
            {
                GetSprite(petType, color, animation);
            }
        }

        /// <summary>
        /// Clears the sprite cache.
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _spriteCache.Clear();
            }
        }

        private BitmapImage LoadSprite(PetType petType, PetColor color, string animationName)
        {
            var petTypeName = petType.ToString().ToLowerInvariant();
            var colorName = GetColorFileName(color);

            // Try embedded resource first
            var resourcePath = $"Resources/Pets/{petTypeName}/{colorName}_{animationName}_8fps.gif";
            
            try
            {
                var uri = new Uri($"pack://application:,,,/VSPets;component/{resourcePath}", UriKind.Absolute);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Make it thread-safe
                return bitmap;
            }
            catch
            {
                // Resource not found, try alternative paths or return null
                return TryLoadFallbackSprite(petType, animationName);
            }
        }

        private BitmapImage TryLoadFallbackSprite(PetType petType, string animationName)
        {
            // Try to load a default/fallback sprite
            // This could be a simple colored shape or a generic pet sprite
            try
            {
                var uri = new Uri($"pack://application:,,,/VSPets;component/Resources/Pets/default_{animationName}.gif", UriKind.Absolute);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private string GetColorFileName(PetColor color)
        {
            return color switch
            {
                PetColor.Black => "black",
                PetColor.White => "white",
                PetColor.Brown => "brown",
                PetColor.Gray => "gray",
                PetColor.Orange => "orange",
                PetColor.LightBrown => "lightbrown",
                PetColor.Red => "red",
                PetColor.Akita => "akita",
                PetColor.Yellow => "yellow",
                PetColor.Original => "original",
                _ => "brown"
            };
        }

        private string GetCacheKey(PetType petType, PetColor color, string animationName)
        {
            return $"{petType}_{color}_{animationName}";
        }

        private static string[] GetAnimationNames()
        {
            return new[]
            {
                "idle",
                "walk",
                "walk_fast",
                "run",
                "lie",
                "swipe",
                "wallclimb",
                "wallgrab",
                "fall_from_grab",
                "land",
                "with_ball"
            };
        }

        /// <summary>
        /// Gets the expected sprite URI for a pet configuration.
        /// Useful for debugging or validation.
        /// </summary>
        public string GetSpriteUri(PetType petType, PetColor color, string animationName)
        {
            var petTypeName = petType.ToString().ToLowerInvariant();
            var colorName = GetColorFileName(color);
            return $"Resources/Pets/{petTypeName}/{colorName}_{animationName}_8fps.gif";
        }

        /// <summary>
        /// Checks if a sprite exists for the given configuration.
        /// </summary>
        public bool SpriteExists(PetType petType, PetColor color, string animationName)
        {
            BitmapImage sprite = GetSprite(petType, color, animationName);
            return sprite != null;
        }
    }
}
