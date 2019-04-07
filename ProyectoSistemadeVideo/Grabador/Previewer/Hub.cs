using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RecordingStudio
{
    public class Hub
    {
        private int identifier;
        public List<int> devices;

        public Hub(int identifier)
        {
            devices = new List<int>();
            this.identifier = identifier;
        }

        public void Add(int device)
        {
            devices.Add(device);
        }

        public override int GetHashCode()
        {
            return identifier;
        }
    }
}
