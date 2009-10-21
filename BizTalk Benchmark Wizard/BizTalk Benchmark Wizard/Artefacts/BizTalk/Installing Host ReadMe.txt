

Use the InstallHosts.vbs to install Hosts, Host Instances and Adapter handlers

' Paramaters:
'   0 : NTGroupName
'   1 : UserName
'   2 : Password
'   3 : Receive Host
'   4 : Send Host
'   5 : Processing Host

Usages: cscript InstallHosts.vbs [NTGroupName] [UserName] [Password] [Receive Host] [Send Host] [Processing Host]
Sample: cscript InstallHosts.vbs "BizTalk Application Users" \MyUser MyPassword BtsServer1 BtsServer2 BtsServer2




Use the RemoveHosts.vbs to install Hosts, Host Instances and Adapter handlers

' Paramaters:
'   0 : NTGroupName
'   1 : UserName
'   2 : Password
'   3 : Receive Host
'   4 : Send Host
'   5 : Processing Host

Usages: cscript RemoveHosts.vbs [NTGroupName] [UserName] [Password] [Receive Host] [Send Host] [Processing Host]
Sample: cscript RemoveHosts.vbs "BizTalk Application Users" \MyUser MyPassword BtsServer1 BtsServer2 BtsServer2