using VSPets.Services;

namespace VSPets.Commands
{
    [Command(PackageIds.TogglePetsVisibilityCommand)]
    internal sealed class TogglePetsVisibilityCommand : BaseCommand<TogglePetsVisibilityCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                if (PetManager.Instance.IsHidden)
                {
                    await PetManager.Instance.ShowPetsAsync();
                    await VS.StatusBar.ShowMessageAsync("🐾 Pets are back!");
                }
                else
                {
                    await PetManager.Instance.HidePetsAsync();
                    await VS.StatusBar.ShowMessageAsync("🐾 Pets are hidden");
                }

                // Update the command text
                await UpdateCommandTextAsync();
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                await VS.StatusBar.ShowMessageAsync("🐾 Failed to toggle pet visibility");
            }
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Update command text based on current visibility state
            Command.Text = PetManager.Instance.IsHidden ? "Show Pets" : "Hide Pets";
        }

        private async Task UpdateCommandTextAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            Command.Text = PetManager.Instance.IsHidden ? "Show Pets" : "Hide Pets";
        }
    }
}
