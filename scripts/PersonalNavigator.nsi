; Per-user NSIS 3 installer for Personal Navigator.
Unicode true
!include "MUI2.nsh"
!ifndef APP_VERSION
  !error "Define APP_VERSION with /DAPP_VERSION=x.y.z"
!endif
!ifndef PUBLISH_DIR
  !error "Define PUBLISH_DIR"
!endif
!ifndef OUTPUT_DIR
  !error "Define OUTPUT_DIR"
!endif
!ifndef SOURCE_ROOT
  !error "Define SOURCE_ROOT"
!endif
Name "Personal Navigator"
OutFile "${OUTPUT_DIR}\PersonalNavigator-Setup-win-x64.exe"
InstallDir "$LOCALAPPDATA\Programs\Personal Navigator"
InstallDirRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "InstallLocation"
RequestExecutionLevel user
SetCompressor /SOLID lzma
ShowInstDetails show
ShowUninstDetails show
VIProductVersion "${APP_VERSION}.0"
VIAddVersionKey "ProductName" "Personal Navigator"
VIAddVersionKey "FileDescription" "Personal Navigator Windows Installer"
VIAddVersionKey "FileVersion" "${APP_VERSION}"
VIAddVersionKey "ProductVersion" "${APP_VERSION}"

!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\PersonalNavigator.exe"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${SOURCE_ROOT}\LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

Section "Personal Navigator"
  SetShellVarContext current
  SetOutPath "$INSTDIR"
  IfFileExists "$INSTDIR\PersonalNavigator.exe" 0 +3
    MessageBox MB_OKCANCEL|MB_ICONINFORMATION "Setup will close any running Personal Navigator process before updating. Your settings and index are preserved." IDOK +2
    Abort
  nsExec::ExecToLog '"$SYSDIR\taskkill.exe" /F /IM PersonalNavigator.exe'
  File "/oname=PersonalNavigator.exe" "${PUBLISH_DIR}\PersonalNavigator.exe"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  CreateDirectory "$SMPROGRAMS\Personal Navigator"
  CreateShortcut "$SMPROGRAMS\Personal Navigator\Personal Navigator.lnk" "$INSTDIR\PersonalNavigator.exe"
  CreateShortcut "$SMPROGRAMS\Personal Navigator\Uninstall Personal Navigator.lnk" "$INSTDIR\Uninstall.exe"
  CreateShortcut "$SENDTO\Personal Navigator.lnk" "$INSTDIR\PersonalNavigator.exe"
  WriteRegStr HKCU "Software\Classes\Directory\Background\shell\PersonalNavigator" "MUIVerb" "Open Personal Map"
  WriteRegStr HKCU "Software\Classes\Directory\Background\shell\PersonalNavigator" "Icon" "$INSTDIR\PersonalNavigator.exe"
  WriteRegStr HKCU "Software\Classes\Directory\Background\shell\PersonalNavigator\command" "" '"$INSTDIR\PersonalNavigator.exe" "%V"'
  WriteRegStr HKCU "Software\Classes\Directory\shell\PersonalNavigator" "MUIVerb" "Open Personal Map"
  WriteRegStr HKCU "Software\Classes\Directory\shell\PersonalNavigator" "Icon" "$INSTDIR\PersonalNavigator.exe"
  WriteRegStr HKCU "Software\Classes\Directory\shell\PersonalNavigator\command" "" '"$INSTDIR\PersonalNavigator.exe" "%1"'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "PersonalNavigator" '"$INSTDIR\PersonalNavigator.exe" --background'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "DisplayName" "Personal Navigator"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "Publisher" "Personal Navigator contributors"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "URLInfoAbout" "https://github.com/linux-cmd/personal-navigator"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "DisplayIcon" "$INSTDIR\PersonalNavigator.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "NoRepair" 1
SectionEnd

Section "Uninstall"
  SetShellVarContext current
  nsExec::ExecToLog '"$SYSDIR\taskkill.exe" /F /IM PersonalNavigator.exe'
  Delete "$INSTDIR\PersonalNavigator.exe"
  Delete "$INSTDIR\Uninstall.exe"
  Delete "$SMPROGRAMS\Personal Navigator\Personal Navigator.lnk"
  Delete "$SMPROGRAMS\Personal Navigator\Uninstall Personal Navigator.lnk"
  RMDir "$SMPROGRAMS\Personal Navigator"
  Delete "$SENDTO\Personal Navigator.lnk"
  DeleteRegKey HKCU "Software\Classes\Directory\Background\shell\PersonalNavigator"
  DeleteRegKey HKCU "Software\Classes\Directory\shell\PersonalNavigator"
  DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "PersonalNavigator"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator"
  RMDir "$INSTDIR"
  ; Preserve %LOCALAPPDATA%\PersonalNavigator preferences and cache.
SectionEnd
