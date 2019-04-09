
# [>> CLICK HERE TO INSTALL THE MOD <<](http://07th-mod.com/wiki/Umineko/Umineko-Getting-started/)

<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>
<br>


# Developer Information

## Branches

**ALL DEVELOPMENT NOW HAPPENS ON THE `1080p` BRANCH!**

Do not make any code changes to the `master` branch.

I'm waiting until the new installer is ready before merging the 1080p branch into master (as the old installer still relies on the 'master' branch).

## Important information

Generally, you only want to look at the main umineko script, on the '1080p' branch.

The main umineko script file for development is located in `InDevelopment\ManualUpdates\0.utf`


## Folder structure:

- `InDevelopment\ManualUpdates\0.utf`: The main umineko script file, where development happens
- `InDevelopment\UminekoVoiceParser`: The c# project which was initially used to merge the voice lines from the old script to the new script
- `dev`: This folder contains the (I believe) original script files. Usually you don't use this folder unless you need to check something
- `image_resizing`: This folder contains xnConvert scripts which can be used to resize PS3 backgrounds etc.
- `tools\VoicePuter`: Contains a c# project which was used to port the japanese voices for the question and answer arcs.
- `tools\adv_mod`: Contains an abandoned c# project which was used to add ADV mode - however please don't use that as reference, the final ADV mode was NOT implemented using this project.
- `umihook`: Contains a project which was used to hook file accesses of the game and play back the PS3 .at3 files externally (as the game doesn't support .at3 files). It would probably be easier to either capture the output from debug mode or recompile the game if you really want to attempt this. The audio quality probably wouldn't be improved significantly even with this method.
- `widescreen`: contains old python scripts used for converting widescreen (consider removal of this folder). The answer arcs repository has more up-to-date scripts.
- `POnscripter_Video_Encode_Settings.txt`: Contains ffmpeg settings used for encoding the videos - however you may still need to play around with the settings and test within the game to make sure they work.

# Umineko no Naku Koro ni (Question Arcs) 

This patch aims to modify the newest release of Umineko by MangaGamer, changing its assets to replicate the PS3 version of the game.
It is compatible with the Steam version ***and*** the MangaGamer DRM-free download. See the roadmap below for current patch status.

## Install Instructions

See the Getting Started guide: http://07th-mod.com/wiki/Umineko/Umineko-Getting-started/

## Troubleshooting

See the [Troubleshooting](http://07th-mod.com/wiki/Umineko/Umineko-Part-0-TroubleShooting-and-FAQ/) section of the Wiki.

## Feedback and bug reports

We now have a discord server: https://discord.gg/acSbBtD . If you have a formal issue to report, please still raise a github issue as it helps us keep track of issues. For small problems/questions however you can just ask on discord.

The patch is now fully released, however some bugs may remain. If you think you've found a bug, first check if the bug has already been reported.

For most issues, it is extremely useful to get the game's error log. Newer auto-installations can simply double click the "Umineko1to4_DebugMode.bat" file to start the game in debug mode. However, if you are missing the batch file, ([download this to your game directory, and rename it as a .bat file](https://github.com/07th-mod/resources/raw/master/umineko-question/utilities/StartUminekoInDebugMode.bat))

Once you have started the game in debug mode, just play until the game crashes or behaves badly, then submit the `stderr.txt` and `stdout.txt` to us (when game is started in debug mode, a folder will appear showing `stderr.txt` and `stdout.txt`).

Open a new issue (or find the relevant existing issue) and please include as much information as you can in the ['Issues'](https://github.com/07th-mod/umineko-question/issues) section. You can copy and paste the following template:

- [a description of the issue]
- [pictures of the issue (if applicable)]
- The bug occurs just after [text from when bug occurs] is shown on the screen
- My operating system is [Windows, MacOS, Linux]
- I installed the game [X Months ago]
- I am running the [Full Patch / Voice only patch]
- I installed the game [Manually / using the Automatic Installer]
- My computer is a [Gaming Beast / Laptop with Integrated Graphics]
- Add the `stderr.txt` and `stdout.txt` to the post

If you're not sure if it's a bug, you can report it on our discord (but try not to post any spoilers, if possible).
We really appreciate your help!

## Screenshots

![](https://i.imgur.com/EWITCxL.jpg)
![](https://i.imgur.com/NXUNU4r.jpg)

## Roadmap

The patch is currently **FINISHED**, although some bugs may remain...

- [x] Voices
- [x] Sprites
- [x] Backgrounds
- [x] Effects
- [x] CGs
- [x] Menus

## Credits

 * [DoctorDiablo](https://github.com/DoctorDiablo)
 * [drojf](https://github.com/drojf)
 * [Forteissimo](https://github.com/Forteissimo)
 * [ReitoKanzaki](https://github.com/ReitoKanzaki)

There is another 'Umineko Modification' project which has a different set of goals, see [Umineko Project](https://umineko-project.org/en/) if you are interested in that.
