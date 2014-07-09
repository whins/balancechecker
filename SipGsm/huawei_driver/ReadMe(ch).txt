##
## Copyright (C) Huawei Technologies Co., Ltd. 2004-2005. All rights reserved.
##


    本程序包为数据卡终端设备驱动程序的安装程序, 在DevSetup目录下有Win2K/WinXP两个子目录, 分别保存有设备Win2000与WinXP的驱动程序. 本程序包提供为开发人员进行二次开发;

DevSetup.dll使用说明:

1, DWORD DevSetup(BYTE byType, bool bDebug);
说明: 可以实现驱动程序的安装、删除、查询、帮助功能，可以打开、关闭调试信息的输出。

byType: 表示操作类型，取值如下：
	0： 安装E600驱动；
	1： 删除E600驱动；
	2： 查询当前系统是否已经安装E600驱动程序；
	3： 输出帮助信息；

bDebug：打开、关闭调试信息的输出，取值如下：
	true: 	打开调试信息的输出；
	false: 	关闭调试信息的输出；

返回0表示操作成功，其它表示操作失败，取值参见“错误码”。


2, DWORD Install(void);
说明: 安装E600驱动；

返回0表示操作成功，其它表示操作失败，取值参见“错误码”。

3, DWORD Uninstall(void);
说明: 删除E600驱动；

返回0表示操作成功，其它表示操作失败，取值参见“错误码”。

4, DWORD Detect(void);
说明: 查询当前系统是否已经安装E600驱动程序；

返回0表示操作成功，其它表示操作失败，取值参见“错误码”。

5, DWORD Help(void);
说明: 输出帮助信息；

返回0表示操作成功，其它表示操作失败，取值参见“错误码”。


6, DWORD Scan(DWORD dwTime, bool bDebug, DEVSETUP_CALLBACK_TYPE CallBack);;
说明: 扫描数据卡设备并安装驱动程序, (目前不对外提供, 也不提供英文说明)；

dwTime: 扫描设备插入的时长, 单位(秒);

bDebug：打开、关闭调试信息的输出，取值如下：
	true: 	打开调试信息的输出；
	false: 	关闭调试信息的输出；

CallBack: 如果有设置此回调函数, 则在扫描到设备插入时会调用此函数通知调用者, 目前回调函数参数保留.
    函数类型: typedef void(*DEVSETUP_CALLBACK_TYPE)(DWORD dwStauts, void* pData);
    
返回0表示操作成功，其它表示操作失败，取值参见“错误码”。

DevSetup.exe使用说明:

1, Install
调用 [路径/]DevSetup /install

2, Uninstall
调用 [路径/]DevSetup /uninstall

3, Detect
调用 [路径/]DevSetup /detect

4, Usage
调用  [路径/]DevSetup /help 或 [路径/]DevSetup ?

执行结果:
    执行成功返回0;
    执行出错返回如下相应错误码:



说明:
    本安装程序执行完成后，需要重新插拔设备或调用系统函数CM_Locate_DevNode来刷新系统驱动程序列表，只有当系统重新加载新的驱动程序后，才表示驱动程序安装完成。



错误码:

// 应用已经存在/已经存在一个操作设备驱动的进程;
#define DEV_SETUP_ERROR_ALREADY_EXISTS	    	DWORD(20001)	
// 命令行参数错误;
#define DEV_SETUP_ERROR_INVALID_CMD 		DWORD(20002)	
// 不支持的WINDOWS版本;
#define DEV_SETUP_ERROR_INVALID_SYS_VER	    	DWORD(20003)	
// 没有找到驱动安装文件;
#define DEV_SETUP_ERROR_EMPTY_DIR		DWORD(20004)
// 安装驱动文件失败;
#define DEV_SETUP_ERROR_INSTALL_FAIL		DWORD(20005)
// 删除已有驱动失败;
#define DEV_SETUP_ERROR_REMOVE_FAIL		DWORD(20006)
// 没有找到驱动;
#define DEV_SETUP_DRIVER_NOT_EXISTS         	DWORD(10000)
// 驱动已经存在;
#define DEV_SETUP_DRIVER_ALREADY_EXISTS     	DWORD(10001)


DevSetup.exe使用方法(例程):

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


