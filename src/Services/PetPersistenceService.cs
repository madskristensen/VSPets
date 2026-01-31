using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static readonly string DataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VSPets");

        private static readonly string PetsFile = Path.Combine(DataFolder, "pets.json");

        /// <summary>
        /// Saves the current pet collection to disk.
        /// </summary>
        public static async Task SavePetsAsync(IEnumerable<PetData> pets)
        {
            try
            {
                if (!Directory.Exists(DataFolder))
                {
                    Directory.CreateDirectory(DataFolder);
                }

                var json = JsonConvert.SerializeObject(pets.ToList(), Formatting.Indented);
                
                using (var writer = new StreamWriter(PetsFile, false))
                {
                    await writer.WriteAsync(json);
                }

                System.Diagnostics.Debug.WriteLine($"VSPets: Saved {pets.Count()} pets to disk");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VSPets: Failed to save pets: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads saved pets from disk.
        /// </summary>
        public static async Task<List<PetData>> LoadPetsAsync()
        {
            try
            {
                if (!File.Exists(PetsFile))
                {
                    return new List<PetData>();
                }

                using (var reader = new StreamReader(PetsFile))
                {
                    var json = await reader.ReadToEndAsync();
                    List<PetData> pets = JsonConvert.DeserializeObject<List<PetData>>(json);
                    
                    System.Diagnostics.Debug.WriteLine($"VSPets: Loaded {pets?.Count ?? 0} pets from disk");
                    return pets ?? new List<PetData>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VSPets: Failed to load pets: {ex.Message}");
                return new List<PetData>();
            }
        }

        /// <summary>
        /// Clears saved pet data.
        /// </summary>
        public static void ClearSavedPets()
        {
            try
            {
                if (File.Exists(PetsFile))
                {
                    File.Delete(PetsFile);
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
