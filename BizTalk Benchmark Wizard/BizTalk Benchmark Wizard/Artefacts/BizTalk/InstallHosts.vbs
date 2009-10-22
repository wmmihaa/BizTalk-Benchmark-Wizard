Option Explicit
const UpdateOnly = 1
const CreateOnly = 2
Const HostInstServiceState_Running = 4
Const HostInstConfigState_NotInstalled = 5

' Paramaters:
'   0 : NTGroupName
'   1 : UserName
'   2 : Password
'   3 : Receive Host
'   4 : Send Host
'   5 : Processing Host


If WScript.Arguments.Count = 5 Then
    wscript.echo "NTGroupName = " & WScript.Arguments(0)
    wscript.echo "UserName = " & WScript.Arguments(1)
    wscript.echo "Password = " & WScript.Arguments(2)
    wscript.echo "Receive Host = " & WScript.Arguments(3)
    wscript.echo "Send Host = " & WScript.Arguments(4)
    wscript.echo "Processing Host = " & WScript.Arguments(5)

    wscript.echo "**********************************************************"
    wscript.echo "Create BBW_RxHost"
    wscript.echo "**********************************************************"
    CreateHost "BBW_RxHost",1,WScript.Arguments(0),false, false
    MapInstallHostInstance "BBW_RxHost", WScript.Arguments(3), WScript.Arguments(1), WScript.Arguments(2)
    CreateReceiveHandler "BBW_RxHost", "WCF-NetTcp", "MSBTS_ReceiveHandler"
    CreateReceiveHandler "BBW_RxHost", "WCF-NetTcp", "MSBTS_SendHandler2"
    wscript.echo""

    wscript.echo "**********************************************************"
    wscript.echo "Create BBW_TxHost"
    wscript.echo "**********************************************************"
    CreateHost "BBW_TxHost",1,WScript.Arguments(0),false, false
    MapInstallHostInstance "BBW_TxHost", WScript.Arguments(4), WScript.Arguments(1), WScript.Arguments(2)
    CreateReceiveHandler "BBW_TxHost", "WCF-NetTcp", "MSBTS_ReceiveHandler"
    CreateReceiveHandler "BBW_TxHost", "WCF-NetTcp", "MSBTS_SendHandler2"
    wscript.echo""

    wscript.echo "**********************************************************"
    wscript.echo "Create BBW_PxHost"
    wscript.echo "**********************************************************"
    'Create BBW_PxHost
    CreateHost "BBW_PxHost",1,WScript.Arguments(0),false, true
    MapInstallHostInstance "BBW_PxHost", WScript.Arguments(5), WScript.Arguments(1), WScript.Arguments(2)

    wscript.echo "**********************************************************"
    wscript.echo "DONE!"
    wscript.echo "**********************************************************"
Else
    wscript.echo "**********************************************************"
    wscript.echo "Use the InstallHosts.vbs to install Hosts, Host Instances and Adapter handlers"
    wscript.echo ""
    wscript.echo " Paramaters:"
    wscript.echo "   0 : NTGroupName"
    wscript.echo "   1 : UserName"
    wscript.echo "   2 : Password"
    wscript.echo "   3 : Receive Host"
    wscript.echo "   4 : Send Host"
    wscript.echo "   5 : Processing Host"
    wscript.echo ""
    wscript.echo "Usages: cscript InstallHosts.vbs [NTGroupName] [UserName] [Password] [Receive Host] [Send Host] [Processing Host]"
    wscript.echo "Sample: cscript InstallHosts.vbs ""BizTalk Application Users"" \MyUser MyPassword BtsServer1 BtsServer2 BtsServer2"

    wscript.echo "**********************************************************"
    wscript.echo""
End If
Sub CreateHost (HostName, HostType, NTGroupName, AuthTrusted, HostTracking)
   On Error Resume Next
   Dim objLocator, objService, objHostSetting, objHS

   ' Connects to local server WMI Provider BizTalk namespace
   Set objLocator = Createobject ("wbemScripting.SWbemLocator")
   Set objService = objLocator.ConnectServer(".", "root/MicrosoftBizTalkServer")

   ' Get WMI class MSBTS_HostSetting
   Set objHostSetting = objService.Get ("MSBTS_HostSetting")

   Set objHS = objHostSetting.SpawnInstance_

   objHS.Name = HostName
   objHS.HostType = HostType
   objHS.NTGroupName = NTGroupName
   objHS.AuthTrusted = AuthTrusted
   objHS.IsHost32BitOnly = False
   objHS.HostTracking = HostTracking

   ' Create instance
   objHS.Put_(CreateOnly)

   CheckWMIError
   wscript.echo "Host - " & HostName & " - has been created successfully"
   
