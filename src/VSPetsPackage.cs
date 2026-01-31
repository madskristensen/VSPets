global using System;
global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using Task = System.Threading.Tasks.Task;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using VSPets.Options;
using VSPets.Pets;
using VSPets.Services;

namespace VSPets
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.VSPetsString)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(OptionsProvider.GeneralOptions), Vsix.Name, "General", 0, 0, true, SupportsProfiles = true)]
    public sealed class VSPetsPackage : ToolkitPackage
    {
        private DTE2 _dte;
        private EnvDTE.BuildEvents _buildEvents;
        private readonly Random _startupRandom = new();

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.RegisterCommandsAsync();

            // Initialize the pet manager after VS is fully loaded
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Delay initialization slightly to ensure VS UI is ready
            await Task.Delay(2000, cancellationToken);

            try
            {
                // Load settings
                General settings = await General.GetLiveInstanceAsync();
                PetManager.Instance.MaxPets = Math.Max(1, Math.Min(settings.MaxPets, 10));

                await PetManager.Instance.InitializeAsync();

                // Subscribe to build events
                _dte = await VS.GetServiceAsync<DTE, DTE2>();
                if (_dte != null)
                {
                    _buildEvents = _dte.Events.BuildEvents;
                    _buildEvents.OnBuildDone += OnBuildDone;
                }

                // Try to restore saved pets if enabled
                if (settings.RememberPets)
                {
                    List<PetData> savedPets = await PetPersistenceService.LoadPetsAsync();
                    if (savedPets.Any())
                    {
                        // Stagger pet spawns to avoid overlap
                        foreach (PetData petData in savedPets)
                        {
                            await PetManager.Instance.AddPetAsync(petData.PetType, petData.Color, petData.Name);

                            // Wait before spawning next pet (except for the last one)
                            if (petData != savedPets.Last())
                            {
                                // Randomize spacing between pets to feel natural
                                var spawnDelay = _startupRandom.Next(4000, 10001); // 4-10 seconds
                                await Task.Delay(spawnDelay, cancellationToken);
                            }
                        }
                        System.Diagnostics.Debug.WriteLine($"VSPets: Restored {savedPets.Count} pets");
                    }
                    else if (settings.AutoSpawnOnStartup && PetManager.Instance.PetCount == 0)
                    {
                        // No saved pets, spawn a default one if enabled
                        await PetManager.Instance.AddRandomPetAsync();
                    }
                }
                else if (settings.AutoSpawnOnStartup && PetManager.Instance.PetCount == 0)
                {
                    // Persistence disabled but auto-spawn enabled
                    await PetManager.Instance.AddRandomPetAsync();
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
            }
        }

        private void OnBuildDone(vsBuildScope scope, vsBuildAction action)
        {
            // Determine if build was successful based on error count
            JoinableTaskFactory.RunAsync(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();

                try
                {
                    var errorCount = _dte?.ToolWindows.ErrorList.ErrorItems.Count ?? 0;
                    var success = errorCount == 0;
                    PetManager.Instance.NotifyBuildComplete(success);
                }
                catch
                {
                    // If we can't determine, assume success
                    PetManager.Instance.NotifyBuildComplete(true);
                }
            }).FireAndForget();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unsubscribe from events
                if (_buildEvents != null)
                {
                    _buildEvents.OnBuildDone -= OnBuildDone;
                }

                // Save pets before closing - must complete synchronously to avoid data loss
                try
                {
                    General settings = General.Instance;
                    if (settings.RememberPets)
                    {
                        IReadOnlyList<IPet> pets = PetManager.Instance.GetPets();
                        var petDataList = pets.Select(p => new PetData
                        {
                            Name = p.Name,
                            PetType = p.PetType,
                            Color = p.Color,
                            Size = p.Size
                        }).ToList();

                        // Use synchronous save with timeout to ensure data is persisted before VS exits
                        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
                        {
                            try
                            {
                                JoinableTaskFactory.Run(() => PetPersistenceService.SavePetsAsync(petDataList));
                            }
                            catch (OperationCanceledException)
                            {
                                System.Diagnostics.Debug.WriteLine("VSPets: Save timed out during shutdown");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.Log();
                }

                PetManager.Instance.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}