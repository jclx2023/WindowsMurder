using UnityEngine;
using TMPro;

/// <summary>
/// ��ʼ��α��� bat �ļ���������
/// </summary>
public class BatWindowInitializer : MonoBehaviour
{
    [Header("Reference")]
    public TMP_InputField inputField; // ���ı���

    private void Start()
    {

        // ����α������������ݣ�������α���룩
        inputField.text = GetFakeBatContent();
    }

    /// <summary>
    /// ����α��� BAT �ļ�α���루�����¡�����ִ�У�
    /// </summary>
    private string GetFakeBatContent()
    {
        return
@"@echo off
REM ============================================
REM  WindowsMurder �� 'Merge & Seal' utility (F I C T I O N A L)
REM  NOTE: THIS FILE IS PURELY NARRATIVE PSEUDOCODE.
REM  IT IS NOT REAL, IT WILL NOT RUN, AND MUST NOT BE USED FOR REAL SYSTEM ACTIONS.
REM ============================================

:: ---------- meta ----------
echo [*] Merge&Seal v1.4 �� narrative build
echo [*] Mode: pack -> shell -> deploy (SIMULATED)
echo.

:: ---------- environment (fictional) ----------
set WORK_DIR=%~dp0
set OUTPUT_TEXT=%WORK_DIR%sealed_output.txt
set STAGING_MARKER=%WORK_DIR%._staging_flag

echo [*] staging workspace: %WORK_DIR%
echo [*] creating virtual package descriptor...
echo filelist: knife.jpg, log_0930.txt, goodbye.txt, system_report.ini > %STAGING_MARKER%

:: ---------- collect & bundle (simulated) ----------
echo [*] collecting listed files into a single package (SIMULATED)
:: (In-game this would append binary archive bytes after a text header.
::  Here we only record the action for the story.)
echo [SIMULATED_PACKAGE_START] >> %OUTPUT_TEXT%
echo [PACKED_CONTENT_PLACEHOLDER] >> %OUTPUT_TEXT%
echo [SIMULATED_PACKAGE_END] >> %OUTPUT_TEXT%

:: ---------- privilege escalation (fictional narrative) ----------
echo [*] requesting elevated execution context from Control Panel (SIMULATED)
:: In the fiction, Control Panel approves and spawns a privileged task runner.
echo [SIMULATION] Requesting 'ControlPanel' privilege token... Granted (fictional)

:: ---------- execute tool.cps (fictional) ----------
echo [*] invoking tool.cps with ControlPanel privileges (SIMULATED)
:: tool.cps is described in-game as the 'direct instrument' that disables/restores entity bindings of RecycleBin.
:: The execution is represented here as a narrative event, not a command.
echo [SIMULATION] tool.cps executed -> target: RecycleBin (fictional effect logged)

:: ---------- disguise & self-modify (simulated) ----------
echo [*] disguising package as plain text shell...
:: The script 'wraps' the binary container inside a benign-looking text file cover.
:: It then writes a 'shell signature' to indicate the disguise has been applied.
echo [SIMULATED] cover text appended to %OUTPUT_TEXT%
echo [SIMULATED] signature: SHELL_OK >> %OUTPUT_TEXT%

:: ---------- create sealed-room (in-game flag, simulated) ----------
echo [*] sealing parent folder to simulate locked room (SIMULATED)
:: NOTE: In-game we do NOT modify real OS permissions. Instead we set an in-world 'lock flag'
:: that other game systems read to render the folder as inaccessible.
echo [IN-GAME FLAG] PARENT_FOLDER_LOCKED=TRUE

:: ---------- cleanup & cover tracks (simulated) ----------
echo [*] removing staging artifacts and traces (SIMULATED)
del %STAGING_MARKER% >nul 2>nul
echo [SIMULATION] audit logs truncated (fictional narrative)

echo.
echo [REPORT]
echo  - Package created: %OUTPUT_TEXT%
echo  - Tool executed: tool.cps (ControlPanel context) [fictional]
echo  - Parent folder: sealed (in-game lock flag set)
echo  - Notes: this file intentionally leaves a 'benign' looking cover to mislead investigators
echo.
echo Operation complete (SIMULATED). Press any key to close.
pause >nul
exit /b 0";
    }
}