end Sub

Sub DeleteHost (HostName)
   On Error Resume Next

   Dim objLocator, objService, objHS

   ' Connects to local server WMI Provider BizTalk namespace
   Set objLocator = Createobject ("wbemScripting.SWbemLocator")
   Set objService = objLocator.ConnectServer(".", "root/MicrosoftBizTalkServer")

   ' Look for WMI Class MSBTS_HostSetting with name equals HostName value
   Set objHS = objService.Get("MSBTS_HostSetting.Name='" & HostName & "'")

   ' Delete instance
   objHS.Delete_

   ' Check for error condition before continuing.
   CheckWMIError
   wscript.echo "Host - " & HostName & " - has been deleted successfully"

end Sub

' Map and install a host instance using MSBTS_ServerHost and MSBTS_HostInstance
Sub MapInstallHostInstance (HostName, ServerName, uid, pwd)
   On Error Resume Next

   Dim objLocator, objService, objServerHost, objSH
   Dim objHostInstance, objHI

   ' Connects to local server WMI Provider BizTalk namespace
   Set objLocator = Createobject ("wbemScripting.SWbemLocator")
   Set objService = objLocator.ConnectServer(".", "root/MicrosoftBizTalkServer")

   ' Step 1 - Create mapping between server and host using MSBTS_ServerHost class
   Set objServerHost = objService.Get ("MSBTS_ServerHost")

   Set objSH = objServerHost.SpawnInstance_

   objSH.HostName = HostName
   objSH.ServerName = ServerName

   ' Invoke MSBTS_ServerHost Map method
   objSH.Map

   CheckWMIError
   wscript.echo "Host - " & HostName & " - has been mapped successfully - " & ServerName

   ' Step 2 - Install the host instance using MSBTS_HostInstance class
   Set objHostInstance = objService.Get ("MSBTS_HostInstance")

   Set objHI = objHostInstance.SpawnInstance_

   objHI.Name = "Microsoft BizTalk Server " & HostName & " " & ServerName
   
   ' Invoke MSBTS_HostInstance Install method
   objHI.Install uid, pwd, true   ' Calling MSBTS_HostInstance::Install(string Logon, string Password, boolean GrantLogOnAsService) method

   CheckWMIError
   wscript.echo "HostInstance - " & HostName & " - has been installed successfully - " & ServerName
   
end Sub



