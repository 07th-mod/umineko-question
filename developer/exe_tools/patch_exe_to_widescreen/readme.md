## Important
The default resolution has been set to 1706 x 960 (original game was 1280x960). This is so that a minimal amount of things in the game break (as the 'height' of the game is kept the same. Unfortunately, this results in the screen always being stretched slightly to 1920 x 1080 if you enter fullscreen mode. Setting the resolution to 1080p is possible with this script, however you will have to deal with more things in the game breaking - the upside is that background images will be as sharp as possible.

## Widescreen patch instructions

0) Apply any patches to the script you want first (eg voice patches, script fixes)

1) Copy the python script into the directory containing the game .exe and 0.utf file

2) Run the script (from command line, "py patchUminekoToWidescreen.py"). This will generate "widescreen-Umineko1to4.exe" and "widescreen-0.utf"

3) Rename your old "0.utf" to something else, like "0.original.utf". Then rename the "widescreen-0.utf" to "0.utf" (you can _try_ to use the --script argument to the game .exe, but I found it didn't work sometimes)

4) Copy in the "bmp" folder containing the resized effect images and resized backgrounds. no need to repack the .nsa, the game will prefer to load the files in folders and not the .nsa files. 

5) Run the widescreen version of the .exe . The main menu is not fixed yet.

## Notes:
- When the python script patches the game .exe, it does not apply a diff, instead it searches for the instruction to set the width and height, then changes the value which is set. So in theory, even if the game is updated, it should still work.
- The python script will patch _any_ version of the script, it doesn't have to be the original one. The intention is to run the widescreen patch after every script revision to produce a different "widescreen version" of the game. It will only touch lines which start with "[whitespace]setwindow". The code for this section is messy because that part is not final and may change.
- Don't run the script on a 0.utf which has already been wide-screened
- I could make the script automatically rename the 0.utf file, but I don't want to break anything

## Problems:
- At some point need to go through the entire bmp folder to check all images which are not of the correct aspect ratio. In most cases, nothing that bad will happen, they will just appear centered in the middle of the screen. Some images would be very difficult to resize (the flashback images??), so probably they will have to be left the way they are. Effect files MUST be resized, otherwise when effects in the game are used, they will only occupy a portion of the screen.