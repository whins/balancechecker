##
## Copyright (C) Huawei Technologies Co., Ltd. 2004-2005. All rights reserved.
##


    �������Ϊ���ݿ��ն��豸��������İ�װ����, ��DevSetupĿ¼����Win2K/WinXP������Ŀ¼, �ֱ𱣴����豸Win2000��WinXP����������. ��������ṩΪ������Ա���ж��ο���;

DevSetup.dllʹ��˵��:

1, DWORD DevSetup(BYTE byType, bool bDebug);
˵��: ����ʵ����������İ�װ��ɾ������ѯ���������ܣ����Դ򿪡��رյ�����Ϣ�������

byType: ��ʾ�������ͣ�ȡֵ���£�
	0�� ��װE600������
	1�� ɾ��E600������
	2�� ��ѯ��ǰϵͳ�Ƿ��Ѿ���װE600��������
	3�� ���������Ϣ��

bDebug���򿪡��رյ�����Ϣ�������ȡֵ���£�
	true: 	�򿪵�����Ϣ�������
	false: 	�رյ�����Ϣ�������

����0��ʾ�����ɹ���������ʾ����ʧ�ܣ�ȡֵ�μ��������롱��


2, DWORD Install(void);
˵��: ��װE600������

����0��ʾ�����ɹ���������ʾ����ʧ�ܣ�ȡֵ�μ��������롱��

3, DWORD Uninstall(void);
˵��: ɾ��E600������

����0��ʾ�����ɹ���������ʾ����ʧ�ܣ�ȡֵ�μ��������롱��

4, DWORD Detect(void);
˵��: ��ѯ��ǰϵͳ�Ƿ��Ѿ���װE600��������

����0��ʾ�����ɹ���������ʾ����ʧ�ܣ�ȡֵ�μ��������롱��

5, DWORD Help(void);
˵��: ���������Ϣ��

����0��ʾ�����ɹ���������ʾ����ʧ�ܣ�ȡֵ�μ��������롱��


6, DWORD Scan(DWORD dwTime, bool bDebug, DEVSETUP_CALLBACK_TYPE CallBack);;
˵��: ɨ�����ݿ��豸����װ��������, (Ŀǰ�������ṩ, Ҳ���ṩӢ��˵��)��

dwTime: ɨ���豸�����ʱ��, ��λ(��);

bDebug���򿪡��رյ�����Ϣ�������ȡֵ���£�
	true: 	�򿪵�����Ϣ�������
	false: 	�رյ�����Ϣ�������

CallBack: ��������ô˻ص�����, ����ɨ�赽�豸����ʱ����ô˺���֪ͨ������, Ŀǰ�ص�������������.
    ��������: typedef void(*DEVSETUP_CALLBACK_TYPE)(DWORD dwStauts, void* pData);
    
����0��ʾ�����ɹ���������ʾ����ʧ�ܣ�ȡֵ�μ��������롱��

DevSetup.exeʹ��˵��:

1, Install
���� [·��/]DevSetup /install

2, Uninstall
���� [·��/]DevSetup /uninstall

3, Detect
���� [·��/]DevSetup /detect

4, Usage
����  [·��/]DevSetup /help �� [·��/]DevSetup ?

ִ�н��:
    ִ�гɹ�����0;
    ִ�г�����������Ӧ������:



˵��:
    ����װ����ִ����ɺ���Ҫ���²���豸�����ϵͳ����CM_Locate_DevNode��ˢ��ϵͳ���������б�ֻ�е�ϵͳ���¼����µ���������󣬲ű�ʾ��������װ��ɡ�



������:

// Ӧ���Ѿ�����/�Ѿ�����һ�������豸�����Ľ���;
#define DEV_SETUP_ERROR_ALREADY_EXISTS	    	DWORD(20001)	
// �����в�������;
#define DEV_SETUP_ERROR_INVALID_CMD 		DWORD(20002)	
// ��֧�ֵ�WINDOWS�汾;
#define DEV_SETUP_ERROR_INVALID_SYS_VER	    	DWORD(20003)	
// û���ҵ�������װ�ļ�;
#define DEV_SETUP_ERROR_EMPTY_DIR		DWORD(20004)
// ��װ�����ļ�ʧ��;
#define DEV_SETUP_ERROR_INSTALL_FAIL		DWORD(20005)
// ɾ����������ʧ��;
#define DEV_SETUP_ERROR_REMOVE_FAIL		DWORD(20006)
// û���ҵ�����;
#define DEV_SETUP_DRIVER_NOT_EXISTS         	DWORD(10000)
// �����Ѿ�����;
#define DEV_SETUP_DRIVER_ALREADY_EXISTS     	DWORD(10001)


DevSetup.exeʹ�÷���(����):

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


