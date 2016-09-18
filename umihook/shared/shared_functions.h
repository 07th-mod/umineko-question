#ifndef _UMIHOOK_SHARED_FUNCTIONS_H_
#define _UMIHOOK_SHARED_FUNCTIONS_H_

#include <stdio.h>
#include <conio.h>

#define ENABLE_LOGGING

// returns a pointer to the last n characters of the string 
const char * str_end(const char * string, unsigned int n)
{
	unsigned int length = strlen(string);
	if (n > length)
		n = length;
	return &string[length - n];
}

void press_any_key_to_exit()
{
	printf("Press any key to exit...\n");
	_getch();
	exit(0);
}

BOOL makeProcessWindows(LPWSTR command, PROCESS_INFORMATION * pInfo, DWORD creationFlags)
{
	STARTUPINFO sInfo = {0};
	sInfo.cb = sizeof(sInfo);

	return CreateProcess(NULL, command, NULL, NULL, false, creationFlags, NULL, NULL, &sInfo, pInfo);
}



#endif