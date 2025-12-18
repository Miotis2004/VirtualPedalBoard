using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PedalBoard.Core;

namespace PedalBoard.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PedalViewModel : ViewModelBase
    {
        private IAudioEffect _effect;
        
        public string Name => _effect.Name;
        
        public bool IsEnabled 
        {
            get => _effect.IsEnabled;
            set
            {
                if(_effect.IsEnabled != value)
                {
                    _effect.IsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        // We expose generic parameter controls. 
        // In a real app, we might use reflection or a dictionary of parameters.
        // For this POC, let's expose specific properties based on type.

        public bool HasGain => _effect is BoostEffect;
        public float Gain
        {
            get => (_effect as BoostEffect)?.Gain ?? 1.0f;
            set
            {
                if (_effect is BoostEffect b)
                {
                    b.Gain = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasDrive => _effect is DistortionEffect;
        public float Drive
        {
            get => (_effect as DistortionEffect)?.Drive ?? 0.0f;
            set
            {
                if (_effect is DistortionEffect d)
                {
                    d.Drive = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasMix => _effect is ChorusEffect || _effect is ReverbEffect;
        public float Mix
        {
            get 
            {
                 if (_effect is ChorusEffect c) return c.Mix;
                 if (_effect is ReverbEffect r) return r.Mix;
                 return 0.0f;
            }
            set
            {
                 if (_effect is ChorusEffect c) c.Mix = value;
                 if (_effect is ReverbEffect r) r.Mix = value;
                 OnPropertyChanged();
            }
        }
        
        // Add other parameters (Rate, Depth, Decay) similarly...
        public bool HasRate => _effect is ChorusEffect;
        public float Rate 
        {
            get => (_effect as ChorusEffect)?.Rate ?? 0.0f;
            set { if (_effect is ChorusEffect c) { c.Rate = value; OnPropertyChanged(); } }
        }

        public bool HasDepth => _effect is ChorusEffect;
        public float Depth 
        {
            get => (_effect as ChorusEffect)?.Depth ?? 0.0f;
            set { if (_effect is ChorusEffect c) { c.Depth = value; OnPropertyChanged(); } }
        }
        
        public bool HasDecay => _effect is ReverbEffect;
        public float Decay
        {
            get => (_effect as ReverbEffect)?.Decay ?? 0.0f;
            set { if (_effect is ReverbEffect r) { r.Decay = value; OnPropertyChanged(); } }
        }


        public PedalViewModel(IAudioEffect effect)
        {
            _effect = effect;
        }
    }
}
