using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace BizTalkInstaller
{
    class RegistryHelper
    {
        /// <summary>
        /// [HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\BTSSvc$BBW_TxHost]
        /// "ThrottlingPublishOverride"=dword:00000002
        /// "ThrottlingDeliveryOverride"=dword:00000002
        /// </summary>
        public void DisableSendHostThrottling()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BTSSvc$BBW_TxHost", true);
            key.SetValue("ThrottlingPublishOverride", "00000002", RegistryValueKind.DWord);
            key.SetValue("ThrottlingDeliveryOverride", "00000002", RegistryValueKind.DWord);
        }
        /// <summary>
        /// [HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\BTSSvc$BizTalkServerApplication\CLR Hosting]
        /// "MaxIOThreads"=dword:00000064
        /// "MaxWorkerThreads"=dword:00000064
        /// "MinIOThreads"=dword:00000019
        /// "MinWorkerThreads"=dword:00000019
        /// </summary>
        public void UpdateBizTalkServerApplication()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BTSSvc$BizTalkServerApplication\CLR Hosting", true);
            key.SetValue("MaxIOThreads", "00000064", RegistryValueKind.DWord);
            key.SetValue("MaxWorkerThreads", "00000064", RegistryValueKind.DWord);
            key.SetValue("MinIOThreads", "00000019", RegistryValueKind.DWord);
            key.SetValue("MinWorkerThreads", "00000019", RegistryValueKind.DWord);
        }
        /// <summary>
        /// [HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\BTSSvc$BBW_RxHost\CLR Hosting]
        /// "MaxIOThreads"=dword:00000064
        /// "MaxWorkerThreads"=dword:00000064
        /// "MinIOThreads"=dword:00000019
        /// "MinWorkerThreads"=dword:00000019
        /// </summary>
        public void CLRHosting_BBWRxHost()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BTSSvc$BBW_RxHost\CLR Hosting", true);
            key.SetValue("MaxIOThreads", "00000064", RegistryValueKind.DWord);
            key.SetValue("MaxWorkerThreads", "00000064", RegistryValueKind.DWord);
            key.SetValue("MinIOThreads", "00000019", RegistryValueKind.DWord);
            key.SetValue("MinWorkerThreads", "00000019", RegistryValueKind.DWord);
        }

        public void CLRHosting_BBWPxHost()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BTSSvc$BBW_PxHost\CLR Hosting", true);
            key.SetValue("MaxIOThreads", "00000064", RegistryValueKind.DWord);
            key.SetValue("MaxWorkerThreads", "00000064", RegistryValueKind.DWord);
            key.SetValue("MinIOThreads", "00000019", RegistryValueKind.DWord);
            key.SetValue("MinWorkerThreads", "00000019", RegistryValueKind.DWord);
        }

        public void CLRHosting_BBWTxHost()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BTSSvc$BBW_TxHost\CLR Hosting", true);
            key.SetValue("MaxIOThreads", "00000064", RegistryValueKind.DWord);
            key.SetValue("MaxWorkerThreads", "00000064", RegistryValueKind.DWord);
            key.SetValue("MinIOThreads", "00000019", RegistryValueKind.DWord);
            key.SetValue("MinWorkerThreads", "00000019", RegistryValueKind.DWord);
        }

    }
}
