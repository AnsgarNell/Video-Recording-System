using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace RecordingStudio
{
    // clase para gestionar la grabación de audio
    class AudioManager
    {
        public double startTime = 0.0;
        bool grabando = false;
        string mic = "";

        Thread t;

        public void Start()
        {
            checkMic();
            grabando = true;
            t = new Thread(AudioRecorder);
            t.Start();
        }

        // Procedimiento para obtener el identificador de DirectShow del micrófono
        private void checkMic()
        {
            Process proc = new Process();

            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.Arguments = "/c ffmpeg -list_devices true -f dshow -i dummy";

            // Redirect standard input & error output (as FFMPEG uses this) for the process
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.CreateNoWindow = true;

            // UseShellExecute must be false to redirect input/output
            proc.StartInfo.UseShellExecute = false;

            // Start the process
            proc.Start();

            // Use a StringBuilder to hold the program's output
            StringBuilder procOutput = new StringBuilder();

            bool audioDevices = false;
            bool alternativeName = false;

            // Read the process's output until recording is finished
            while (!proc.StandardError.EndOfStream)
            {
                string result = proc.StandardError.ReadLine();

                if (result.Contains("DirectShow audio devices"))
                    audioDevices = true;

                else if(audioDevices)
                {
                    if (!(result.Contains("HD Pro Webcam C920") || result.Contains("Alternative name") || result.Contains("Intel SST Audio Device") || result.Contains("dummy")))
                    {
                        alternativeName = true;            
                    }
                    else if(alternativeName)
                    {
                        string[] mics = result.Split(']');
                        mics = mics[1].Split(' ');
                        mic = mics[7];
                        alternativeName = false;
                    }
                }

                // Append the chars we just read to the procOutput StringBuilder
                procOutput.Append(result);
            }

            string output = procOutput.ToString();

            proc.WaitForExit();
        }

        // Procedimiento que graba el audio
        private void AudioRecorder(object obj)
        {
            Process proc = new Process();

            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.Arguments = "/c ffmpeg -f dshow -i audio=" + mic + " -y -strict experimental -acodec aac -copyts -y videos\\audio.m4a";

            // Redirect standard input & error output (as FFMPEG uses this) for the process
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.CreateNoWindow = true;

            // UseShellExecute must be false to redirect input/output
            proc.StartInfo.UseShellExecute = false;

            // Start the process
            proc.Start();

            // Use a StringBuilder to hold the program's output
            StringBuilder procOutput = new StringBuilder();

            // Read the process's output until recording is finished
            while (!proc.StandardError.EndOfStream)
            {
                string result = proc.StandardError.ReadLine();

                // Append the chars we just read to the procOutput StringBuilder
                procOutput.Append(result);

                if (!grabando)
                {
                    // If recording has finished send "q" and exit this loop
                    proc.StandardInput.WriteLine("q");
                    break;
                }
            }

            string output = procOutput.ToString();

            int found = output.IndexOf("start: ");

            if (found > 0)
            {
                int found_dot = output.IndexOf(".", found);
                int found_end = output.IndexOf(", ", found);
                found = found + 7;
                startTime = double.Parse(output.Substring(found, found_dot - found + 4), CultureInfo.InvariantCulture);
            }

            // Now that we've sent "q" wait for the process to exit
            proc.WaitForExit();
        }

        internal void abortar()
        {
            grabando = false;
            t.Join();
        }
    }
}
