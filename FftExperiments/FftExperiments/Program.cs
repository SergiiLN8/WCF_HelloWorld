using AForge.Math;
using MathNet.Numerics.Transformations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FftExperiments
{
    class Program
    {
        float[] L = null;
        static void Main(string[] args)
        {
            int expectedFrequency = 645;
            int expectedDuration = 3;
            string file = @"C:\Users\Lena\Desktop\5645.wav";
            
            int sampleRate = 0; //Hz
            double[] allSamples = GetSamplesForMonoWav(file, out sampleRate);
            List<double[]> partisipantSegments = GetPartisipantSegments(allSamples, sampleRate);
            foreach (double[] segment in partisipantSegments)
            {
                VerifyPartisipant(segment, expectedFrequency, expectedDuration, sampleRate);
            }
           

            Console.ReadKey();
        }

        private static bool VerifyPartisipant(double[] segment, int expectedFrequency, int expectedDuration, int sampleRate)
        {
            bool partisipantIsCorrect = false;
            int numberOfSamplesForFft = 2048;
            double[] realSamplesForFft = segment.Skip(numberOfSamplesForFft).Take(numberOfSamplesForFft).ToArray();
            double[] allSamplesForFft = new double[realSamplesForFft.Count() * 2];
           
            for (int i = 0; i < allSamplesForFft.Count(); i += 2)
            {
                allSamplesForFft[i] = i % 2 == 0 ? realSamplesForFft[i / 2] : 0; //real part remain, imagine - inserts zeros
            }
            try
            {
                double[] freqReal, freqImag;
                RealFourierTransformation rft = new RealFourierTransformation();
                rft.TransformForward(allSamplesForFft, out freqReal, out freqImag);
                int maxFftArrayIndex = 22000 * numberOfSamplesForFft / sampleRate / 2; //2200 - max fequency, which can be heared (theoretically); /2 - array is mirroved;
                freqReal = freqReal.ToList().GetRange(0,maxFftArrayIndex).ConvertAll(element => element = Math.Abs(element)).ToArray();
                int fftArrayIndex = Array.IndexOf(freqReal, freqReal.Max());
                double dominantFrequency = Convert.ToInt32(fftArrayIndex * sampleRate / numberOfSamplesForFft);
                double numberOfSamples = segment.Count();
                double segmentDuration =  numberOfSamples/(double)sampleRate;
                bool correctSegmentDuration = expectedDuration * 0.95 < segmentDuration && segmentDuration < expectedDuration * 1.05;
                partisipantIsCorrect = expectedFrequency * 0.95 < dominantFrequency && dominantFrequency < expectedFrequency * 1.05;
            
            }
            catch(Exception e)
            {
                //fail report
            }

            return partisipantIsCorrect;
        }
        private static Double[] GetSamplesForMonoWav(String wavePath, out int sampleRate)
        {
            Double[] data;
            byte[] wave;
            try
            {
                System.IO.FileStream waveFile = System.IO.File.OpenRead(wavePath);
                wave = new byte[waveFile.Length];
                waveFile.Read(wave, 0, Convert.ToInt32(waveFile.Length)); //read the wave file into the wave variable;
                /***********Converting and PCM accounting***************/
                int bitsPerSample = BitConverter.ToInt16(wave, 34); //in wav file bitsPerSample info is in 34-35 bytes;
                double maxSampleValue = Math.Pow(2, bitsPerSample - 1); //-1 becouse samples are signed;
                int bytesForOneSample = bitsPerSample / 8;
                data = new Double[(wave.Length - 44) / bytesForOneSample]; //shifting the headers out of the PCM data;

                for (int i = 0; i < data.Length; i ++)
                {
                    data[i] = (BitConverter.ToInt16(wave, i * bytesForOneSample)) / maxSampleValue;
                }

                int headerBytesToSkip = 44 / bytesForOneSample; // header length is 44 bytes;
                data = data.Skip(headerBytesToSkip).ToArray();
                /**************assigning sample rate**********************/
                sampleRate = BitConverter.ToInt32(wave, 24);    //in wav file sampleRate info is in 24-27 bytes;
                return data;
            }
            catch(Exception e)
            {
                //Failreport
                sampleRate = -1;
            }
           
            return null;
        }

        private static List<double[]> GetPartisipantSegments(double[] allSamples, int sampleRate)
        {
            List<double[]> partisipantSegments = new List<double[]>();
            double[] tempPartisipantSegment;
            double minVoiceActivityPeriod = 0.1; //Will be calculated Root Mean Square for each 0.1 seconds;
            int samplesCountForRMS = Convert.ToInt32(sampleRate * minVoiceActivityPeriod); 
            double[] samplesRange = new double[samplesCountForRMS];
            int startPosition = 0;
            int numberOfDetectedVoiceActivity = 0;
            int stopPosition;
            int numberOfDetectedVoiceSilence = 0;

            bool partisipantSegmentStarted = false;

            for (int i = 0; i + samplesCountForRMS < allSamples.Count(); i += samplesCountForRMS)
            {
                samplesRange = allSamples.Skip(i).Take(samplesCountForRMS).ToArray();
                if (DetectVoiceActivity(samplesRange))
                {
                    numberOfDetectedVoiceActivity++;
                    startPosition = !partisipantSegmentStarted ? i : startPosition;
                    partisipantSegmentStarted = true;
                    numberOfDetectedVoiceSilence = 0;
                }
                else if (partisipantSegmentStarted) 
                {
                    numberOfDetectedVoiceSilence++;
                    if (numberOfDetectedVoiceActivity <= 1) //minVoiceActivityPeriod * 1 = 0.1 seconds - if audio activity less - assume it as noise;
                    {
                        partisipantSegmentStarted = false;
                        numberOfDetectedVoiceActivity = 0;
                        continue;
                    }
                    if (numberOfDetectedVoiceSilence > 20) //minVoiceActivityPeriod * 20 = 2 seconds - if silence less - assume it as broken signal;
                    {
                        stopPosition = i - 20 * samplesCountForRMS;
                        tempPartisipantSegment = new double[stopPosition - startPosition];
                        tempPartisipantSegment = allSamples.Skip(startPosition).Take(stopPosition - startPosition).ToArray();
                        partisipantSegments.Add(tempPartisipantSegment);
                        partisipantSegmentStarted = false;
                        numberOfDetectedVoiceActivity = 0;
                    }
                }
            }
            return partisipantSegments;
        }

        private static bool DetectVoiceActivity(double[] samplesRange)
        {
            double squaredSum = 0;
            double rootMeanSquare = 0;

            samplesRange.ToList().ForEach(sample => squaredSum += sample * sample);
            rootMeanSquare = Math.Sqrt(squaredSum / samplesRange.Count());

            double signalLowLevelLimit = 0.025; // low limit for signal;
            return rootMeanSquare > signalLowLevelLimit;
        }
    }
}


//Complex[] complexSamples = samplesForFft.Select(sample => new Complex((double)sample, 0.0)).ToArray();
//FourierTransform.FFT(complexSamples, FourierTransform.Direction.Forward);
//for (int i = 0; i < complexSamples.Length; i++)
//{
//    if(i == complexSamples.Length/2)
//        Console.WriteLine(complexSamples[i].ToString());
//}