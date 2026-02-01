using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VSPets.Models;

namespace VSPets.Services
{
    /// <summary>
    /// Handles saving and loading pet data across VS sessions.
    /// </summary>
    public class PetPersistenceService
    {
        private static readonly string _dataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VSPets");

        private static readonly string _petsFile = Path.Combine(_dataFolder, "pets.json");

        /// <summary>
        /// Saves the current pet collection to disk.
        /// </summary>
        public static async Task SavePetsAsync(IEnumerable<PetData> pets)
        {
            try
            {
                if (!Directory.Exists(_dataFolder))
                {
                    Directory.CreateDirectory(_dataFolder);
                }

                // Materialize once to avoid double enumeration
                List<PetData> petList = [.. pets];
                var json = JsonConvert.SerializeObject(petList, Formatting.Indented);

                using (var writer = new StreamWriter(_petsFile, false))
                {
                    await writer.WriteAsync(json);
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
            }
        }

        /// <summary>
        /// Loads saved pets from disk.
        /// </summary>
        public static async Task<List<PetData>> LoadPetsAsync()
        {
            try
            {
                if (!File.Exists(_petsFile))
                {
                    return [];
                }

                using (var reader = new StreamReader(_petsFile))
                {
                    var json = await reader.ReadToEndAsync();
                    List<PetData> pets = JsonConvert.DeserializeObject<List<PetData>>(json);

                    System.Diagnostics.Debug.WriteLine($"VSPets: Loaded {pets?.Count ?? 0} pets from disk");
                    return pets ?? [];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VSPets: Failed to load pets: {ex.Message}");
                return [];
            }
        }

        /// <summary>
        /// Clears saved pet data.
        /// </summary>
        public static void ClearSavedPets()
        {
            try
            {
                if (File.Exists(_petsFile))
                {
                    File.Delete(_petsFile);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VSPets: Failed to clear pets: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Serializable data for a pet.
    /// </summary>
    public class PetData
    {
        public string Name { get; set; }
        public PetType PetType { get; set; }
        public PetColor Color { get; set; }
        public PetSize Size { get; set; }
    }
}
