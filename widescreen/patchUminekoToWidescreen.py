# sequence of instructions can be decoded at https://defuse.ca/online-x86-assembler.htm#disassembly
# mov edx, 960
# mov ecx, 1280
# BA C0030000   -> mov edx, 0x03c0 (note immediate operand is little-endian for x86)
# B9 00050000   -> mov ecx, 0x0500
# or reference, expected values for 1920 x 1080 are: width: 80 07 00 00, height: 38 04 00 00
import re, hashlib, argparse

########################################## Function/Constant Definitions ###############################################

# a list of output .exe files' sha-1 values which are known to work
valid_sha1 = ['f9f26593d5dc5a5efd917404699a7f0c04ad3c26']

#target instructions to be modified
heightPattern = b'\xBA\xC0\x03\x00\x00'
widthPattern = b'\xB9\x00\x05\x00\x00'

#functions only for printing - not critical
def getInstructionString(arr, offset = 0):
    return ' '.join(['{:02x}'.format(b) for b in arr[offset:offset + 5]])

def printInstruction(arr, offset):
    print(getInstructionString(arr,offset))

################################################# Begin code execution #################################################

print("""Remember that the background images and effect images must be patched to a 16:9 aspect ratio!
Movie files should also be changed to 1080p otherwise they will not fill the screen.
Please use --width and --height if you would like a custom aspect ratio.""")

#read in height/width/filename from command line if provided
parser = argparse.ArgumentParser(description='Patch Umineko to use a different resolution')
parser.add_argument('--filename', default = "Umineko1to4.exe")
parser.add_argument('--width', type=int, default = 1706)
parser.add_argument('--height', type=int, default = 960)
parser.add_argument('--script', default ='0.utf')
parser.add_argument('--windows_line_endings', action='store_true')
parser.add_argument('--debug', action='store_true') #enter debug mode if '-d' given

args = parser.parse_args()
print('Settings used:', args)

#set line ending mode (either force windows newlines, or use whatever is given
newlineMode=''
if args.windows_line_endings:
    newlineMode = None

#calculate what the new instructions should be
newHeightPattern = b'\xBA' + args.height.to_bytes(4, 'little')
newWidthPattern = b'\xB9' + args.width.to_bytes(4, 'little')

print("\n\n----------------")
print("Begin exe widescreen patch")
print("----------------")

with open(args.filename, "rb") as f:
    exe_byte_array = f.read()

    #calculate sha1 of the exe and check if it is as expected. if not, possibly different .exe version but will still work
    m = hashlib.sha1()
    m.update(exe_byte_array)

    print('Input file SHA-1 [{}]'.format(m.hexdigest()))
    if m.hexdigest() in valid_sha1:
        print('SHA-1 OK, patch should work')
    else:
        print('SHA-1 does not match, however may still work. Please report this new SHA-1 value, along with whether patch was successful')

    print("----------------")
    print("Trying to patch resolution to {}x{}. Changes:".format(args.width, args.height))
    print("\tHeight: [{}] -> [{}]".format(getInstructionString(heightPattern),getInstructionString(newHeightPattern)))
    print("\tWidth : [{}] -> [{}]".format(getInstructionString(widthPattern), getInstructionString(newWidthPattern)))

    #substitute the old byte sequence with the new byte sequence. Only replace the first instance
    (exe_byte_array, n_height_subs) = re.subn(heightPattern, newHeightPattern, exe_byte_array, count=1)
    if n_height_subs != 1:
        print("Error: couldn't patch set height instruction!")
        exit(0)

    (exe_byte_array, n_width_subs) = re.subn(widthPattern, newWidthPattern, exe_byte_array, count=1)
    if n_width_subs != 1:
        print("Error: couldn't patch set width instruction!")
        exit(0)

    #finally, save the file to disk with 'widescreen' appended to the name
    with open('widescreen-' + args.filename, 'wb') as outFile:
        outFile.write(exe_byte_array)
        print("----------------")
        print("Finished, wrote to:", outFile.name)

####################################### Script Setwindow argument modification #########################################

# accepts a string containing the arguments to the 'setWindow' function in onscripter (in CSV format)
# modifies the arguments for widescreen mode, then returns the same string with modified arguments
# big NOTE: sometimes, the setwindow function doesn't actually fill the whole screen. I think this is the bgm
# demo scenes,so not at all important. See 'setWindowExample.txt' for a list.
def modifySetWindowArguments(setWindowArguments):
    #want to center the text in the middle of the screen
    desiredRealPixelOffset = (args.width - 1280)/2

    #original width of game was 1280, which corresponded to a width of 640 in the script
    #therefore scaling factor is 640/1280 to get from real pixel offset to script offset
    scriptOffset = int(desiredRealPixelOffset * 640 / 1280)

    csv_array = setWindowArguments.split(',')

    #adjust "text top left x"
    csv_array[0] = str(int(csv_array[0]) + scriptOffset)

    #adjust "window top left x"
    #edit: looks better if the window takes up the whole screen
    #csv_array[12] = str(int(csv_array[12]) + scriptOffset)

    #adjust "window bottom left x" - need to add 1 so that covers central region completely (otherwise small strip is left uncovered)
    #csv_array[14] = str(int(csv_array[14]) + scriptOffset + 1)
    #edit: just make it take up the whole screen
    csv_array[14] = str(int(args.width * 640 / 1280))

    #adjust "window bottom left y" - I found that subtracting 1 makes a small border, so don't subtract 1.
    current_window_bottom_left_y = int(csv_array[15])
    if args.height != 960:
        csv_array[15] = str(int((current_window_bottom_left_y+1) / 960 * args.height))
    #csv_array[15] = str(int((current_window_bottom_left_y+1) / 960 * 1080 - 1))

    return ','.join(csv_array)

print("\n\n----------------")
print("Begin widescreen script modification")
print("----------------")

numMatches = 0
with open(args.script, encoding='utf-8') as script:
    with open('widescreen-' + args.script, 'w', encoding='utf-8', newline=newlineMode) as outScript:
        for line in script:
            #search for lines which contain the 'setwindow' function, and extract the arguments. leave all other lines alone
            matchObject = re.match('(\s*setwindow )(.*)', line)
            if matchObject:
                newline = matchObject.group(1) + modifySetWindowArguments(matchObject.group(2)) + '\n' #regex ate the newline
                outScript.write(newline)
                numMatches += 1

                if args.debug:
                    print('changed\n', line, 'to\n', newline)
                else:
                    print('.', end='')
            else:
                outScript.write(line)

        print('found', numMatches, 'matches, expected', 56)
        print('Finished, wrote to:', outScript.name)
