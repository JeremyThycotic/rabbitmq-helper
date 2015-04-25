using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Thycotic.Logging;
using Thycotic.WindowsService.Bootstraper.Wmi;

namespace Thycotic.WindowsService.Bootstraper
{
    public class ServiceUpdater
    {
        private readonly CancellationTokenSource _cts;
        private readonly string _workingPath;
        private readonly string _serviceName;
        private readonly string _msiPath;
        private readonly ILogWriter _log = Log.Get(typeof (ServiceUpdater));

        public ServiceUpdater(CancellationTokenSource cts, string workingPath, string serviceName, string msiPath)
        {
            _cts = cts;
            _workingPath = workingPath;
            _serviceName = serviceName;
            _msiPath = msiPath;
        }

        private static void CleanDirectory(string path)
        {
            var directoryInfo = new DirectoryInfo(path);

            directoryInfo.GetFiles().ToList().ForEach(f => f.Delete());

            directoryInfo.GetDirectories().ToList().ForEach(d => d.Delete(true));
        }

        private static void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        private static ManagementObject GetManagementObject(ManagementPath computerPath)
        {
            var path = computerPath;
            var managementObject = new ManagementObject(path);
            return managementObject;
        }

        private IWin32Service GetService()
        {
            var computerPath = Win32Service.GetLocalServiceManagementPath(_serviceName);
            var managementObject = GetManagementObject(new ManagementPath(computerPath));

            //Task.Delay(TimeSpan.FromSeconds(5)).Wait();

            managementObject.Scope.Connect();

            return new Win32Service(managementObject);

        }

        private void InteractiveWithService(Action<IWin32Service> action)
        {
            try
            {
                using (var win32Service = GetService())
                {
                    action.Invoke(win32Service);
                }

            }
            catch (Exception ex)
            {
                _log.Error("Interaction with service failed", ex);
                throw;
            }
        }

        private string GetServiceState()
        {
            try
            {
                using (var win32Service = GetService())
                {
                    return win32Service.State;
                }

            }
            catch (Exception ex)
            {
                _log.Error("Interaction with service failed", ex);
                throw;
            }
        }

        public void Update()
        {
            using (LogCorrelation.Create())
            {
                try
                {
                    _log.Info(string.Format("Running bootstrap process for {0} with {1}", _serviceName, _msiPath));

                    StopService();

                    CleanDirectory(_workingPath);
                    //recreate the log path that was just cleaned up
                    CreateDirectory(Path.Combine(_workingPath, "log"));

                    var processInfo = new ProcessStartInfo("msiexec")
                    {
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        WorkingDirectory = _workingPath,
                        Arguments = string.Format(@"/i {0} /qn /log log\SSDEUpdate.log", _msiPath)
                    };

                    _log.Info(string.Format("Running MSI with arguments: {0}", processInfo.Arguments));


                    Process process = null;

                    var task = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            process = Process.Start(processInfo);
                        }
                        catch (Exception ex)
                        {
                            throw new ApplicationException("Could not start process", ex);
                        }


                        if (process == null)
                        {
                            throw new ApplicationException("Process could not start");
                        }

                        process.WaitForExit();

                    }, _cts.Token);

                    //wait for 30 seconds for process to complete
                    task.Wait(TimeSpan.FromSeconds(30));

                    //there was an exception, rethrow it
                    if (task.Exception != null)
                    {
                        throw task.Exception;
                    }

                    if (process != null)
                    {
                        if (!process.HasExited)
                        {
                            _log.Warn("Process has not exited. Forcing exit");
                            process.Kill();
                        }

                        //process didn't exit correctly, extract output and throw
                        if (process.ExitCode != 0)
                        {
                            var output = process.StandardOutput.ReadToEnd();

                            throw new ApplicationException("Process failed", new Exception(output));
                        }
                    }

                    _log.Info("MSI finished");

                    //Configuration configuration = System.Configuration.ConfigurationManager.OpenExeConfiguration(Path.Combine(parentDirectory.ToString(), "SecretServerAgentService.exe"));
                    //configuration.AppSettings.Settings["RPCAgentVersion"].Value = args[0];
                    //configuration.Save();

                    StartService();

                    _log.Info("Update complete");

                }
                catch (Exception ex)
                {
                    _log.Error("Failed to bootstrap", ex);
                }
            }
        }

        private void StartService()
        {
            InteractiveWithService(service =>
            {
                _log.Info("Starting service");
                service.StartService();

            });

            while (!_cts.Token.IsCancellationRequested)
            {
                if (GetServiceState() == ServiceStates.Running)
                {
                    _log.Info("Service running");
                    break;
                }

                Task.Delay(TimeSpan.FromSeconds(5), _cts.Token).Wait(_cts.Token);
            }
        }

        private void StopService()
        {
            InteractiveWithService(service =>
            {
                _log.Info("Stopping service");
                service.StopService();
            });

            while (!_cts.Token.IsCancellationRequested)
            {
                if (GetServiceState() == ServiceStates.Stopped)
                {
                    _log.Info("Service stopped");
                    break;
                }

                Task.Delay(TimeSpan.FromSeconds(5), _cts.Token).Wait(_cts.Token);
            }
        }
    }
}