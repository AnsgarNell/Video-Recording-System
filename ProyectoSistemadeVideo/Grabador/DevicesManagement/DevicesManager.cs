using System;
using System.Collections.Generic;

namespace RecordingStudio
{
    public class DevicesManager
    {
        public Dictionary<int, Hub> hubs;
        public List<CameraPreviewer> previewers;
        private MainForm main;
        private int currentHub;
        private int currentCamera = 0;

        public DevicesManager(MainForm main)
        {
            this.main = main;
            hubs = DeviceEnumerator.GetDevices(this, out previewers);
            currentHub = hubs.Keys.GetEnumerator().Current;

            foreach (CameraPreviewer cameraPreviewer in previewers)
            {
                main.AddPreviewer(cameraPreviewer);
                cameraPreviewer.Start();
                cameraPreviewer.captureDevice.WaitForStop();
            }
            if (previewers.Count > 0) main.enableRec();
        }

        public void StartPreview(int identifier)
        {
            if (currentCamera == identifier) return;
            previewers[currentCamera].Stop();
            currentCamera = identifier;
        }

        public void StopPreview()
        {
            if (previewers.Count > 0)
            {
                previewers[currentCamera].Stop();
                previewers[currentCamera].captureDevice.WaitForStop();
                foreach (KeyValuePair<int, Hub> entry in hubs)
                {                    
                    foreach (int device in entry.Value.devices)
                    {
                        previewers[device].Disable();
                    }                 
                }
            }
        }

        public string nextCamId()
        {
            Random random = new Random();
            List<int> cameras = getCameras();
            int next = random.Next(cameras.Count);
            next = cameras[next];
            currentHub = previewers[next].hub;
            //disableHub(currentHub);
            return previewers[next].DShowID;            
        }

        private List<int> getCameras()
        {
            List<int> result = new List<int>();
            foreach (KeyValuePair<int, Hub> entry in hubs)
            {
                if (entry.Key != currentHub)
                {
                    foreach(int camera in entry.Value.devices)
                    {
                        result.Add(camera);
                    }
                }
            }
            return result;
        }

        private void disableHub(int hubId)
        {
            foreach(KeyValuePair<int, Hub> entry in hubs)
            {
                if(entry.Key == hubId)
                {
                    foreach(int device in entry.Value.devices)
                    {
                        previewers[device].Disable();
                    }
                }
                else
                {
                    foreach (int device in entry.Value.devices)
                    {
                        previewers[device].Enable();
                    }
                }
            }
        }
    }
}
