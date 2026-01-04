namespace DocumentOrganizer.Services
{
    public interface IAlertService
    {
        Task ShowAlertAsync(string title, string message, string cancel = "OK");
        Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No");

        Task<string> ShowPromptAsync(string title, string message, string initialValue, string accept = "Rename", string cancel = "Cancel");

        void ShowAlert(string title, string message, string cancel = "OK");

        void ShowConfirmation(string title, string message, Action<bool> callback,
                              string accept = "Yes", string cancel = "No");

        string ShowPrompt(string title, string message, string initialValue, string accept = "Rename", string cancel = "Cancel");
    }

}
