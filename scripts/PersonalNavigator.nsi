; Per-user NSIS 3 installer for Personal Navigator.
Unicode true
!include "MUI2.nsh"
!ifndef APP_VERSION
  !error "Define APP_VERSION with /DAPP_VERSION=x.y.z"
!endif
!ifndef APP_FILE_VERSION
  !error "Define APP_FILE_VERSION with /DAPP_FILE_VERSION=x.y.z.0"
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
OutFile "${OUTPUT_DIR}\PersonalNavigator-Setup-${APP_VERSION}-win-x64.exe"
InstallDir "$PROGRAMFILES64\Personal Navigator"
InstallDirRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "InstallLocation"
RequestExecutionLevel admin
SetCompressor /SOLID lzma
ShowInstDetails show
ShowUninstDetails show
VIProductVersion "${APP_FILE_VERSION}"
VIAddVersionKey "ProductName" "Personal Navigator"
VIAddVersionKey "FileDescription" "Personal Navigator Windows Installer"
VIAddVersionKey "FileVersion" "${APP_VERSION}"
VIAddVersionKey "ProductVersion" "${APP_VERSION}"
VIAddVersionKey "LegalCopyright" "Copyright 2026 Abhijay Panwar"

!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\PersonalNavigator.exe"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${SOURCE_ROOT}\LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

Section "Personal Navigator"
  SectionIn RO
  SetShellVarContext all
  SetOutPath "$INSTDIR"
  IfFileExists "$INSTDIR\PersonalNavigator.exe" 0 +3
    MessageBox MB_OKCANCEL|MB_ICONINFORMATION "Setup will close any running Personal Navigator process before updating. Your settings and index are preserved." IDOK +2
    Abort
  nsExec::ExecToLog '"$SYSDIR\taskkill.exe" /F /IM PersonalNavigator.exe'
  ; Remove the files and registration used by the earlier per-user installer.
  Delete "$LOCALAPPDATA\Programs\Personal Navigator\PersonalNavigator.exe"
  Delete "$LOCALAPPDATA\Programs\Personal Navigator\Uninstall.exe"
  RMDir "$LOCALAPPDATA\Programs\Personal Navigator"
  DeleteRegKey HKCU "Software\Classes\Directory\Background\shell\PersonalNavigator"
  DeleteRegKey HKCU "Software\Classes\Directory\shell\PersonalNavigator"
  DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "PersonalNavigator"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator"
  SetShellVarContext current
  Delete "$SMPROGRAMS\Personal Navigator\Personal Navigator.lnk"
  Delete "$SMPROGRAMS\Personal Navigator\Uninstall Personal Navigator.lnk"
  RMDir "$SMPROGRAMS\Personal Navigator"
  Delete "$SENDTO\Personal Navigator.lnk"
  SetShellVarContext all
  File "/oname=PersonalNavigator.exe" "${PUBLISH_DIR}\PersonalNavigator.exe"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  CreateDirectory "$SMPROGRAMS\Personal Navigator"
  CreateShortcut "$SMPROGRAMS\Personal Navigator\Personal Navigator.lnk" "$INSTDIR\PersonalNavigator.exe"
  CreateShortcut "$SMPROGRAMS\Personal Navigator\Uninstall Personal Navigator.lnk" "$INSTDIR\Uninstall.exe"
  CreateShortcut "$SENDTO\Personal Navigator.lnk" "$INSTDIR\PersonalNavigator.exe"
  WriteRegStr HKLM "Software\Classes\Directory\Background\shell\PersonalNavigator" "MUIVerb" "Open Personal Map"
  WriteRegStr HKLM "Software\Classes\Directory\Background\shell\PersonalNavigator" "Icon" "$INSTDIR\PersonalNavigator.exe"
  WriteRegStr HKLM "Software\Classes\Directory\Background\shell\PersonalNavigator\command" "" '"$INSTDIR\PersonalNavigator.exe" "%V"'
  WriteRegStr HKLM "Software\Classes\Directory\shell\PersonalNavigator" "MUIVerb" "Open Personal Map"
  WriteRegStr HKLM "Software\Classes\Directory\shell\PersonalNavigator" "Icon" "$INSTDIR\PersonalNavigator.exe"
  WriteRegStr HKLM "Software\Classes\Directory\shell\PersonalNavigator\command" "" '"$INSTDIR\PersonalNavigator.exe" "%1"'
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "DisplayName" "Personal Navigator"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "Publisher" "Abhijay Panwar"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "URLInfoAbout" "https://github.com/linux-cmd/personal-navigator"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "DisplayIcon" "$INSTDIR\PersonalNavigator.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator" "NoRepair" 1
SectionEnd

Section /o "Desktop shortcut" DesktopShortcut
  SetShellVarContext all
  CreateShortcut "$DESKTOP\Personal Navigator.lnk" "$INSTDIR\PersonalNavigator.exe"
SectionEnd

Section "Start with Windows" StartupShortcut
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Run" "PersonalNavigator" '"$INSTDIR\PersonalNavigator.exe" --background'
SectionEnd

Section "Uninstall"
  SetShellVarContext all
  nsExec::ExecToLog '"$SYSDIR\taskkill.exe" /F /IM PersonalNavigator.exe'
  Delete "$INSTDIR\PersonalNavigator.exe"
  Delete "$INSTDIR\Uninstall.exe"
  Delete "$SMPROGRAMS\Personal Navigator\Personal Navigator.lnk"
  Delete "$SMPROGRAMS\Personal Navigator\Uninstall Personal Navigator.lnk"
  RMDir "$SMPROGRAMS\Personal Navigator"
  Delete "$SENDTO\Personal Navigator.lnk"
  Delete "$DESKTOP\Personal Navigator.lnk"
  DeleteRegKey HKLM "Software\Classes\Directory\Background\shell\PersonalNavigator"
  DeleteRegKey HKLM "Software\Classes\Directory\shell\PersonalNavigator"
  DeleteRegValue HKLM "Software\Microsoft\Windows\CurrentVersion\Run" "PersonalNavigator"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\PersonalNavigator"
  RMDir "$INSTDIR"
  ; Preserve %LOCALAPPDATA%\PersonalNavigator preferences and cache.
SectionEnd
