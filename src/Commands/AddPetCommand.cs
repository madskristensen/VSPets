using System.Windows.Interop;
using VSPets.Controls;
using VSPets.Pets;
using VSPets.Services;

namespace VSPets.Commands
{
    [Command(PackageIds.AddPetCommand)]
    internal sealed class AddPetCommand : BaseCommand<AddPetCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var dialog = new PetSelectionDialog();
                // Ensure dialog is modal to VS
                var window = new WindowInteropHelper(dialog)
                {
                    Owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle
                };

                if (dialog.ShowDialog() == true)
                {
                    IPet pet = await PetManager.Instance.AddPetAsync(dialog.SelectedPetType, dialog.SelectedColor);

                    if (pet != null)
                    {
                        await VS.StatusBar.ShowMessageAsync($"🐾 {pet.Name} the {pet.PetType} joined your IDE!");
                    }
                    else
                    {
                        await VS.StatusBar.ShowMessageAsync($"🐾 Maximum pets reached ({PetManager.Instance.MaxPets})");
                    }
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync("🐾 Failed to add pet");
            }
        }
    }
}
