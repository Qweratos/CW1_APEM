﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Foundation.Collections;
using System.Runtime.InteropServices;
using Windows.Media;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml.Controls;

namespace AudioEffectComponent
{
    public sealed class ExampleAudioEffect : IBasicAudioEffect
    {
        public IReadOnlyList<AudioEncodingProperties> SupportedEncodingProperties
        {
            get
            {
                var supportedEncodingProperties = new List<AudioEncodingProperties>();
                AudioEncodingProperties encodingProps1 = AudioEncodingProperties.CreatePcm(44100, 1, 32);
                encodingProps1.Subtype = MediaEncodingSubtypes.Float;
                AudioEncodingProperties encodingProps2 = AudioEncodingProperties.CreatePcm(48000, 1, 32);
                encodingProps2.Subtype = MediaEncodingSubtypes.Float;

                supportedEncodingProperties.Add(encodingProps1);
                supportedEncodingProperties.Add(encodingProps2);

                return supportedEncodingProperties;

            }
        }

        private float[] echoBuffer;
        private int currentActiveSampleIndex;
        private AudioEncodingProperties currentEncodingProperties;

        public void SetEncodingProperties(AudioEncodingProperties encodingProperties)
        {
            currentEncodingProperties = encodingProperties;
            echoBuffer = new float[encodingProperties.SampleRate]; // exactly one second delay
            currentActiveSampleIndex = 0;
        }

        IPropertySet configuration;
        public void SetProperties(IPropertySet configuration)
        {
            this.configuration = configuration;
        }

        public float Mix
        {
            get
            {
                object val;
                if (configuration != null && configuration.TryGetValue("Mix", out val))
                {
                    return (float)val;
                }
                return .5f;
            }
        }

        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        unsafe public void ProcessFrame(ProcessAudioFrameContext context)
        {
            AudioFrame inputFrame = context.InputFrame;
            AudioFrame outputFrame = context.OutputFrame;

            using (AudioBuffer inputBuffer = inputFrame.LockBuffer(AudioBufferAccessMode.Read),
                                outputBuffer = outputFrame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference inputReference = inputBuffer.CreateReference(),
                                            outputReference = outputBuffer.CreateReference())
            {
                byte* inputDataInBytes;
                byte* outputDataInBytes;
                uint inputCapacity;
                uint outputCapacity;

                ((IMemoryBufferByteAccess)inputReference).GetBuffer(out inputDataInBytes, out inputCapacity);
                ((IMemoryBufferByteAccess)outputReference).GetBuffer(out outputDataInBytes, out outputCapacity);

                float* inputDataInFloat = (float*)inputDataInBytes;
                float* outputDataInFloat = (float*)outputDataInBytes;

                float inputData;
                float echoData;

                // Process audio data
                int dataInFloatLength = (int)inputBuffer.Length / sizeof(float);

                for (int i = 0; i < dataInFloatLength; i++)
                {
                    inputData = inputDataInFloat[i] * (1.0f - this.Mix);
                    echoData = echoBuffer[currentActiveSampleIndex] * this.Mix;
                    outputDataInFloat[i] = inputData + echoData;
                    echoBuffer[currentActiveSampleIndex] = inputDataInFloat[i];
                    currentActiveSampleIndex++;

                    if (currentActiveSampleIndex == echoBuffer.Length)
                    {
                        // Wrap around (after one second of samples)
                        currentActiveSampleIndex = 0;
                    }
                }
            }
        }

        public void Close(MediaEffectClosedReason reason)
        {
            // Dispose of effect resources
            echoBuffer = null;
        }

        public void DiscardQueuedFrames()
        {
            // Reset contents of the samples buffer
            Array.Clear(echoBuffer, 0, echoBuffer.Length - 1);
            currentActiveSampleIndex = 0;
        }

        public bool TimeIndependent { get { return true; } }

        public bool UseInputFrameForOutput { get { return false; } }

        
    }

}
