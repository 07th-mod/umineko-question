Allows the the patched MG release of umineko to play the original .at3 (Atrac3plus) files.

## Usage:

- Ensure that voice files are present in the root directory under a folder colled 'voice' (eg .\voice\10\10100041.at3)
- Copy the program and 3 DLL files to the root directory.
- Run the program. If your game .exe has a different name, you can specify it on the command line as the first argument
- The second argument is the volume for playback as an integer percentage

## Implementation:

When POnscripter plays a sound file, it doesn't preload it - it just 
access a file on disk/in an archive. Therefore we can determine which file is going to be played by intercepting all disk accesses. 

- All fopen calls by the game are intercepted (using the "EasyHook" library)
- If the file extension is not .ogg, this program will ignore the access and just issue a normal fopen call
- If the file extension is .ogg, then it's considered a voice file or a music file, and the program will try to find a corresponding .at3 file at the same file path (just swaps .ogg for .at3 in the file path)
- If the .at3 file is found, it is played back using the libMPV library

## Precompiled Binaries:
[MEGA download link](https://mega.nz/#!FshzFbab!5DM2jrb7lN6I-uLVjQjftuUK3MlWs543xNBSOtpelQc)

## Manual Compilation:
Probably easiest to use Visual Studio and open the project. You may need to update the included header and .lib /.a files if you are using a largely different version of the .dll files The only requirements are the Easyhook library (for the .exe injector) and libMPV for .at3 playback libMPV windows builds can be found here: https://mpv.srsfckn.biz/ (under 'Dev')

If you using this as a base for a different program, you must change the "msvcrt" library to whatever library is used by your program. It would be a good idea to check what imports the program uses using IDA free or a similar program.