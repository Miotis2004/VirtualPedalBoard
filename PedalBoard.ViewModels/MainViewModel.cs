using System.Collections.ObjectModel;
using System.Linq;
using PedalBoard.Core;
using PedalBoard.Audio;

namespace PedalBoard.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private IAudioDriver _audioDriver;
        private string _selectedDriver;
        private ObservableCollection<string> _availableDrivers;
        private ObservableCollection<PedalViewModel> _pedals;

        public ObservableCollection<string> AvailableDrivers => _availableDrivers;
        public ObservableCollection<PedalViewModel> Pedals => _pedals;

        public string SelectedDriver
        {
            get => _selectedDriver;
            set
            {
                _selectedDriver = value;
                OnPropertyChanged();
                InitializeAudio();
            }
        }

        public MainViewModel()
        {
            _pedals = new ObservableCollection<PedalViewModel>();
            _availableDrivers = new ObservableCollection<string>();

            // Setup default pedal board
            _pedals.Add(new PedalViewModel(new BoostEffect()));
            _pedals.Add(new PedalViewModel(new OverdriveEffect()));
            _pedals.Add(new PedalViewModel(new DistortionEffect()));
            _pedals.Add(new PedalViewModel(new ChorusEffect()));
            _pedals.Add(new PedalViewModel(new ReverbEffect()));

            LoadDrivers();
        }

        private void LoadDrivers()
        {
            try
            {
                // In a real scenario, we might wrap this in a try-catch because NAudio might throw if no drivers
                 var driver = new AsioAudioDriver(); 
                 foreach(var d in driver.GetInputDevices())
                 {
                     _availableDrivers.Add(d);
                 }
                 if(_availableDrivers.Any()) SelectedDriver = _availableDrivers.First();
            }
            catch
            {
                // Handle case where no ASIO drivers exist
            }
        }

        private void InitializeAudio()
        {
            if (_audioDriver != null)
            {
                _audioDriver.Stop();
                _audioDriver.Dispose();
            }

            if (!string.IsNullOrEmpty(SelectedDriver))
            {
                try 
                {
                    _audioDriver = new AsioAudioDriver(SelectedDriver);
                    // Pass the underlying effects to the driver
                    // Note: In a real MVVM app, we might need a better way to sync the list
                    // but for now we extract the model from the viewmodel
                    var effects = _pedals.Select(p => typeof(PedalViewModel).GetField("_effect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(p) as IAudioEffect).ToList();
                    
                    _audioDriver.SetEffectChain(effects);
                    _audioDriver.Start(SelectedDriver, 44100, 512);
                }
                catch
                {
                    // Error initializing
                }
            }
        }
    }
}
