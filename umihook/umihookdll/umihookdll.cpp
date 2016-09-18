// umihookdll.cpp : Defines the exported functions for the DLL application.

/* Note on MPV

  Was previously using pipes to communicate with an external MPV provcess
  but now directly use libmpv as it is easier. I had a problem before where
  if the pipe was not read from, MPV's side of the pipe would become full 
  and stop working.

  However, using libmpv directly states that it will silently discard events
  
  >The internal event queue has a limited size (per client handle). If you
  >don't empty the event queue quickly enough with mpv_wait_event(), it will
  >overflow and silently discard further events. If this happens, making
  >asynchronous requests will fail as well (with MPV_ERROR_EVENT_QUEUE_FULL).
  >> from https://github.com/mpv-player/mpv/blob/master/libmpv/client.h

  Therefore a discard thread is not necessary.
*/

#define RUN_MPV_CODE 1
#define RUN_DISCARD_THREAD 0

//#include "stdafx.h"

#include "../shared/easyhook.h"
#include <Windows.h>
#include "stdio.h"
#include <malloc.h>	//needed for single "malloca" call

#include "../shared/shared_functions.h"

#include "mpv/client.h"

typedef FILE * (*FOPEN_FN_TYPE)(const char *, const char *);
typedef int (*FCLOSE_FN_TYPE)(FILE *);
typedef int (*FPUTS_FN_TYPE)(const char *, FILE *);
typedef int(*FFLUSH_FN_TYPE)(FILE *);
typedef int(*ATEXIT_FN_TYPE)(void(*func)(void));

FOPEN_FN_TYPE original_fopen;
FPUTS_FN_TYPE original_fputs;
FCLOSE_FN_TYPE original_fclose;
FFLUSH_FN_TYPE original_fflush;
ATEXIT_FN_TYPE original_atexit;

FILE * fDebug;

mpv_handle * ctx;

#define DPRINTF(x) fprintf(fDebug, x)

#if RUN_MPV_CODE
static inline void check_error(int status)
{
	if (status < 0) {
		original_fputs("mpv API error: ", fDebug);
		original_fputs(mpv_error_string(status), fDebug);
	}
}
#endif

void on_game_exit()
{
#if RUN_MPV_CODE
	mpv_terminate_destroy(ctx);
#endif

	original_fclose(fDebug);

	FreeConsole();
}

// file_path:				.\voice\10\10100041.ogg
// atrac3p_path:			.\voice\10\10100041.at3
// if not an .ogg file, file path and ext may be invalid (but that's OK)
// String manipulation in C is :-( could just use C++ strings for this bit
void intercept_file_access(const char * filename)
{
	unsigned int filename_length = strlen(filename);

	//exit if you get some bogus filename which is huge
	if (filename_length > 1000)
		return;
	
	//just assume the file extension is the last 3 characters, even if it's not
	const char * file_ext = str_end(filename, 3);

	//if not an ogg file, exit 
	if (strncmp(file_ext, "ogg", 3) != 0)
		return;

	//allocate space to store the new filename/path (plus null terminator)
	char * atrac3p_path = (char *) _alloca(filename_length + 1);
	strncpy(atrac3p_path, filename, filename_length+1);

	//overwrite the file extension, eg ".\voice\10\10100041.at3"
	strncpy(&atrac3p_path[filename_length - 3], "at3", 3);

	//check if the .at3 file exists before trying to play it. Required as otherwise 
	//mpv will stop the current track and "play" the non-existent file instead
	FILE * fptr = original_fopen(atrac3p_path, "r");
	if (fptr == NULL)
		return;
	else
		original_fclose(fptr);

#if RUN_MPV_CODE
	//call mpv to play voice here
	const char *cmd[] = { "loadfile", atrac3p_path, NULL };
	check_error(mpv_command(ctx, cmd));
#endif

#ifdef ENABLE_LOGGING
	original_fputs(cmd[0], fDebug);
	original_fputs(cmd[1], fDebug);
	original_fputs("\n", fDebug);
	original_fflush(fDebug);
#endif
}

// This function is called whenever the game calls 'fopen'
FILE * myFopen(const char * filename, const char * mode)
{
	intercept_file_access(filename);

	FILE * fp = original_fopen(filename, mode);
	return fp;
}

// EasyHook will be looking for this export to support DLL injection 
extern "C" void __declspec(dllexport) __stdcall NativeInjectionEntryPoint(REMOTE_ENTRY_INFO* inRemoteInfo);

void __stdcall NativeInjectionEntryPoint(REMOTE_ENTRY_INFO* inRemoteInfo)
{
	//game uses "msvcrt" while we use a different runtime (or something like that)
	HMODULE msvcrt_handle = GetModuleHandle(TEXT("msvcrt"));

	// must use the game's version of these functions, using our own versions seems
	// to conflict and cause the game to crash/functions to not work correctly
	original_fopen = (FOPEN_FN_TYPE)GetProcAddress(msvcrt_handle, "fopen");
	original_fputs = (FPUTS_FN_TYPE)GetProcAddress(msvcrt_handle, "fputs");
	original_fclose = (FCLOSE_FN_TYPE)GetProcAddress(msvcrt_handle, "fclose");
	original_fflush = (FFLUSH_FN_TYPE)GetProcAddress(msvcrt_handle, "fflush");
	original_atexit = (ATEXIT_FN_TYPE)GetProcAddress(msvcrt_handle, "atexit");

	//create a console for debugging. Close handle when game exits.
	AllocConsole();
	fDebug = original_fopen("CONOUT$", "w");

	// Perform hooking
	HOOK_TRACE_INFO hHook = { NULL }; // keep track of our hook
	// Install hook - game uses "msvcrt" not "ucrtbased"
	// This part had to be changed from the default EasyHook example
	NTSTATUS result = LhInstallHook(
		GetProcAddress(msvcrt_handle, "fopen"),		
		myFopen,
		NULL,
		&hHook);

#if RUN_MPV_CODE
	//create an mpv instance
	ctx = mpv_create();
	if (ctx) 
	{
		// Done setting up options.
		check_error(mpv_initialize(ctx));
		original_fputs("Sucess creating mpv instance!\n", fDebug);
	}
	else
	{
		original_fputs("failed to create mpv instance!\n", fDebug);
	}

	//desired volume was passed in remotely
	DWORD volume = 100;
	if (inRemoteInfo->UserDataSize == sizeof(DWORD))
	{
		volume = ((DWORD *)inRemoteInfo->UserData)[0];
	}

	char vol_str[10];
	snprintf(vol_str, 10, "%d", volume);
	const char *cmd[] = { "set", "volume", vol_str, NULL };
	check_error(mpv_command(ctx, cmd));

	original_fputs("Setting volume to: ", fDebug);
	original_fputs(vol_str, fDebug);
	original_fputs("\n", fDebug);
#endif

	//register our cleanup function to be called when game exits
	original_atexit(on_game_exit);

	// If the threadId in the ACL is set to 0,
	// then internally EasyHook uses GetCurrentThreadId()
	ULONG ACLEntries[1] = { 0 };

	// Disable the hook for the provided threadIds, enable for all others
	LhSetExclusiveACL(ACLEntries, 1, &hHook);

	original_fputs("Hooking Finished\n", fDebug);
	original_fflush(fDebug);

	return;
}