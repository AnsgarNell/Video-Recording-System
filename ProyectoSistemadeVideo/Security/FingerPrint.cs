using System;
using System.Management;
using System.Security.Cryptography;
using System.Security;
using System.Collections;
using System.Text;
namespace Security
{
    /// <summary>
    /// Generates a 16 byte Unique Identification code of a computer
    /// Example: 4876-8DB5-EE85-69D3-FE52-8CF7-395D-2EA9
    /// </summary>
    public class FingerPrint
    {
        public static string GenerateMachineIdentification()
        {
            //constants
            string[,] check = new string[,] {
                {"Win32_NetworkAdapterConfiguration","MACAddress"},
                {"Win32_Processor", "UniqueId"},
                {"Win32_Processor", "ProcessorId"},
                {"Win32_Processor", "Name"},
                {"Win32_Processor", "Manufacturer"},
                {"Win32_BIOS", "Manufacturer"},
                {"Win32_BIOS", "SMBIOSBIOSVersion"},
                {"Win32_BIOS", "IdentificationCode"},
                {"Win32_BIOS", "SerialNumber"},
                {"Win32_BIOS", "ReleaseDate"},
                {"Win32_BIOS", "Version"},
                {"Win32_DiskDrive", "Model"},
                {"Win32_DiskDrive", "Manufacturer"},
                {"Win32_DiskDrive", "Signature"},
                {"Win32_DiskDrive", "TotalHeads"},
                {"Win32_BaseBoard", "Model"},
                {"Win32_BaseBoard", "Manufacturer"},
                {"Win32_BaseBoard", "Name"},
                {"Win32_BaseBoard", "SerialNumber"},
                {"Win32_VideoController", "DriverVersion"},
                {"Win32_VideoController", "Name"}
            };

            //WMI query
            string query = "SELECT {1} FROM {0}";
            string result = null;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < check.GetLength(0); i++)
            {
                System.Management.ManagementObjectSearcher oWMI = new System.Management.ManagementObjectSearcher(
                    string.Format(query, check[i, 0], check[i, 1]) + (string.Empty));
                foreach (System.Management.ManagementObject mo in oWMI.Get())
                {
                    result = mo[check[i, 1]] as string;
                    if (result != null) sb.AppendLine(result);
                    break;
                }
            }

            //Hashing & format
            MD5 sec = new MD5CryptoServiceProvider();
            ASCIIEncoding enc = new ASCIIEncoding();
            byte[] bt = enc.GetBytes(sb.ToString());
            bt = sec.ComputeHash(bt);
            sb.Clear();
            for (int i = 0; i < bt.Length; i++)
            {
                if (i > 0 && i % 2 == 0) sb.Append('-');
                sb.AppendFormat("{0:X2}", bt[i]);
            }

            return sb.ToString();
        }     
    }
}
