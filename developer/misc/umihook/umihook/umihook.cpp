// umihook.cpp : Defines the entry point for the console application.
// Required DLLs
// - EasyHook DLL "EasyHook32.dll"	
// - MPV DLL "mpv-1.dll" from https://mpv.srsfckn.biz/
// - You probably should get the latest MPV and EasyHook versions and 
// use their DLL, .lib/.a files, and header files incase something 
// has changed. Or copy the DLL files from my prepackaged version

// TODO: tidy code
// TODO: clean up imports

// Including SDKDDKVer.h defines the highest available Windows platform.

// If you wish to build your application for a previous Windows platform, include WinSDKVer.h and
// set the _WIN32_WINNT macro to the platform you wish to support before including SDKDDKVer.h.

#include <SDKDDKVer.h>

#include <stdio.h>
#include <tchar.h>
#include "../shared/easyhook.h"
#include "../shared/shared_functions.h"

const _TCHAR exe_name[] = L"Umineko1to4.exe";
DWORD volume = 100;

int _tmain(int argc, _TCHAR* argv[])
{
	wprintf(L"Usage: umihook [exe name] [volume]\n");
	wprintf(L"Defaults: [%s] [%d]\n\n", exe_name, volume);

	_TCHAR non_const_exe_name[sizeof(exe_name)];
	wcscpy_s(non_const_exe_name, exe_name);

	_TCHAR * hooked_exe_name = non_const_exe_name;

	// if exe name was passed in, use that instead of the default
	if (argc > 1)
	{
		hooked_exe_name = argv[1];
		wprintf(L"Will try to hook %s\n", argv[1]);
	}

	// if volume was passed in, override the default volume
	if (argc > 2)
	{
		volume = _wtoi(argv[2]);
		wprintf(L"Will set volume to %d\n", volume);
	}

	//second argument to CreateProcess MUST be non const! How do you enable warnings for this on Visual Studio??
	PROCESS_INFORMATION pInfo = { 0 };
	BOOL hooking_result;
	if(makeProcessWindows(hooked_exe_name, &pInfo, 0))
	{
		wprintf(L"Process id: %d\n", pInfo.dwProcessId);
	}
	else
	{
		wprintf(L"Couldn't find/start game! %s\n", exe_name);
		press_any_key_to_exit();
	}
	
	
	WCHAR* dllToInject = L"umihookdll.dll";
	wprintf(L"Attempting to inject: %s\n\n", dllToInject);

	// Inject dllToInject into the target process Id, passing 
	// freqOffset as the pass through data.
	NTSTATUS nt = RhInjectLibrary(
		pInfo.dwProcessId,   // The process to inject into
		0,           // ThreadId to wake up upon injection
		EASYHOOK_INJECT_DEFAULT,
		dllToInject, // 32-bit
		NULL,		 // 64-bit not provided
		&volume, //&hPipe,		//&freqOffset, // data to send to injected DLL entry point
		sizeof(DWORD)///sizeof(HANDLE)			//sizeof(DWORD)// size of data to send
	);

	if (nt == 0)
	{
		wprintf(L"Library injected successfully.\n");
	}
	else
	{
		printf("RhInjectLibrary failed with error code = %d\n", nt);
		PWCHAR err = RtlGetLastErrorString();
		wprintf(L"%s\n", err);
		press_any_key_to_exit();
	}
	
	//wait for game to close (by user exiting)
	puts("Waiting for game to exit...!\n");
	WaitForSingleObject(pInfo.hProcess, INFINITE);
	puts("Game has exited!\n");

	//close the process handles for the game
	CloseHandle(pInfo.hProcess);
	CloseHandle(pInfo.hThread);

	return 0;
}
