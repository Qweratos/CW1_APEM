using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayStopAudio_begin.Mvvm;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace PlayStopAudio_begin.ViewModels
{
    class MainPageViewModel : ViewModelBase
    {
        public DelegateCommand PlayCommand { get; }
        public DelegateCommand StopCommand { get; }

        private AudioGraph audioGraph;
        private DeviceInformation selectedDevice;
        private TimeSpan duration;
        private TimeSpan position;
        private AudioFileInputNode fileInputNode;
        private ReverbEffectDefinition effectDefinition; //dodane
        private AudioDeviceOutputNode deviceOutputNode;
        private readonly DispatcherTimer timer;
        private bool updatingPosition;

        private double playbackSpeed;
        private double volume;
        private double decayTime;
        private double reverbGain;
        private double roomSize;
        private string diagnostics;
        private double reverb;


        public ObservableCollection<DeviceInformation> Devices { get; }

        public MainPageViewModel()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Start();
            timer.Tick += TimerOnTick;

            PlayCommand = new DelegateCommand(Play);
            StopCommand = new DelegateCommand(Stop);
            Devices = new ObservableCollection<DeviceInformation>();
            Volume = 5;
            PlaybackSpeed = 100;
            DecayTime = 1;
            RoomSize = 20;
            ReverbGain = -20;
            reverb = 0;

        }

        public async Task InitializeAsync()
        {
            var outputDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioRender); ;
            foreach (var device in outputDevices.Where(d => d.IsEnabled))
            {
                Devices.Add(device);
            }
            SelectedDevice = Devices.FirstOrDefault(d => d.IsDefault);
        }

        public DeviceInformation SelectedDevice
        {
            get { return selectedDevice; }
            set
            {
                if (selectedDevice == value) return;
                selectedDevice = value;
                OnPropertyChanged(nameof(SelectedDevice));
            }
        }

        public TimeSpan Duration
        {
            get { return duration; }
            set
            {
                if (value.Equals(duration)) return;
                duration = value;
                OnPropertyChanged();
            }
        }

        public double PlaybackSpeed
        {
            get { return playbackSpeed; }
            set
            {
                if (value.Equals(playbackSpeed)) return;
                playbackSpeed = value;
                OnPropertyChanged();
                if (fileInputNode != null)
                    fileInputNode.PlaybackSpeedFactor = value / 100.0;
            }
        }

        public double Volume
        {
            get { return volume; }
            set
            {
                if (value.Equals(volume)) return;
                volume = value;
                OnPropertyChanged();
                if (fileInputNode != null)
                    fileInputNode.OutgoingGain = value / 100.0;
            }
        }

      public double DecayTime /////////reverbINPUT DecayTIme
        {
            get { return decayTime; }
            set 
            {
                if (value.Equals(decayTime)) return;
                decayTime = value;
                
                OnPropertyChanged();
                if (effectDefinition != null)
                    effectDefinition.DecayTime = value;
            }
        }

        public double Reverb /////////reverb on off
        {
            get { return reverb; }
            set
            {
                if (value.Equals(reverb)) return;
                reverb = value;

                OnPropertyChanged();
                if (effectDefinition != null)
                    effectDefinition.ReverbDelay = 85;
            }
        }


        public double ReverbGain /////////reverbINPUT ReverbGain
        {
            get { return reverbGain; }
            set
            {
                if (value.Equals(reverbGain)) return;
                reverbGain = value;

                OnPropertyChanged();
                if (effectDefinition != null)
                    effectDefinition.ReverbGain = value;
            }
        }

        public double RoomSize /////////reverbINPUT roomSIZE
        {
            get { return roomSize; }
            set
            {
                if (value.Equals(roomSize)) return;
                roomSize = value;

                OnPropertyChanged();
                if (effectDefinition != null)
                    effectDefinition.RoomSize = value;
            }
        }

        private void TimerOnTick(object sender, object o)
        {
            try
            {
                updatingPosition = true;
                if (fileInputNode != null)
                {
                    Position = fileInputNode.Position;
                }
            }
            finally
            {
                updatingPosition = false;
            }
        }

        public TimeSpan Position
        {
            get { return position; }
            set
            {
                if (value.Equals(position)) return;
                position = value;
                OnPropertyChanged();
                if (!updatingPosition)
                {
                    fileInputNode?.Seek(position);
                }
            }
        }

        private void CreateReverbEffect()
        {
            effectDefinition = new ReverbEffectDefinition(audioGraph);

            
            effectDefinition.DecayTime = 3;
            effectDefinition.ReverbGain = 3;
            effectDefinition.RoomSize = 3;
            effectDefinition.ReverbDelay = 3;


            fileInputNode.EffectDefinitions.Add(effectDefinition);
        }

        private async void Play()
        {
            if (audioGraph == null)
            {
                var settings = new AudioGraphSettings(AudioRenderCategory.Media);
                settings.PrimaryRenderDevice = SelectedDevice;
                var createResult = await AudioGraph.CreateAsync(settings);
                if (createResult.Status != AudioGraphCreationStatus.Success) return;
                audioGraph = createResult.Graph;
                audioGraph.UnrecoverableErrorOccurred += OnAudioGraphError;
            }
            if (deviceOutputNode == null)
            {
                var deviceResult = await audioGraph.CreateDeviceOutputNodeAsync();
                if (deviceResult.Status != AudioDeviceNodeCreationStatus.Success) return;
                deviceOutputNode = deviceResult.DeviceOutputNode;
            }
            if (fileInputNode == null)
            {
                var file = await SelectPlaybackFile();
                if (file == null) return;
                var fileResult = await audioGraph.CreateFileInputNodeAsync(file);
                if (fileResult.Status != AudioFileNodeCreationStatus.Success) return;
                fileInputNode = fileResult.FileInputNode;
                fileInputNode.AddOutgoingConnection(deviceOutputNode);


                Duration = fileInputNode.Duration;
                fileInputNode.PlaybackSpeedFactor = PlaybackSpeed / 100.0;
                fileInputNode.OutgoingGain = Volume / 100.0;
                


                fileInputNode.FileCompleted += FileInputNodeOnFileCompleted;
            }
            if (effectDefinition == null)
            {

                /*var file = await SelectPlaybackFile();
                if (file == null) return;
                var fileResult = audioGraph.
                if (fileResult.Status != AudioDeviceNodeCreationStatus.Success) return;
                effectDefinition = fileResult.effectDefinition;

                */
                //effectDefinition.DecayTime = DecayTime / 100.0;
                //effectDefinition.ReverbGain = ReverbGain / 100.0;
                //effectDefinition.RoomSize = RoomSize / 100.0;

            }
            audioGraph.Start();
            CreateReverbEffect();
        }

        private async void FileInputNodeOnFileCompleted(AudioFileInputNode sender, object args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    audioGraph.Stop();
                    Position = TimeSpan.Zero;
                });
        }

        private async void OnAudioGraphError(AudioGraph sender, AudioGraphUnrecoverableErrorOccurredEventArgs args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    Diagnostics += $"Audio Graph Error: {args.Error}\r\n";
                });
        }

        public string Diagnostics
        {
            get { return diagnostics; }
            set
            {
                if (value == diagnostics) return;
                diagnostics = value;
                OnPropertyChanged();
            }
        }

        private async Task<IStorageFile> SelectPlaybackFile()
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".aac");
            picker.FileTypeFilter.Add(".wav");

            var file = await picker.PickSingleFileAsync();
            return file;
        }

        private void Stop()
        {
            audioGraph?.Stop();
        }
    }
}
