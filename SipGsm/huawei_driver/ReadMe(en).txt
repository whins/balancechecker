##
## Copyright (C) Huawei Technologies Co., Ltd. 2004-2005. All rights reserved.
##


How to use: DevSetup.dll

1, DWORD DevSetup(BYTE byType, bool bDebug);
Descprition: 
     Install function, Uninstall funtion, Query funciton, Help funciton
     Open/Colse debug informaiton

patameter:
    byType: function type 
         0: Install the driver;
         1: Uninstall the driver;
         2: Duery whether we have installed driver in the Windows operating system;
         3: Help information;

    bDebug: Open/Colse debug informaiton
         true:  Open the debug information
         false: Close the debug information 

return value:
        0: success
        other value: failure, please refer to the "Failure code"


2, DWORD Install(void);
Descprition: 
    Install the driver;

return value:
    0: success
    other value: failure, please refer to the "Failure code"

3, DWORD Uninstall(void);
Descprition: 
    Uninstall the driver;

return value:
    0: success
    other value: failure, please refer to the "Failure code"

4, DWORD Detect(void);
Descprition: 
    Duery whether we have installed driver in the Windows operating system;

return value:
    0: success
    other value: failure, please refer to the "Failure code"

5, DWORD Help(void);
Descprition: 
    Help information;

return value:
    0: success
    other value: failure, please refer to the "Failure code"



How to use: DevSetup.exe

1, Install
EXEC [PATH/]DevSetup /install

2, Uninstall
EXEC [PATH/]DevSetup /uninstall

3, Detect
EXEC [PATH/]DevSetup /detect

4, Usage
EXEC [PATH/]DevSetup /help OR EXEC [PATH/]DevSetup ?

return value:
    0: success
    other value: failure, please refer to the "Failure code"



Failure code

// have already had a Process in the operating system which handle the driver;
#define DEV_SETUP_ERROR_ALREADY_EXISTS	    	DWORD(20001)	

// Command parameter error
#define DEV_SETUP_ERROR_INVALID_CMD 		    DWORD(20002)	

// Do not support this Windows version
#define DEV_SETUP_ERROR_INVALID_SYS_VER	    	DWORD(20003)	

// Can not find driver install file
#define DEV_SETUP_ERROR_EMPTY_DIR		        DWORD(20004)

// Install failure 
#define DEV_SETUP_ERROR_INSTALL_FAIL		    DWORD(20005)

// Delete the driver failure;
#define DEV_SETUP_ERROR_REMOVE_FAIL		        DWORD(20006)

// Do not find the driver;
#define DEV_SETUP_DRIVER_NOT_EXISTS         	DWORD(10000)

// driver already exist;
#define DEV_SETUP_DRIVER_ALREADY_EXISTS     	DWORD(10001)


Sample Code(DevSetup.exe):

#include <Windows.h>
#include <Winbase.h>

#include <stdio.h>

void main(void)
{
    STARTUPINFO si;
    PROCESS_INFORMATION pi;

    ZeroMemory( &si, sizeof(si) );
    si.cb = sizeof(si);
    ZeroMemory( &pi, sizeof(pi) );

    // Start the child process. 
    if( !CreateProcess( NULL, 		// No module name (use command line). 
        "DevSetup /install",       	// Command line. [/uninstall | /detect | /help]
        NULL,             		// Process handle not inheritable. 
        NULL,             		// Thread handle not inheritable. 
        FALSE,            		// Set handle inheritance to FALSE. 
        0,                		// No creation flags. 
        NULL,             		// Use parent's environment block. 
        NULL,             		// Use parent's starting directory. 
        &si,              		// Pointer to STARTUPINFO structure.
        &pi )             		// Pointer to PROCESS_INFORMATION structure.
    ) 
    {
        return;
    }

    // Wait until child process exits.
    WaitForSingleObject( pi.hProcess, INFINITE );

    DWORD   dwReturn;

    GetExitCodeProcess(pi.hProcess, &dwReturn);

    printf("%d", dwReturn);
    
    // Close process and thread handles. 
    CloseHandle( pi.hProcess );
    CloseHandle( pi.hThread );

    return;
}

