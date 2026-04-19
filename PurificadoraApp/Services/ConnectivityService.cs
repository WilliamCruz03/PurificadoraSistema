using System.ComponentModel;

namespace PurificadoraApp.Services
{
    public class ConnectivityService : INotifyPropertyChanged
    {
        private bool _isConnected;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Func<Task> ConnectivityChanged;  // ✅ Correcto: Func<Task>

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
                    _ = OnConnectivityChangedAsync();  // ✅ Llamada fire-and-forget
                }
            }
        }

        public ConnectivityService()
        {
            _isConnected = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
            Connectivity.ConnectivityChanged += OnConnectivityChanged;
        }

        private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            var newStatus = e.NetworkAccess == NetworkAccess.Internet;
            if (IsConnected != newStatus)
            {
                IsConnected = newStatus;
            }
        }

        private async Task OnConnectivityChangedAsync()
        {
            if (ConnectivityChanged != null)
            {
                await ConnectivityChanged.Invoke();
            }
        }

        ~ConnectivityService()
        {
            Connectivity.ConnectivityChanged -= OnConnectivityChanged;
        }
    }
}