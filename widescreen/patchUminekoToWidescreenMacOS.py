# see https://github.com/07th-mod/umineko-question/issues/19#issuecomment-254448715 for details on MacOS version

import re, argparse, struct

########################################## Function/Constant Definitions ###############################################
#target instructions to be modified
heightPattern = b'\x66\xb9' + struct.pack('H', 960)
widthPattern =  b'\x66\xb8' + struct.pack('H', 1280)

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
parser.add_argument('--filename', default = "Umineko4")
parser.add_argument('--width', type=int, default = 1707)
parser.add_argument('--height', type=int, default = 960)

args = parser.parse_args()
print('Settings used:', args)

#calculate what the new instructions should be
newHeightPattern = b'\x66\xb9' + struct.pack('H', args.height)
newWidthPattern =  b'\x66\xb8' + struct.pack('H', args.width)

print("\n\n----------------")
print("Begin exe widescreen patch FOR MAC OS")
print("----------------")

with open(args.filename, "rb") as f:
    exe_byte_array = f.read()

    print("----------------")
    print("Trying to patch resolution to {}x{}. Changes:".format(args.width, args.height))
    print("\tHeight: [{}] -> [{}]".format(getInstructionString(heightPattern),getInstructionString(newHeightPattern)))
    print("\tWidth : [{}] -> [{}]".format(getInstructionString(widthPattern), getInstructionString(newWidthPattern)))

    #substitute the old byte sequence with the new byte sequence. Only replace the first instance
    (exe_byte_array, n_height_subs) = re.subn(heightPattern, newHeightPattern, exe_byte_array, count=1)
    if n_height_subs != 1:
        print("Error: couldn't patch set height instruction!")
        exit(0)
    print(n_height_subs, "Height values patched successfully")

    (exe_byte_array, n_width_subs) = re.subn(widthPattern, newWidthPattern, exe_byte_array, count=1)
    if n_width_subs != 1:
        print("Error: couldn't patch set width instruction!")
        exit(0)
    print(n_width_subs, "Width values patched successfully")

    #finally, save the file to disk with 'widescreen' appended to the name
    with open('widescreen-' + args.filename, 'wb') as outFile:
        outFile.write(exe_byte_array)
        print("----------------")
        print("Finished, wrote to:", outFile.name)