using Microsoft.Maui.Controls;

namespace DocumentOrganizer.Services
{
    internal class AlertService : IAlertService
    {
        public Task ShowAlertAsync(string title, string message, string cancel = "OK")
        {
            return Application.Current.Windows[0].Page.DisplayAlert(title, message, cancel);
        }

        public Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No")
        {
            return Application.Current.Windows[0].Page.DisplayAlert(title, message, accept, cancel);
        }

        public Task<string> ShowPromptAsync(string title, string message, string initialValue, string accept = "Rename", string cancel = "Cancel")
        {
            return Application.Current.Windows[0].Page.DisplayPromptAsync(title, message, accept, cancel,
                placeholder: "Enter new name", initialValue: initialValue);
        }

        /// <summary>
        /// "Fire and forget". Method returns BEFORE showing alert.
        /// </summary>
        public void ShowAlert(string title, string message, string cancel = "OK")
        {
            var page = Application.Current?.Windows[0].Page;
            page?.Dispatcher.Dispatch(async () =>
                await ShowAlertAsync(title, message, cancel)
            );
        }

        /// <summary>
        /// "Fire and forget". Method returns BEFORE showing alert.
        /// </summary>
        /// <param name="callback">Action to perform afterwards.</param>
        public void ShowConfirmation(string title, string message, Action<bool> callback,
                                     string accept = "Yes", string cancel = "No")
        {
            var page = Application.Current?.Windows[0].Page;
            page?.Dispatcher.Dispatch(async () =>
                {
                    bool answer = await ShowConfirmationAsync(title, message, accept, cancel);
                    callback(answer);
                });
        }

        public string ShowPrompt(string title, string message, string initialValue,
                                 string accept = "Rename", string cancel = "Cancel")
        {
            string result = string.Empty;

            Application.Current.Windows[0].Page.Dispatcher.Dispatch(async () =>
            {
                result = await ShowPromptAsync(title, message, initialValue, accept, cancel);
            });

            return result;
        }
    }
}