Sub UpdateReceiveHandler (AdapterName, HostName, HostNameToSwitchTo)
   On Error Resume Next

   Dim Query, ReceiveHandlerInstSet, Inst

   ' Look for the target WMI Class MSBTS_ReceiveHandler instance
   Query = "SELECT * FROM MSBTS_ReceiveHandler WHERE AdapterName =""" & AdapterName & """ AND HostName = """ & HostName & """"
   Set ReceiveHandlerInstSet = GetObject("Winmgmts:!root\MicrosoftBizTalkServer").ExecQuery(Query)
   
   If ReceiveHandlerInstSet.Count > 0 Then
      For Each Inst In ReceiveHandlerInstSet
         ' Update host association
         Inst.HostNameToSwitchTo = HostNameToSwitchTo
         Inst.Put_(UpdateOnly)
         
         ' Check for error condition before continuing.
         CheckWMIError
         wscript.echo "Receive Handler - " & AdapterName & " " & HostNameToSwitchTo & " - has been updated sucessfully"
      Next
   Else
      wscript.echo "Receive Handler - " & AdapterName & " " & HosTName & " - cannot be found"
      wscript.quit 0
   End If

end Sub

' Uninstall and unmap a host instance using MSBTS_ServerHost and MSBTS_HostInstance
Sub UnMapUninstallHostInstance (HostName, ServerName)
   On Error Resume Next

   Dim Query, HostInstanceName, HostInstSet, Inst, ServerHostSet

   HostInstanceName = "Microsoft BizTalk Server " & HostName & " " & ServerName

   ' Step 1 - Uninstall the host instance using MSBTS_HostInstance class
   ' Only one instance will be returned becasue Name value is unique
   Query = "SELECT * FROM MSBTS_HostInstance WHERE Name =""" & HostInstanceName & """"
   Set HostInstSet = GetObject("Winmgmts:!root\MicrosoftBizTalkServer").ExecQuery(Query)

   If HostInstSet.Count > 0 Then
      For Each Inst in HostInstSet

         ' If host instance is running, then we need to first stop it
             If( HostInstServiceState_Running = Inst.ServiceState ) Then
            wscript.echo "Stopping host instance..."
                Inst.Stop   ' Calling MSBTS_HostInstance::Stop() method
            CheckWMIError
            wscript.echo "HostInstance - " & HostName & " - has been stopped successfully on server - " & ServerName
         End If
         
         If ( HostInstConfigState_NotInstalled <> Inst.ConfigurationState ) Then
            
            Inst.uninstall      ' Calling MSBTS_HostInstance::Uninstall() method
            CheckWMIError
            wscript.echo "HostInstance - " & HostName & " - has been uninstalled successfully from server - " & ServerName
         End If
      Next
   End If

   ' Step 2 - Delete mapping between server and host using MSBTS_ServerHost class
   ' Only one instance will be returned from this query
   Query = "SELECT * FROM MSBTS_ServerHost WHERE HostName =""" & HostName & """ AND ServerName = """ & ServerName & """"
   Set ServerHostSet = GetObject("Winmgmts:!root\MicrosoftBizTalkServer").ExecQuery(Query)

   If ServerHostSet.Count > 0 Then
      For Each Inst In ServerHostSet
             If( -1 = Inst.IsMapped ) Then
            
                Inst.Unmap   ' Calling MSBTS_ServerHost::Unmap() method
            CheckWMIError

            wscript.echo "HostInstance - " & HostName & " - has been unmapped successfully from server - " & ServerName
         Else
            wscript.echo "HostInstance - " & HostName & " - is not unmapped from server - " & ServerName
             End If
      Next
   Else
      wscript.echo "Server """ & ServerName & """ and Host """ & HostName & """ cannot be unmapped because either the specified server or host is invalid."
      wscript.quit 0
   End If

end Sub

' Sample to show MSBTS_ReceiveHandler instance creation with CustomCfg property
' handlerType = "MSBTS_ReceiveHandler" | "MSBTS_SendHandler2";
Sub CreateReceiveHandler (HostName, adapterName, handlerType)
   On Error Resume Next
   
   Dim objLocator, objService, objReceiveHandler, objRH
   
   ' Connects to local server WMI Provider BizTalk namespace
   Set objLocator = Createobject ("wbemScripting.SWbemLocator")
   Set objService = objLocator.ConnectServer(".", "root/MicrosoftBizTalkServer")

   ' Get WMI class MSBTS_ReceiveHandler
   Set objReceiveHandler = objService.Get (handlerType)

   Set objRH = objReceiveHandler.SpawnInstance_

   objRH.AdapterName = adapterName
   objRH.HostName = HostName
  
   ' Create instance
   objRH.Put_(CreateOnly)

   CheckWMIError
   wscript.echo handlerType & " for " & adapterName & " " & HostName & " - has been created successfully"
   
end Sub

Sub DeleteReceiveHandler (HostName, adapterName, handlerType)
   On Error Resume Next
   Dim objLocator, objService, objReceiveHandler, objRH
   
   ' Connects to local server WMI Provider BizTalk namespace
   Set objLocator = Createobject ("wbemScripting.SWbemLocator")
   Set objService = objLocator.ConnectServer(".", "root/MicrosoftBizTalkServer")

   ' Get WMI class MSBTS_ReceiveHandler
   Set objReceiveHandler = objService.Get (handlerType)

   Set objRH = objReceiveHandler.SpawnInstance_

   objRH.AdapterName = adapterName
   objRH.HostName = HostName
  
   ' Create instance
   objRH.Delete_

   CheckWMIError
   wscript.echo handlerType & " for " & adapterName & " " & HostName & " - has been created successfully"
   
end Sub

'This subroutine deals with all errors using the WbemScripting object.  Error descriptions
'are returned to the user by printing to the console.
Sub   CheckWMIError()

   If Err <> 0   Then
      On Error Resume   Next

      Dim strErrDesc: strErrDesc = Err.Description
      Dim ErrNum: ErrNum = Err.Number
      Dim WMIError : Set WMIError = CreateObject("WbemScripting.SwbemLastError")

      If ( TypeName(WMIError) = "Empty" ) Then
         wscript.echo strErrDesc & " (HRESULT: "   & Hex(ErrNum) & ")."
      Else
         wscript.echo WMIError.Description & "(HRESULT: " & Hex(ErrNum) & ")."
         Set WMIError = nothing
      End   If
      
      wscript.quit 0
   End If

End Sub