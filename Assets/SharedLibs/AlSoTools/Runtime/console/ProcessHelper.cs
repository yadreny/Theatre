//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Management;

//public static class ProcessHelper
//{
//    public static IEnumerable<Process> GetChildProcesses(this Process parentProcess)
//    {
//        List<Process> childProcesses = new List<Process>();

//        foreach (Process process in Process.GetProcesses())
//        {
//            try
//            {
//                if (process.Parent().Id == parentProcess.Id)
//                {
//                    childProcesses.Add(process);
//                }
//            }
//            catch { }
//        }

//        return childProcesses;
//    }

//    public static Process Parent(this Process process)
//    {
//        int parentProcessId = 0;
//        string query = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {process.Id}";
//        using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
//        {
//            foreach (ManagementObject mo in searcher.Get())
//            {
//                parentProcessId = Convert.ToInt32(mo["ParentProcessId"]);
//            }
//        }
//        try
//        {
//            return Process.GetProcessById(parentProcessId);
//        }
//        catch (Exception)
//        {
//            return null;
//        }
//    }
//}